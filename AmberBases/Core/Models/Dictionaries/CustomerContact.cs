using System;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Контакты заказчика.
    /// </summary>
    public class CustomerContact : BaseDictionaryModel
    {
        #region Служебные свойства

        [ColumnDisplayName("Клиент", false)]
        public int CustomerId { get; set; }

        #endregion

        #region Основные свойства

        [ColumnDisplayName("Должность")]
        public string JobTitle { get; set; }

        [ColumnDisplayName("Фамилия")]
        public string LastName { get; set; }

        [ColumnDisplayName("Имя")]
        public string FirstName { get; set; }

        [ColumnDisplayName("Отчество")]
        public string MiddleName { get; set; }

        [ColumnDisplayName("Телефон рабочий")]
        public string PhoneWork1 { get; set; }

        [ColumnDisplayName("Телефон рабочий 2")]
        public string PhoneWork2 { get; set; }

        [ColumnDisplayName("Телефон рабочий 3")]
        public string PhoneWork3 { get; set; }

        [ColumnDisplayName("Телефон домашний")]
        public string PhoneHome { get; set; }

        #endregion
    }
}