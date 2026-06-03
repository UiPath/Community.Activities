using System.Activities.DesignViewModels;
using UiPath.Java;
using Resources = UiPath.Java.Activities.Properties.UiPath_Java_Activities;

namespace UiPath.Activities.Java.ViewModels
{
    /// <summary>
    /// ViewModel for the ConvertJavaObject activity.
    /// </summary>
    class ConvertJavaObjectViewModel<T> : DesignPropertiesViewModel
    {
        public DesignInArgument<JavaObject> JavaObject { get; set; }
        public DesignOutArgument<T> Result { get; set; }

        public ConvertJavaObjectViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            JavaObject.OrderIndex = orderIndex++;
            Result.OrderIndex = orderIndex;

            JavaObject.DisplayName = Resources.JavaObjectDisplayName;
            JavaObject.Tooltip = Resources.JavaObjectDescription;
            JavaObject.Category = Resources.Input;
            JavaObject.IsRequired = true;
            JavaObject.IsPrincipal = true;

            Result.DisplayName = Resources.ResultDisplayName;
            Result.Tooltip = Resources.ConvertJavaObjectResultDescription;
            Result.Category = Resources.Output;
        }
    }
}
