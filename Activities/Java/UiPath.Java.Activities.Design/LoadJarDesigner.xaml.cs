using res = UiPath.Java.Activities.Design.Properties.Resources;

namespace UiPath.Java.Activities.Design
{
    // Interaction logic for InvokeJavaMethodDesigner.xaml
    public partial class LoadJarDesigner
    {

        public string JarFilter
        {
            get
            {
                return res.JavaExcecutableFile + "|*.jar";
            }
        }

        public LoadJarDesigner()
        {
            InitializeComponent();
        }
    }
}
