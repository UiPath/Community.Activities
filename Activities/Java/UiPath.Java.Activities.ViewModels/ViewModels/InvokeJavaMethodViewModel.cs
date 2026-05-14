using System.Activities.DesignViewModels;
using UiPath.Java;
using Resources = UiPath.Java.Activities.Properties.UiPath_Java_Activities;

namespace UiPath.Activities.Java.ViewModels
{
    /// <summary>
    /// ViewModel for the InvokeJavaMethod activity.
    /// </summary>
    class InvokeJavaMethodViewModel : DesignPropertiesViewModel
    {
        public DesignInArgument<string> MethodName { get; set; }
        public DesignInArgument<JavaObject> TargetObject { get; set; }
        public DesignInArgument<string> TargetType { get; set; }
        public DesignOutArgument<JavaObject> Result { get; set; }

        public InvokeJavaMethodViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            MethodName.OrderIndex = orderIndex++;
            TargetObject.OrderIndex = orderIndex++;
            TargetType.OrderIndex = orderIndex++;
            Result.OrderIndex = orderIndex++;

            MethodName.DisplayName = Resources.MethodNameDisplayName;
            MethodName.Tooltip = Resources.MethodNameDescription;
            MethodName.Category = Resources.Input;
            MethodName.IsRequired = true;
            MethodName.IsPrincipal = true;

            TargetObject.DisplayName = Resources.TargetObjectDisplayName;
            TargetObject.Tooltip = Resources.TargetObjectDescription;
            TargetObject.Category = Resources.Target;
            TargetObject.IsPrincipal = true;

            TargetType.DisplayName = Resources.TargetTypeDisplayName;
            TargetType.Tooltip = Resources.TargetTypeDescription;
            TargetType.Category = Resources.Target;
            TargetType.IsPrincipal = true;

            Result.DisplayName = Resources.ResultDisplayName;
            Result.Tooltip = Resources.JavaObjectDescription;
            Result.Category = Resources.Output;
        }
    }
}
