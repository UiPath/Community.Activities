namespace UiPath.Python.Activities.Design
{
    /// <summary>
    /// Interaction logic for RunScriptDesigner.xaml
    /// </summary>
    public partial class RunScriptDesigner
    {
        public string PythonScriptFilter
        {
            get
            {
                return Properties.Resources.PythonScriptFilter + "|*.py";
            }
        }

        public RunScriptDesigner()
        {
            InitializeComponent();
        }
    }
}
