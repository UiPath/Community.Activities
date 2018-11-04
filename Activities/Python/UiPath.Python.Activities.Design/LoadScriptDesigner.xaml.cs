namespace UiPath.Python.Activities.Design
{
    /// <summary>
    /// Interaction logic for LoadScriptDesigner.xaml
    /// </summary>
    public partial class LoadScriptDesigner
    {
        public string PythonScriptFilter
        {
            get
            {
                return Properties.Resources.PythonScriptFilter + "|*.py";
            }
        }
        public LoadScriptDesigner()
        {
            InitializeComponent();
        }
    }
}
