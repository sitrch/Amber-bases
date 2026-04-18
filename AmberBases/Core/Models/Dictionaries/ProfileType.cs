using System;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Тип профиля.
    /// </summary>
    public class ProfileType : BaseDictionaryModel
    {
        #region Основные свойства

        [ColumnDisplayName("Название")]
        public string Name { get; set; }

        #endregion
    }
}