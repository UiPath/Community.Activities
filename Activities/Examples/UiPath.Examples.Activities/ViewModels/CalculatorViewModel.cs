using System.Activities.DesignViewModels;
using System.Diagnostics;

namespace UiPath.Examples.Activities.ViewModels
{
    public class CalculatorViewModel : DesignPropertiesViewModel
    {
        /*
         * Properties names must match the names and generic type arguments of the properties in the activity
         * Use DesignInArgument for properties that accept a variable
         */
        public DesignInArgument<int> FirstNumber { get; set; }
        public DesignInArgument<int> SecondNumber { get; set; }
        /*
         * Use DesignProperty for properties that accept a constant value                
         */
        public DesignProperty<Operation> SelectedOperation { get; set; }
        /*
         * The result property comes from the activity's base class
         */
        public DesignOutArgument<int> Result { get; set; }

        public CalculatorViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            Debugger.Break();
            /*
             * The base call will initialize the properties of the view model with the values from the xaml or with the default values from the activity
             */
            base.InitializeModel();

            PersistValuesChangedDuringInit(); // just for heads-up here; it's a mandatory call only when you change the values of properties during initialization

            var orderIndex = 0;

            FirstNumber.DisplayName = Resources.Calculator_FirstNumber_DisplayName;
            FirstNumber.Tooltip = Resources.Calculator_FirstNumber_Tooltip;
            /*
             * Required fields will automatically raise validation errors when empty.
             * Unless you do custom validation, required activity properties should be marked as such both in the view model and in the activity:
             *   -> in the view model use the IsRequired property
             *   -> in the activity use the [RequiredArgument] attribute.
             */
            FirstNumber.IsRequired = true;

            FirstNumber.IsPrincipal = true; // specifies if it belongs to the main category (which cannot be collapsed)
            FirstNumber.OrderIndex = orderIndex++; // indicates the order in which the fields appear in the designer (i.e. the line number);

            SecondNumber.DisplayName = Resources.Calculator_SecondNumber_DisplayName;
            SecondNumber.Tooltip = Resources.Calculator_SecondNumber_Tooltip;
            SecondNumber.IsRequired = true;
            SecondNumber.IsPrincipal = true;
            SecondNumber.OrderIndex = orderIndex++;

            SelectedOperation.DisplayName = Resources.Calculator_SelectedOperation_DisplayName;
            SelectedOperation.Tooltip = Resources.Calculator_SelectedOperation_Tooltip;
            SelectedOperation.IsRequired = true;
            SelectedOperation.IsPrincipal = true;
            SelectedOperation.OrderIndex = orderIndex++;

            /*
             * Output properties are never mandatory.
             * By convention, they are not principal and they are placed at the end of the activity.
             */
            Result.DisplayName = Resources.Calculator_Result_DisplayName;
            Result.Tooltip = Resources.Calculator_Result_Tooltip;
            Result.OrderIndex = orderIndex;
        }
    }
}
