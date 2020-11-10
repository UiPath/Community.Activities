using System;

namespace UiPath.Python
{
    /// <summary>
    /// class wrapping a Python object
    /// </summary>
    public class PythonObject : IDisposable
    {
        internal dynamic PyObject { get; private set; }

        public Guid Id { get; set; }

        internal PythonObject(object pyObject)
        {
            PyObject = pyObject;
        }

        internal PythonObject(Guid id)
        {
            Id = id;
        }

        internal object AsManagedType(Type t )
        {
            return PyObject.AsManagedObject(t);
        }

        private void DisposePythonObject()
        {
            //Pythonnet added a check on refcount and when RefCount==1 sometimes throws a Memory access violation error from Python engine, validation that was not in previous pythonnet versions
            //
            if(PyObject?.Refcount != 1)
                PyObject?.Dispose();
           
            PyObject = null;
        }

        public void Dispose()
        {
            DisposePythonObject();
            GC.SuppressFinalize(this);
        }

        ~PythonObject()
        {
            DisposePythonObject();
        }
    }
}
