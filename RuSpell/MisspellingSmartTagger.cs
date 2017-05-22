using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace RuSpell
{
    /// <summary>
    /// Таггер для получения списка возможных изменений опечатки.
    /// </summary>
    internal class MisspellingSmartTagger : ITagger<MisspellingSmartTag>, IDisposable
    {
        /// <summary>
        /// Агрегатор тегов с ошибками.
        /// </summary>
        private readonly ITagAggregator<SpellErrorTag> misspellingAggregator;

        /// <summary>
        /// Текстовый буфер.
        /// </summary>
        private ITextBuffer buffer;

        /// <summary>
        /// Событие изменения 
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="buffer">Буфер с текущим текстом из редактора.</param>
        /// <param name="misspellingAggregator">Агрегатор тегов с орфографическими ошибками и опечатками.</param>
        public MisspellingSmartTagger(ITextBuffer buffer, ITagAggregator<SpellErrorTag> misspellingAggregator)
        {
            this.misspellingAggregator = misspellingAggregator;
            this.buffer = buffer;
            
            misspellingAggregator.TagsChanged += MisspellingTagsChanged;
        }        

        /// <summary>
        /// Получение тегов.
        /// </summary>
        /// <param name="spans">Набор спанов.</param>
        /// <returns>Перечисление тегов.</returns>
        public IEnumerable<ITagSpan<MisspellingSmartTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {            
            if (!spans.Any())
            {
                yield break;
            }            

            var snapshot = buffer.CurrentSnapshot;
            foreach (var misspelling in misspellingAggregator.GetTags(spans))
            {                
                var misspellingSpans = misspelling.Span.GetSpans(snapshot);                
                if (misspellingSpans.Count != 1)
                {
                    continue;
                }                

                var errorSpan = misspellingSpans[0];
                var smartTagActions = GetSmartTagActions(errorSpan, misspelling.Tag.Suggestions);
                yield return new TagSpan<MisspellingSmartTag>(errorSpan, new MisspellingSmartTag(smartTagActions));
            }
        }        

        /// <summary>
        /// Реализация IDisposable.
        /// </summary>
        public void Dispose()
        {
            misspellingAggregator.TagsChanged -= MisspellingTagsChanged;
            misspellingAggregator.Dispose();
        }

        /// <summary>
        /// Обработчик события изменения набора тегов.
        /// </summary>
        /// <param name="sender">Отправитель.</param>
        /// <param name="e">Параметр события.</param>
        private void MisspellingTagsChanged(object sender, TagsChangedEventArgs e)
        {
            foreach (var span in e.Span.GetSpans(buffer))
            {
                RaiseTagsChangedEvent(span.TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeExclusive));
            }
        }

        /// <summary>
        /// Получает список smartAction для заданного списка предлагаемых изменений.
        /// </summary>
        /// <param name="errorSpan">Текущий спан.</param>
        /// <param name="suggestions">Предлагаемое написание слова.</param>
        /// <returns>Список smart actions.</returns>
        private ReadOnlyCollection<SmartTagActionSet> GetSmartTagActions(SnapshotSpan errorSpan, IEnumerable<string> suggestions)
        {            
            var trackingSpan = errorSpan.Snapshot.CreateTrackingSpan(errorSpan, SpanTrackingMode.EdgeExclusive);            
            var actions = suggestions.Select(suggestion => new MisspellingSmartTagAction(trackingSpan, suggestion))
                                     .Cast<ISmartTagAction>()
                                     .ToList();

            var smartTagSets = new List<SmartTagActionSet>();
            if (actions.Any())
            {
                smartTagSets.Add(new SmartTagActionSet(actions.AsReadOnly()));
            }
            return smartTagSets.AsReadOnly();
        }

        /// <summary>
        /// Бросает событие изменения набора тегов.
        /// </summary>
        /// <param name="subjectSpan"></param>
        private void RaiseTagsChangedEvent(SnapshotSpan subjectSpan)
        {
            var handler = TagsChanged;
            if (handler != null)
            {
                handler(this, new SnapshotSpanEventArgs(subjectSpan));
            }
        }
    }
}