using System;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Цвет.
    /// </summary>
    public class Color : BaseDictionaryModel
    {
        /// <summary>
        /// Название цвета (отображаемое значение).
        /// </summary>
        public string ColorName { get; set; }
        
        /// <summary>
        /// Код RAL.
        /// </summary>
        public int RAL { get; set; }
        
        /// <summary>
        /// ID типа покрытия.
        /// </summary>
        public int? CoatingTypeId { get; set; }
    }
}
