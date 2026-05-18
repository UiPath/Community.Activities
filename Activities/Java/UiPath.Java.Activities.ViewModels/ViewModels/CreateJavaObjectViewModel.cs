using System.Activities;
using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Collections.Generic;
using UiPath.Java;
using UiPath.Shared.ViewModels.Helpers;
using Resources = UiPath.Java.Activities.Properties.UiPath_Java_Activities;

namespace UiPath.Activities.Java.ViewModels
{
    /// <summary>
    /// ViewModel for the CreateJavaObject activity.
    /// </summary>
    class CreateJavaObjectViewModel : DesignPropertiesViewModel
    {
        public DesignInArgument<string> TargetType { get; set; }
        public DesignProperty<List<InArgument>> Parameters { get; set; }
        public DesignInArgument<List<object>> ParametersList { get; set; }
        public DesignOutArgument<JavaObject> Result { get; set; }

        private DesignPropertyToggle<DesignProperty<List<InArgument>>, DesignInArgument<List<object>>> _parametersToggle;

        public CreateJavaObjectViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            _parametersToggle = new DesignPropertyToggle<DesignProperty<List<InArgument>>, DesignInArgument<List<object>>>(Parameters, ParametersList);
            _parametersToggle.Initialize(showFirst: ParametersList.Value == null);

            PersistValuesChangedDuringInit();

            var orderIndex = 0;
            TargetType.OrderIndex = orderIndex++;
            Parameters.OrderIndex = orderIndex++;
            ParametersList.OrderIndex = orderIndex++;
            Result.OrderIndex = orderIndex++;

            TargetType.DisplayName = Resources.TargetTypeDisplayName;
            TargetType.Tooltip = Resources.TargetTypeDescription;
            TargetType.Category = Resources.Target;
            TargetType.IsRequired = true;
            TargetType.IsPrincipal = true;

            Parameters.DisplayName = Resources.ParametersDisplayName;
            Parameters.Tooltip = Resources.ParametersDescription;
            Parameters.Category = Resources.Input;

            ParametersList.DisplayName = Resources.ParametersListDisplayName;
            ParametersList.Category = Resources.Input;

            Result.DisplayName = Resources.ResultDisplayName;
            Result.Tooltip = Resources.JavaObjectDescription;
            Result.Category = Resources.Output;

            Parameters.AddMenuAction(new MenuAction
            {
                DisplayName = Resources.MenuAction_UseAnExpression,
                IsMain = true,
                Handler = (_) => _parametersToggle.ShowSecond()
            });
            ParametersList.AddMenuAction(new MenuAction
            {
                DisplayName = Resources.MenuAction_UseStaticNames,
                IsMain = true,
                Handler = (_) => _parametersToggle.ShowFirst()
            });
        }
    }
}
