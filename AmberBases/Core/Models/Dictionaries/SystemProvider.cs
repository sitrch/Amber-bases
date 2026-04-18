using System;
using System.Collections.Generic;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Поставщик профильных систем.
    /// </summary>
    public class SystemProvider : BaseDictionaryModel
    {
        #region Основные свойства

        [ColumnDisplayName("Название")]
        public string Name { get; set; }

        [ColumnDisplayName("Информация")]
        public string Information { get; set; }

        // Навигационное свойство
        [ColumnVisible(false)]
        public List<ProfileSystem> ProfileSystems { get; set; } = new List<ProfileSystem>();

        #endregion
    }
}
