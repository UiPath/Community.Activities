using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UiPath.Java.Properties;

namespace UiPath.Java.Service
{
    [DataContract]
    internal class JavaObjectInstance
    {
        #region Private Members

        private object _value;

        #endregion

        #region Data Members

        [DataMember(Name = "value", EmitDefaultValue = false)]
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                SetValue(value);

            }
        }

        [DataMember(Name = "runtime_type", EmitDefaultValue = false)]
        public string RunTimeType { get; set; }

        [DataMember(Name = "reference_id", EmitDefaultValue = false)]
        public string ReferenceId { get; set; }

        #endregion

        #region Set Value Private Method

        /// <summary>
        /// Unwraps the passed as an IEnumerable of ObjectInstace and sets it to _value.
        /// Sets type Type to 'Array' if the argument is an IEnumerable, otherwise the type is the type of the raw object.
        /// </summary>
        /// <param name="value"></param>
        private void SetValue(object value)
        {
            if (value is IEnumerable ie && !(value is string))
            {
                List<object> items = ie.Cast<object>().ToList();
                _value = items?.Select(v =>
                {
                    if (v is JavaObject jo)
                    {
                        return jo.Instance;
                    }
                    else if (v is JavaObjectInstance joi)
                    {
                        return joi;
                    }
                    else
                    {
                        return new JavaObjectInstance() { Value = v };
                    }
                }).ToList();
                RunTimeType = "Array";
            }
            else
            {
                _value = value;
                RunTimeType = RunTimeType ?? _value?.GetType()?.ToString();
            }
        }

        #endregion

        #region Convert Object Public Method

        public object ConvertTo(Type convertType)
        {
            if (RunTimeType == "JavaOjbect")
            {
                throw new InvalidOperationException(Resources.CastException);
            }

            if (_value is IEnumerable ie && convertType.IsArray && RunTimeType == "Array")
            {
                List<JavaObjectInstance> items = ie.Cast<JavaObjectInstance>().ToList();
                var elementType = convertType.GetElementType();
                var result = Array.CreateInstance(elementType, items.Count);

                for (var i = 0; i < items.Count; ++i)
                {
                    result.SetValue(items[i].ConvertTo(elementType), i);
                }

                return result;
            }
            try
            {
                return Convert.ChangeType(_value, convertType);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(Resources.CastException, e);
            }
        }

        #endregion

    }
}
