using System;
using System.Globalization;
using System.Windows.Data;

namespace AmberBases.UI
{
    /// <summary>
    /// Конвертер для инвертирования булевых значений.
    /// Используется для блокировки текстового поля при включенном чекбоксе связи.
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Преобразует true в false и false в true.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        /// <summary>
        /// Обратное преобразование (также инвертирует значение).
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }
}