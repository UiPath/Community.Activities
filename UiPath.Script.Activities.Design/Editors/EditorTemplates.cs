using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UiPath.Script.Activities.Design.Editors
{
    public partial class EditorTemplates
    {
        static ResourceDictionary resources;

        internal static ResourceDictionary ResourceDictionary
        {
            get
            {
                if (resources == null)
                {
                    resources = new EditorTemplates();
                }

                return resources;
            }
        }

        public EditorTemplates()
        {
            InitializeComponent();
        }
    }
}
