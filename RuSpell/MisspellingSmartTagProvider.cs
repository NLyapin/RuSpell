using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace RuSpell
{
    /// <summary>
    /// Провайдер для подсказок с изменениями. 
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("any")]
    [TagType(typeof(Microsoft.VisualStudio.Language.Intellisense.SmartTag))]
    internal class MisspellingSmartTagProvider : IViewTaggerProvider
    {
        /// <summary>
        /// Фабрика для агрегатора.
        /// </summary>
        [Import]
        internal IViewTagAggregatorFactoryService TagAggregatorFactory = null;

        /// <summary>
        /// Создает теггер для подсказок с изменениями.
        /// </summary>
        /// <typeparam name="T">Тип тега.</typeparam>
        /// <param name="textView">Текущее view.</param>
        /// <param name="buffer">Текущий буфер.</param>
        /// <returns>Теггер.</returns>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {            
            if (!textView.Roles.Contains(PredefinedTextViewRoles.Editable) || !textView.Roles.Contains(PredefinedTextViewRoles.PrimaryDocument))
            {
                return null;
            }
            
            if (buffer != textView.TextBuffer)
            {
                return null;
            }

            var misspellingAggregator = TagAggregatorFactory.CreateTagAggregator<SpellErrorTag>(textView);
            return new MisspellingSmartTagger(buffer, misspellingAggregator) as ITagger<T>;
        }        
    }
}
