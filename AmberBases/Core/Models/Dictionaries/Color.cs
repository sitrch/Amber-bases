using System;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Цвет.
    /// </summary>
    public class Color : BaseDictionaryModel
    {
        #region Основные свойства

        /// <summary>
        /// Название цвета (отображаемое значение).
        /// </summary>
        [ColumnDisplayName("Название цвета")]
        public string ColorName { get; set; }

        /// <summary>
        /// Computed.свойство для совместимости с FK lookup (DisplayMemberPath = "Name").
        /// </summary>
        public string Name => ColorName;

        /// <summary>
        /// Код RAL.
        /// </summary>
        [ColumnDisplayName("RAL код")]
        public int RAL { get; set; }

        /// <summary>
        /// Тип покрытия (навигационное свойство).
        /// </summary>
        [ColumnDisplayName("Тип покрытия", false)]
        public CoatingType CoatingType { get; set; }

        #endregion

        #region Служебные свойства

        /// <summary>
        /// ID типа покрытия.
        /// </summary>
        [ColumnDisplayName("Тип покрытия", false)]
        public int? CoatingTypeId { get; set; }

        #endregion
    }
}
