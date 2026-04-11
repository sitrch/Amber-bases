using System;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Контакты заказчика.
    /// </summary>
    public class CustomerContact : BaseDictionaryModel
    {
        public int CustomerId { get; set; } // В БД FBase это поле "id_Заказчики"
        public string JobTitle { get; set; } // Должность
        public string LastName { get; set; } // Фамилия
        public string FirstName { get; set; } // Имя
        public string MiddleName { get; set; } // Отчество
        public string PhoneWork1 { get; set; } // ТелефонРабочий
        public string PhoneWork2 { get; set; } // ТелефонРабочий2
        public string PhoneWork3 { get; set; } // ТелефонРабочий3
        public string PhoneHome { get; set; } // ТелефонДомашний
    }
}