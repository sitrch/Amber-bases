using System;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Заказчик.
    /// </summary>
    public class Customer : BaseDictionaryModel
    {
        public string Name { get; set; } // В БД FBase это поле "Заказчик"
        public string Address { get; set; }
    }
}