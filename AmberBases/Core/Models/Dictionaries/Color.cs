using System;

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
        public string ColorName { get; set; }

        /// <summary>
        /// Computed.свойство для совместимости с FK lookup (DisplayMemberPath = "Name").
        /// </summary>
        public string Name => ColorName;
        
        /// <summary>
        /// Код RAL.
        /// </summary>
        public int RAL { get; set; }

        /// <summary>
        /// Тип покрытия (навигационное свойство).
        /// </summary>
        public CoatingType CoatingType { get; set; }
        
        #endregion

        #region Служебные свойства
        
        /// <summary>
        /// ID типа покрытия.
        /// </summary>
        public int? CoatingTypeId { get; set; }
        
        #endregion
    }
}
