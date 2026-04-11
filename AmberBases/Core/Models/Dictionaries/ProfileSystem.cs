using System;
using System.Collections.Generic;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Профильная система.
    /// </summary>
    public class ProfileSystem : BaseDictionaryModel
    {
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public int SystemProviderId { get; set; }
        public SystemProvider Provider { get; set; }
        
        // Навигационное свойство
        public List<ProfileArticle> ProfileArticles { get; set; } = new List<ProfileArticle>();
    }
}
