using System;
using AmberBases.Core.Models;

namespace AmberBases.Core.Models.Dictionaries
{
    /// <summary>
    /// Заказчик.
    /// </summary>
    public class Customer : BaseDictionaryModel
    {
        #region Основные свойства

        [ColumnDisplayName("Название")]
        public override string Name { get; set; }

        [ColumnDisplayName("Адрес")]
        public string Address { get; set; }

        #endregion
    }
}