using System;
using System.Collections.Generic;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Поставщик профильных систем.
    /// </summary>
    public class SystemProvider : BaseDictionaryModel
    {
        public string Name { get; set; }
        public string Information { get; set; }
        
        // Навигационное свойство
        public List<ProfileSystem> ProfileSystems { get; set; } = new List<ProfileSystem>();
    }
}
