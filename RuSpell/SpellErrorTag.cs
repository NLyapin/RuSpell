using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace RuSpell
{
    /// <summary>
    /// Тег орфографической ошибки.
    /// </summary>
    internal class SpellErrorTag : ErrorTag
    {
        /// <summary>
        /// Конструктор. 
        /// </summary>
        /// <param name="span">Спан с ошибкой.</param>
        /// <param name="suggestions">Предложения с исправлениями.</param>
        public SpellErrorTag(SnapshotSpan span, IEnumerable<string> suggestions)
        {
            if(suggestions == null)
            {
                throw new ArgumentException("suggestions");
            }

            Suggestions = suggestions.ToArray();
            Span = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
        }

        /// <summary>
        /// Спан.
        /// </summary>
        public ITrackingSpan Span { get; private set; }

        /// <summary>
        /// Набор возможных вариантов.
        /// </summary>
        public IEnumerable<string> Suggestions { get; private set; }

        /// <summary>
        /// Получение тег спана на основе snapshot.
        /// </summary>
        /// <param name="snapshot">Snapshot.</param>
        /// <returns>Tag span.</returns>
        public ITagSpan<SpellErrorTag> GetTagSpan(ITextSnapshot snapshot)
        {
            return new TagSpan<SpellErrorTag>(Span.GetSpan(snapshot), this);
        }
    }
}
