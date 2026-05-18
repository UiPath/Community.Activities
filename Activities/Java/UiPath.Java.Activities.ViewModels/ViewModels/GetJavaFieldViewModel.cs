using System.Activities.DesignViewModels;
using UiPath.Java;
using Resources = UiPath.Java.Activities.Properties.UiPath_Java_Activities;

namespace UiPath.Activities.Java.ViewModels
{
    /// <summary>
    /// ViewModel for the GetJavaField activity.
    /// </summary>
    class GetJavaFieldViewModel : DesignPropertiesViewModel
    {
        public DesignInArgument<string> FieldName { get; set; }
        public DesignInArgument<JavaObject> TargetObject { get; set; }
        public DesignInArgument<string> TargetType { get; set; }
        public DesignOutArgument<JavaObject> Result { get; set; }

        public GetJavaFieldViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            FieldName.OrderIndex = orderIndex++;
            TargetObject.OrderIndex = orderIndex++;
            TargetType.OrderIndex = orderIndex++;
            Result.OrderIndex = orderIndex;

            FieldName.DisplayName = Resources.FieldNameDisplayName;
            FieldName.Tooltip = Resources.FieldNameDescription;
            FieldName.Category = Resources.Input;
            FieldName.IsRequired = true;
            FieldName.IsPrincipal = true;

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
