using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PhotoPreparation.Helpers
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                return timeSpan.ToString(@"hh\:mm");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                if (TimeSpan.TryParseExact(strValue, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan result))
                {
                    return result;
                }
            }
            throw new ArgumentException("Invalid time format");
            //return TimeSpan.Zero;
        }
    }
}
