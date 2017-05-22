using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace RuSpell
{
    /// <summary>
    /// Таггер для списка ошибок.
    /// </summary>
    internal class SpellErrorTagger : ITagger<SpellErrorTag>, IDisposable
    {
        /// <summary>
        /// Набор разделителей для слов.
        /// </summary>
        private const string wordBreakers = "\"\'\\{}()[]-:;.,!?_\t/*@";

        private readonly IClassifier classifier;
        private readonly ITextBuffer buffer;
        private readonly ITextView view;
        private readonly DispatcherTimer timer;

        /// <summary>
        /// Список спанов, которые нужно проверить.
        /// </summary>
        private readonly ConcurrentQueue<SnapshotSpan> notCheckedSpans = new ConcurrentQueue<SnapshotSpan>();
        
        /// <summary>
        /// Список уже найденных ошибок.
        /// </summary>
        private readonly List<SpellErrorTag> spellErrors = new List<SpellErrorTag>();
        
        /// <summary>
        /// Lock-объект для списка уже найденных ошибок.
        /// </summary>
        private readonly object spellErrorsLock = new object();

        /// <summary>
        /// Событие изменения набора тегов с ошибками.
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// Конструктор.
        /// </summary>        
        /// <param name="textView">View.</param>        
        /// <param name="buffer">Буффер.</param>
        /// <param name="classifier">Объект классификатора.</param>
        public SpellErrorTagger(ITextView textView, ITextBuffer buffer,  IClassifier classifier)
        {
            timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, Dispatcher.CurrentDispatcher)
                        {
                            Interval = TimeSpan.FromMilliseconds(500)
                        };
            timer.Tick += SpellCheckingThread;
            timer.Start();

            this.buffer = buffer;
            this.classifier = classifier;
            this.view = textView;

            this.classifier.ClassificationChanged += ClassificationChanged;
            this.view.Closed += ViewOnClosed;
            this.buffer.Changed += BufferOnChanged;
            
            foreach (var lineOfCode in buffer.CurrentSnapshot.Lines)
            {
                AddNotCheckedSpan(lineOfCode.Extent);
            }
        }

        /// <summary>
        /// Метод получения тегов.
        /// </summary>
        /// <param name="spans">Нормализованная коллекция спанов.</param>
        /// <returns>Список тегов с ошибками.</returns>
        public IEnumerable<ITagSpan<SpellErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (classifier == null || spans == null || !spans.Any())
            {
                yield break;
            }

            lock (spellErrorsLock)
            {
                if (!spellErrors.Any())
                {
                    yield break;
                }

                var snapshot = buffer.CurrentSnapshot;
                foreach (var spellError in spellErrors)
                {
                    var tagSpan = spellError.GetTagSpan(snapshot);
                    if (tagSpan.Span.Length == 0)
                    {
                        continue;
                    }
                    if (spans.IntersectsWith(new NormalizedSnapshotSpanCollection(tagSpan.Span)))
                    {
                        yield return tagSpan;
                    }
                }
            }
        }        

        /// <summary>
        /// Реализация Dispose.
        /// </summary>
        public void Dispose()
        {
            if (classifier != null)
            {
                classifier.ClassificationChanged -= ClassificationChanged;
            }
        }

        /// <summary>
        /// Разбивает некоторую строку с текстом на набор слов.
        /// </summary>
        /// <param name="text">Исходный текст.</param>
        /// <returns>Набор слов.</returns>
        private IEnumerable<Span> ParseTextForWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            for (int i = 0; i < text.Length; i++)
            {
                if (IsWordBreaker(text[i]))
                {
                    continue;
                }
                var end = i;
                for (; end < text.Length; end++)
                {
                    if (IsWordBreaker(text[end]))
                    {
                        break;
                    }
                }
                yield return Span.FromBounds(i, end);
                i = end - 1;
            }
        }

        /// <summary>
        /// Является ли символ разделителем слов.
        /// </summary>
        /// <param name="character">Символ, который нужно проверить.</param>
        /// <returns>True, если символ является разделителем слов.</returns>
        private bool IsWordBreaker(char character)
        {
            return char.IsWhiteSpace(character) || wordBreakers.Contains(character);
        }
        
        /// <summary>
        /// Поток проверки орфографии.
        /// </summary>
        /// <param name="sender">Отправитель.</param>
        /// <param name="eventArgs">Параметры.</param>
        private void SpellCheckingThread(object sender, EventArgs eventArgs)
        {                        
            var spansForCheck = new List<SnapshotSpan>(notCheckedSpans.Count);
            SnapshotSpan span;
            while (notCheckedSpans.TryDequeue(out span))
            {
                spansForCheck.Add(span);                
            }
            if (!spansForCheck.Any())
            {
                return;
            }

            var snapshot = buffer.CurrentSnapshot;
            var normalizedSpansForCheck = new NormalizedSnapshotSpanCollection(spansForCheck.Select(s => s.TranslateTo(snapshot, SpanTrackingMode.EdgeInclusive)));
            CheckForSpellErrors(normalizedSpansForCheck);                                                     
        }

        /// <summary>
        /// Производит проверку для указанного набора спанов.
        /// </summary>
        /// <param name="spans">Список спанов, которые нужно проверить.</param>
        private void CheckForSpellErrors(IEnumerable<SnapshotSpan> spans)
        {                        
                var snapshot = buffer.CurrentSnapshot;
                var spansForUpdate = new List<SnapshotSpan>();    
                foreach (var spanForCheck in spans)
                {
                    lock (spellErrorsLock)
                    {
                        spellErrors.RemoveAll(tag => tag.GetTagSpan(snapshot).Span.OverlapsWith(spanForCheck));
                        spellErrors.RemoveAll(tag => tag.GetTagSpan(snapshot).Span.IsEmpty);                        
                    }
                    
                    foreach (var classificationSpan in classifier.GetClassificationSpans(spanForCheck))
                    {
                        var name = classificationSpan.ClassificationType.Classification.ToLowerInvariant();                        
                        if (name.Contains("comment") || name.Contains("string"))
                        {
                           spansForUpdate.Add(classificationSpan.Span);
                        }
                    }                    
                }    
            
                foreach (var span in (IEnumerable<SnapshotSpan>)spansForUpdate)
                {
                    var text = span.GetText();
                    foreach (var word in ParseTextForWords(text))
                    {
                        var wordText = span.Snapshot.GetText(span.Start + word.Start, word.Length);
                        if (HunspellWrapper.Spell(wordText))
                        {
                            continue;
                        }
                        var suggestions = HunspellWrapper.Suggest(wordText);
                        var errorSpan = new SnapshotSpan(span.Start + word.Start, word.Length);
                        lock (spellErrorsLock)
                        {
                            spellErrors.Add(new SpellErrorTag(errorSpan, suggestions));
                        }                        
                    }                    
                }
        }

        /// <summary>
        /// Обработчик события закрытия view.
        /// </summary>
        /// <param name="sender">Отправитель.</param>
        /// <param name="eventArgs">Параметры события.</param>
        private void ViewOnClosed(object sender, EventArgs eventArgs)
        {
            Dispose();
        }

        /// <summary>
        /// Обработчик события изменений в буфере.
        /// </summary>
        /// <param name="sender">Отправитель.</param>
        /// <param name="textContentChangedEventArgs">Параметры события.</param>
        private void BufferOnChanged(object sender, TextContentChangedEventArgs textContentChangedEventArgs)
        {
            var snapshot = textContentChangedEventArgs.After;

            foreach (var change in textContentChangedEventArgs.Changes)
            {
                var changedSpan = new SnapshotSpan(snapshot, change.NewSpan);
                var startLine = changedSpan.Start.GetContainingLine();
                var endLine = (startLine.EndIncludingLineBreak < changedSpan.End) ? changedSpan.End.GetContainingLine() : startLine;

                AddNotCheckedSpan(new SnapshotSpan(startLine.Start, endLine.End));                
                RaiseTagsChanged(new SnapshotSpan(startLine.Start, endLine.End));
            }
        }

        /// <summary>
        /// Добавляет спан в коллекцию еще не проверенных спанов. 
        /// </summary>
        /// <param name="span">Спан.</param>
        private void AddNotCheckedSpan(SnapshotSpan span)
        {
            if(span.IsEmpty)
            {
                return;
            }
            notCheckedSpans.Enqueue(span);
        }                

        /// <summary>
        /// Изменение классификатора.
        /// </summary>
        /// <param name="sender">Отправитель.</param>
        /// <param name="eventArgs">Параметр события.</param>
        private void ClassificationChanged(object sender, ClassificationChangedEventArgs eventArgs)
        {               
            //AddNotCheckedSpan(eventArgs.ChangeSpan);
            RaiseTagsChanged(eventArgs.ChangeSpan);
        }

        /// <summary>
        /// Пробрасывает событие изменения набора тегов.
        /// </summary>
        /// <param name="span">Объект спана.</param>
        private void RaiseTagsChanged(SnapshotSpan span)
        {            
            var temp = TagsChanged;
            if (temp != null)
            {                                
                temp(this, new SnapshotSpanEventArgs(span.TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive)));
            }            
        }
    }
}
