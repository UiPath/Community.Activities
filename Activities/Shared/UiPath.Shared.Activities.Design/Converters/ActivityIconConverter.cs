using System;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UiPath.Activities.Presentation.Converters
{
    public class ActivityIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null)
                {
                    return null;
                }
                Type activityType = (value as ModelItem).ItemType;
                string resourceName = activityType.Name;

                if (activityType.IsGenericType)
                {
                    resourceName = resourceName.Split('`')[0];
                }
                resourceName += "Icon";

                var iconsSource = new ResourceDictionary { Source = new Uri(parameter as string) };

                var icon = iconsSource[resourceName] as DrawingBrush;
                if (icon == null)
                {
                    icon = Application.Current.Resources[resourceName] as DrawingBrush;
                }
                if (icon == null)
                {
                    icon = Application.Current.Resources["GenericLeafActivityIcon"] as DrawingBrush;
                }

                return icon.Drawing;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}