using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace RuSpell
{
    /// <summary>
    /// Теггер провайдер для орфографических ошибок.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(SpellErrorTag))]
    internal class SpellErrorTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal IClassifierAggregatorService ClassifierAggregatorService { get; set; }

        [Import]
        internal IBufferTagAggregatorFactoryService TagAggregatorFactory { get; set; }        
        
        /// <summary>
        /// Создает теггер для списка ошибок.
        /// </summary>
        /// <typeparam name="T">Тип тега.</typeparam>
        /// <param name="textView">Текущее view.</param>
        /// <param name="buffer">Текущий буфер.</param>
        /// <returns>Теггер.</returns>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {                        
            if (textView == null)
            {
                throw new ArgumentNullException("textView");
            }

            if (textView.TextBuffer != buffer)
            {
                return null;
            }

            if(buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            SpellErrorTagger tagger;
            if (textView.Properties.TryGetProperty(typeof(SpellErrorTagger), out tagger))
            {
                return tagger as ITagger<T>;
            }

            var classifierAggregator = ClassifierAggregatorService.GetClassifier(buffer);
            tagger = new SpellErrorTagger(textView, buffer, classifierAggregator);
            textView.Properties[typeof(SpellErrorTagger)] = tagger;
            return tagger as ITagger<T>;
        }
    }
}
