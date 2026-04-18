using System;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Тип покрытия.
    /// </summary>
    public class CoatingType : BaseDictionaryModel
    {
        #region Основные свойства

        /// <summary>
        /// Название типа покрытия.
        /// </summary>
        [ColumnDisplayName("Название")]
        public string Name { get; set; }

        #endregion
    }
}