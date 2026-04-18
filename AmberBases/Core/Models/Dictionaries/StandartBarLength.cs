using System;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Длина хлыста (базы профиля).
    /// </summary>
    public class StandartBarLength : BaseDictionaryModel
    {
        #region Основные свойства

        [ColumnDisplayName("Длина")]
        public decimal Length { get; set; }

        /// <summary>
        /// Computed-свойство для совместимости с FK lookup (DisplayMemberPath = "Name").
        /// </summary>
        public string Name => Length.ToString();

        #endregion
    }
}
