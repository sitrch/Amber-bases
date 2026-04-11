using System;
using System.Collections.Generic;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Базовый класс для моделей справочников (поддержка рефлексии).
    /// </summary>
    public abstract class BaseDictionaryModel
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public string Info { get; set; }
    }
}