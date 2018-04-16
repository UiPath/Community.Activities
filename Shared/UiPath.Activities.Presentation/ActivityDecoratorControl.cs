using System.Windows;
using System.Windows.Controls;

namespace UiPath.Activities.Presentation
{
    public class ActivityDecoratorControl : ContentControl
    {
        static ActivityDecoratorControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ActivityDecoratorControl), new FrameworkPropertyMetadata(typeof(ActivityDecoratorControl)));
        }
    }
}