using System;
using System.Collections.Generic;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Профильная система.
    /// </summary>
    public class ProfileSystem : BaseDictionaryModel
    {
        #region Основные свойства

        [ColumnDisplayName("Название")]
        public override string Name { get; set; }

        [ColumnDisplayName("Описание")]
        public string Description { get; set; }

        [ColumnDisplayName("Поставщик", false)]
        public SystemProvider Provider { get; set; }

        // Навигационное свойство
        [ColumnVisible(false)]
        public List<ProfileArticle> ProfileArticles { get; set; } = new List<ProfileArticle>();

        #endregion

        #region Служебные свойства

        [ColumnDisplayName("Поставщик", false)]
        public int SystemProviderId { get; set; }

        #endregion
    }
}
