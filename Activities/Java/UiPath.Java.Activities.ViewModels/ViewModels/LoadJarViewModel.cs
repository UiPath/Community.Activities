using System.Activities.DesignViewModels;
using System.Threading.Tasks;
using Resources = UiPath.Java.Activities.Properties.UiPath_Java_Activities;

namespace UiPath.Activities.Java.ViewModels
{
    /// <summary>
    /// ViewModel for the LoadJar activity.
    /// </summary>
    class LoadJarViewModel : DesignPropertiesViewModel
    {
        public DesignInArgument<string> JarPath { get; set; }

        public LoadJarViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            JarPath.OrderIndex = orderIndex;

            JarPath.DisplayName = Resources.JarPathDisplayName;
            JarPath.Tooltip = Resources.JarPathDescription;
            JarPath.Category = Resources.Input;
            JarPath.IsRequired = true;
            JarPath.IsPrincipal = true;
            WidgetSupportHelper.ApplyLocalResourceWidget(JarPath, Services);
        }

        // Studio 25.10.1-25.10.14 ignore NoWrap and would store LocalResource.FromPath("...") in the string JarPath;
        // unwrap it and keep relative paths portable ('/').
        public override async ValueTask<UpdateViewModelResult> UpdateAsync(string propertyName, object value)
        {
            if (propertyName == nameof(JarPath))
            {
                value = LocalResourcePathNormalizer.Normalize(value);
            }

            return await base.UpdateAsync(propertyName, value);
        }
    }
}
