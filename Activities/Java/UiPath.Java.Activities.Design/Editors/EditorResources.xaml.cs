using System.Windows;

namespace UiPath.Java.Activities.Design
{

    public partial class EditorResources
    {

        private static ResourceDictionary s_resources;

        internal static ResourceDictionary GetResources()
        {
            if (s_resources == null)
            {
                s_resources = new EditorResources();
            }
            return s_resources;
        }

        public EditorResources()
        {
            InitializeComponent();
        }
    }
}
