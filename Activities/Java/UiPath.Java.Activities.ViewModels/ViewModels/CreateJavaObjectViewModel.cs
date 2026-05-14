using System.Activities.DesignViewModels;
using UiPath.Java;
using Resources = UiPath.Java.Activities.Properties.UiPath_Java_Activities;

namespace UiPath.Activities.Java.ViewModels
{
    /// <summary>
    /// ViewModel for the CreateJavaObject activity.
    /// </summary>
    class CreateJavaObjectViewModel : DesignPropertiesViewModel
    {
        public DesignInArgument<string> TargetType { get; set; }
        public DesignOutArgument<JavaObject> Result { get; set; }

        public CreateJavaObjectViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            TargetType.OrderIndex = orderIndex++;
            Result.OrderIndex = orderIndex++;

            TargetType.DisplayName = Resources.TargetTypeDisplayName;
            TargetType.Tooltip = Resources.TargetTypeDescription;
            TargetType.Category = Resources.Target;
            TargetType.IsRequired = true;
            TargetType.IsPrincipal = true;

            Result.DisplayName = Resources.ResultDisplayName;
            Result.Tooltip = Resources.JavaObjectDescription;
            Result.Category = Resources.Output;
        }
    }
}
