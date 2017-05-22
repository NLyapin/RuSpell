using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHunspell;

namespace RuSpell
{
    /// <summary>
    /// Класс-оболочка для работы с Hunspell.
    /// </summary>
    public static class HunspellWrapper
    {
        /// <summary>
        /// Объект русского словаря.
        /// </summary>
        private static readonly Hunspell hunspellRussian = new Hunspell(@"ru_RU.aff", @"ru_RU.dic");
        
        /// <summary>
        /// Объект английского словаря.
        /// </summary>
        private static readonly Hunspell hunspellEnglish = new Hunspell(@"en_GB.aff", @"en_GB.dic");

        /// <summary>
        /// Проверяет, что слово имеет корректное написание.
        /// </summary>
        /// <param name="word">Слово, которое нужно проверить.</param>
        /// <returns>True, если слово написано корректно. False в противном случае.</returns>
        public static bool Spell(string word)
        {
            try
            {
                if(!hunspellRussian.Spell(word))
                {
                    return hunspellEnglish.Spell(word);
                }
            }
            catch(Exception) //При работе hunspell очень редко возникает exception, игнорируем и продолжаем работать.
            {                
            }
            return true;
        }

        /// <summary>
        /// Получает список слов, которые могли подразумеваться при написании некорректного слова.
        /// </summary>
        /// <param name="word">Слово для которого нужно получить варианты написания.</param>
        /// <returns>Список слов, предлагаемых для исправления написания.</returns>
        public static IEnumerable<string> Suggest(string word)
        {
            try
            {
                var result = hunspellRussian.Suggest(word);
                if(!result.Any())
                {
                    result = hunspellEnglish.Suggest(word);
                }
                return result;
            }
            catch (Exception)//При работе hunspell очень редко возникает exception, игнорируем и продолжаем работать.
            {                             
            }            
            return new List<string>();
        }
    }
}
