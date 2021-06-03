using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace LogReader.Converters
{
    class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = new SolidColorBrush();
            var severity = value.ToString().ToLower();
            switch (severity)
            {
                case "e":
                    color = Brushes.DarkRed;
                    break;
                case "w":
                    color = Brushes.DarkBlue;
                    break;
                case "s":
                    color = Brushes.DarkGreen;
                    break;
                default:
                    color = Brushes.DimGray;
                    break;
            }

            return color;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
