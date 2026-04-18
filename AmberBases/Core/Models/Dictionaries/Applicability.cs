using System;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Применимость.
    /// </summary>
    public class Applicability : BaseDictionaryModel
    {
        #region Основные свойства

        [ColumnDisplayName("Название")]
        public string Name { get; set; }

        #endregion
    }
}