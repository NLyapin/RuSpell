using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Language.Intellisense;

namespace RuSpell
{
    /// <summary>
    /// Тег с описание замен для опечатки.
    /// </summary>
    internal class MisspellingSmartTag : SmartTag
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="actionSets">Коллекция экшенов с вариантами изменения опечатки.</param>
        public MisspellingSmartTag(ReadOnlyCollection<SmartTagActionSet> actionSets) :
            base(SmartTagType.Factoid, actionSets)
        {
        }
    }
}
