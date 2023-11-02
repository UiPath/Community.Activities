using System.Activities.DesignViewModels;

namespace UiPath.Activities.Template.ViewModels
{
    public class ActivityTemplateViewModel : DesignPropertiesViewModel
    {
        /*
         * The result property comes from the activity's base class
         */
        public DesignOutArgument<int> Result { get; set; }

        public ActivityTemplateViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            /*
             * The base call will initialize the properties of the view model with the values from the xaml or with the default values from the activity
             */
            base.InitializeModel();

            PersistValuesChangedDuringInit(); // mandatory call only when you change the values of properties during initialization
        }
    }
}
