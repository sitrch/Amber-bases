using System;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Базовый класс для моделей справочников (поддержка рефлексии).
    /// </summary>
    public abstract class BaseDictionaryModel
    {
        [ColumnDisplayName("ID", false)]
        public int Id { get; set; }

        [ColumnDisplayName("Позиция")]
        public int Position { get; set; }

        [ColumnDisplayName("Информация")]
        public string Info { get; set; }
    }
}