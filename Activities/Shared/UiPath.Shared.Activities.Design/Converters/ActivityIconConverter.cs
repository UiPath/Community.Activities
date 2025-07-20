using System;
using System.Activities.Presentation.Model;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UiPath.Activities.Presentation.Converters
{
    public class ActivityIconConverter : IValueConverter
    {
        private const string IconsUri = "pack://application:,,,/{0};component/Themes/Icons.xaml";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null)
                {
                    return null;
                }

                GetResourceInfoFromParam(out var resourceName, out var source);

                DrawingBrush icon = GetDrawingBrushFromCustomDictionary(source, resourceName);
                if (icon == null)
                {
                    icon = Application.Current.Resources[resourceName] as DrawingBrush;
                }
                if (icon == null)
                {
                    icon = Application.Current.Resources["GenericLeafActivityIcon"] as DrawingBrush;
                }

                return icon?.Drawing;
            }
            catch
            {
                return null;
            }

            void GetResourceInfoFromParam(out string resourceName, out string sourceUri)
            {
                Type activityType = (value as ModelItem).ItemType;
                resourceName = activityType.Name;

                if (activityType.IsGenericType)
                {
                    resourceName = resourceName.Split('`')[0];
                }
                resourceName += "Icon";

                sourceUri = GetDefaultResource();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static DrawingBrush GetDrawingBrushFromCustomDictionary(object source, string resourceName)
        {
            var sourceUri = source as string;
            if (sourceUri == null)
                return null;
            try
            {
                var iconsSource = new ResourceDictionary { Source = new Uri(sourceUri, UriKind.Absolute) };
                return iconsSource[resourceName] as DrawingBrush;
            }
            catch
            {
                //just default to null
                return null;
            }
        }

        private static string GetDefaultResource()
        {
            return string.Format(IconsUri, typeof(ActivityIconConverter).Assembly.FullName);
        }
    }
}