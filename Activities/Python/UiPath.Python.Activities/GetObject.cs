using System;
using System.Activities;
using System.Diagnostics;
using UiPath.Python.Activities.Properties;

namespace UiPath.Python.Activities
{
    /// <summary>
    /// Activity for extracting the .NET object from the Python type 
    /// </summary>
    [LocalizedDisplayName(nameof(Resources.GetObjectNameDisplayName))]
    [LocalizedDescription(nameof(Resources.GetObjectDescription))]
    public class GetObject<T> : PythonCodeActivity
    {
        [RequiredArgument]
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.PythonObjectNameDisplayName))]
        [LocalizedDescription(nameof(Resources.PythonObjectDescription))]
        public InArgument<PythonObject> PythonObject { get; set; }


        [LocalizedCategory(nameof(Resources.Output))]
        [LocalizedDisplayName(nameof(Resources.ResultNameDisplayName))]
        [LocalizedDescription(nameof(Resources.GetObjectResultDescription))]
        public OutArgument<T> Result { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            IEngine pythonEngine = PythonScope.GetPythonEngine(context);
            PythonObject pyObject = PythonObject.Get(context);
            if (null == pyObject)
            {
                throw new ArgumentNullException(nameof(PythonObject));
            }

            T result;
            try
            {
                result = (T)pythonEngine.Convert(pyObject, typeof(T));
            }
            catch (Exception e)
            {
                Trace.TraceError($"Error casting Python object: {e.ToString()}");
                throw new InvalidOperationException(Resources.ConvertException, e);
            }

            Result.Set(context, result);
        }
    }
}
