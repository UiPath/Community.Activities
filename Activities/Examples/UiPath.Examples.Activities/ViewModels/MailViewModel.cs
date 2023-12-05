using System.Activities.DesignViewModels;

namespace UiPath.Examples.Activities.ViewModels
{
    public class MailViewModel : IntegrationActivityBaseViewModel
    {
        public DesignOutArgument<string> Result { get; set; }

        public MailViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            base.InitializeModel();

            Result.DisplayName = Resources.Calculator_Result_DisplayName;
            Result.Tooltip = Resources.Calculator_Result_Tooltip;
            Result.OrderIndex = 100;
        }
    }
}
