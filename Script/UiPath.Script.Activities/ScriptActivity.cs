using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Markup;

namespace UiPath.Script.Activities
{
    public abstract class ScriptActivity<T> : AsyncCodeActivity<T>
    {
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> ScriptPath { get; set; }

        [Category("Input")]
        public InArgument<string> FunctionName { get; set; }

        [Category("Input")]
        [DependsOn("FunctionName")]
        public List<InArgument<string>> Parameters { get; protected set; } = new List<InArgument<string>>();

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            // index parameters
            int index = 1;
            foreach (var item in Parameters)
            {
                string name = "param" + index++;
                var runtimeArg = new RuntimeArgument(name, typeof(string), ArgumentDirection.In);
                metadata.Bind(item, runtimeArg);
                metadata.AddArgument(runtimeArg);
            }

            if(FunctionName == null && Parameters.Count > 0)
            {
                metadata.AddValidationError("The function name must be specified if parameters are passed.");
            }
        }
    }
}
