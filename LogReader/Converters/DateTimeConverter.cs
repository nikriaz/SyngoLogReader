using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LogReader.Converters
{
    /// <summary>
    /// In Date and Time pickers both return both date and time but Time picker does not change previosuly set date
    /// but Date picker does change time to default. We need this converter to hold time for Date picker.
    /// </summary>
    class DateTimeConverter : IValueConverter
    {
        private TimeSpan _fromTime;
        private TimeSpan _toTime;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((string)parameter)
            {
                case "from":
                    _fromTime = ((DateTime)value).TimeOfDay;
                    break;
                case "to":
                    _toTime = ((DateTime)value).TimeOfDay;
                    break;
            }
            
            return value;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            DateTime returnValue = default(DateTime);

            switch ((string)parameter)
            {
                case "from":
                    returnValue = ((DateTime)value).Date + _fromTime;
                    break;
                case "to":
                    returnValue = ((DateTime)value).Date + _toTime;
                    break;
            }

            return returnValue;
        }

    }
}
