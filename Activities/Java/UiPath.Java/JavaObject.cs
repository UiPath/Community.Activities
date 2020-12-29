using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.Java.Service;

namespace UiPath.Java
{
    public class JavaObject
    {
        #region Instance Property

        internal JavaObjectInstance Instance { get; set; }

        #endregion

        #region Internal Constructor

        internal JavaObject() { }

        #endregion

        #region Convert Method

        public T Convert<T>()
        {
            return (T)Instance.ConvertTo(typeof(T));
        }

        #endregion 

        public bool IsNull()
        {
            return Instance == null;
        }

    }
}
