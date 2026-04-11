using System;
using System.Collections.Generic;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Артикул профиля.
    /// </summary>
    public class ProfileArticle : BaseDictionaryModel
    {
        public string Article { get; set; }
        
        public string FileName { get; set; }
        public double Size { get; set; }
        public double StepHeight { get; set; }
        
        public int ProfileSystemId { get; set; }
        public ProfileSystem System { get; set; }
        
        public int ProfileTypeId { get; set; }
        public ProfileType Type { get; set; }

        public int ApplicabilityId { get; set; }
        public Applicability Applicability { get; set; }
    }
}
