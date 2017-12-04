using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;

namespace Instant_Chat
{
    public class onlineStatus : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value.ToString().Equals("1"))
            {
                BitmapImage empImage = new BitmapImage(new Uri("/Icons/online.png", UriKind.RelativeOrAbsolute));
                return empImage;
            }
            else
            {
                BitmapImage empImage = new BitmapImage(new Uri("/Icons/offline.png", UriKind.RelativeOrAbsolute));
                return empImage;
            }
        }
 
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
