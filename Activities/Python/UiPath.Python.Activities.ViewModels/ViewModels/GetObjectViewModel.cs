using System;
using System.Activities.ViewModels;
using System.Activities.ViewModels.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using System.Activities.DesignViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Studio.Activities.Api;
using UiPath.Python;
using Resources = UiPath.Python.Activities.Properties.UiPath_Python_Activities;

namespace UiPath.Activities.Python.ViewModels
{
    [ExcludeFromCodeCoverage]
    class GetObjectViewModel<T> : DesignPropertiesViewModel
    {
        private bool _typeWidgetAvailable;
        private readonly IWorkflowDesignApi _workflowDesignApi;
        private IDesignerStaticTypesService _morphingService;

        public DesignInArgument<PythonObject> PythonObject { get; set; }

        [NotMappedProperty]
        public DesignProperty<Type> InputType { get; set; }

        public DesignOutArgument<T> Result { get; set; }

        public GetObjectViewModel(IDesignServices services) : base(services)
        {
            _workflowDesignApi = services.GetService<IWorkflowDesignApi>();
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            var orderIndex = 0;
            PythonObject.OrderIndex = orderIndex++;
            InputType.OrderIndex = orderIndex++;
            Result.OrderIndex = orderIndex++;

            PythonObject.DisplayName = Resources.PythonObjectNameDisplayName;
            PythonObject.Tooltip = Resources.PythonObjectDescription;
            PythonObject.Category = Resources.Input;
            PythonObject.IsRequired = true;
            PythonObject.IsPrincipal = true;

            InputType.Category = Resources.Input;
            // hide the property until we know if the type picker widget is available
            InputType.IsVisible = false;

            Result.DisplayName = Resources.ResultNameDisplayName;
            Result.Tooltip = Resources.GetObjectResultDescription;
            Result.Category = Resources.Output;

            if (_workflowDesignApi is null)
                return;

            _morphingService = Services.GetService<IDesignerStaticTypesService>();

            if (_workflowDesignApi.HasFeature(DesignFeatureKeys.WidgetSupportInfoService))
            {
                AddTypePicker();
            }
        }

        private void AddTypePicker()
        {
            if (_morphingService is null)
                return;

            var typePicker = ViewModelWidgetType.TypePicker;
            if (_workflowDesignApi.WidgetSupportInfoService?.IsWidgetSupported(typePicker) != true)
                return;

            _typeWidgetAvailable = true;

            InputType.IsVisible = _typeWidgetAvailable;
            InputType.Widget = new DefaultWidget() { Type = typePicker };
            InputType.Value = ModelItem.ItemType.GenericTypeArguments.Single();
        }

        protected override void ManualRegisterDependencies()
        {
            base.ManualRegisterDependencies();

            if (_typeWidgetAvailable)
                RegisterDependency(InputType, nameof(InputType.Value), nameof(InputType));
        }

        protected override void InitializeRules()
        {
            base.InitializeRules();

            if (_typeWidgetAvailable)
                Rule(nameof(InputType), MorphActivityAsync, false);
        }

        private async Task MorphActivityAsync()
        {
            if (!InputType.HasValue)
                return;

            if (InputType.Value == ModelItem.ItemType.GenericTypeArguments.Single())
                return;

            await _morphingService.UseTypeAsync(InputType.Value);
        }
    }
}
