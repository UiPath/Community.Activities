using System.Activities.DesignViewModels;
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
            JarPath.OrderIndex = orderIndex++;

            JarPath.DisplayName = Resources.JarPathDisplayName;
            JarPath.Tooltip = Resources.JarPathDescription;
            JarPath.Category = Resources.Input;
            JarPath.IsRequired = true;
            JarPath.IsPrincipal = true;
        }
    }
}
