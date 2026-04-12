using System;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Длина хлыста (базы профиля).
    /// </summary>
    public class StandartBarLength : BaseDictionaryModel
    {
        #region Основные свойства
        
        /// <summary>
        /// Длина хлыста.
        /// </summary>
        public decimal Length { get; set; }

        /// <summary>
        /// Computed-свойство для совместимости с FK lookup (DisplayMemberPath = "Name").
        /// </summary>
        public string Name => Length.ToString();
        
        #endregion
    }
}
