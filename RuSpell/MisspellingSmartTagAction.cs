using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace RuSpell
{
    /// <summary>
    /// Пункт в контекстном меню (action) с предлагаемым написанием слова.
    /// </summary>
    internal class MisspellingSmartTagAction : ISmartTagAction
    {
        /// <summary>
        /// Объект спана с которым ассоциируется ошибка.
        /// </summary>
        private readonly ITrackingSpan span;
        
        /// <summary>
        /// Выражение, которым нужно заменить ошибочное выражение.
        /// </summary>
        private readonly string replaceWith;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="span">Спан, который нужно заменить.</param>
        /// <param name="replaceWith">На что предлагается заменить.</param>
        public MisspellingSmartTagAction(ITrackingSpan span, string replaceWith)
        {
            this.span = span;
            this.replaceWith = replaceWith;
        }

        /// <summary>
        /// Предлагаемое написание.
        /// </summary>
        public string DisplayText
        {
            get
            {
                return replaceWith;
            }
        }

        /// <summary>
        /// Иконка для пункта меню.
        /// </summary>
        public System.Windows.Media.ImageSource Icon
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Действие по замене текста.
        /// </summary>
        public void Invoke()
        {
            span.TextBuffer.Replace(span.GetSpan(span.TextBuffer.CurrentSnapshot), replaceWith);
        }

        /// <summary>
        /// Доступность.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Дочерние действия.
        /// </summary>
        public ReadOnlyCollection<SmartTagActionSet> ActionSets
        {
            get
            {
                return null;
            }
        }
    }
}
