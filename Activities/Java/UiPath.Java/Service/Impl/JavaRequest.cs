using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using UiPath.Java.Properties;

namespace UiPath.Java.Service
{
    [DataContract]
    [KnownType(typeof(JavaObjectInstance))]
    internal class JavaRequest
    {

        #region Data Members

        [DataMember(Name = "method_name", EmitDefaultValue = false)]
        public string MethodName { get; set; }

        [DataMember(Name = "class_name", EmitDefaultValue = false)]
        public string ClassName { get; set; }

        [DataMember(Name = "instance", EmitDefaultValue = false)]
        public JavaObjectInstance Instance { get; set; }

        [DataMember(Name = "jar_path", EmitDefaultValue = false)]
        public string JarPath { get; set; }

        [DataMember(Name = "field_name", EmitDefaultValue = false)]
        public string FieldName { get; set; }

        [DataMember(Name = "parameters", EmitDefaultValue = false)]
        public List<JavaObjectInstance> Parameters { get; private set; } = new List<JavaObjectInstance>();

        [DataMember(Name = "request_type")]
        public string RequestTypeString
        {
            get
            {
                return Enum.GetName(typeof(RequestType), this.RequestType);
            }
            set
            {
                this.RequestType = (RequestType)Enum.Parse(typeof(RequestType), value);
            }
        }

        public RequestType RequestType { get; set; }


        #endregion

        #region Add Parameters Method

        public void AddParametersToRequest(List<object> parameters, List<Type> types = null)
        {
            if (parameters == null)
            {
                return;
            }
            int index = -1;
            foreach (object param in parameters)
            {
                index++;
                if (param is JavaObject jo)
                {
                    Parameters.Add(jo.Instance);
                }
                else
                {
                    if (param==null)
                    {
                        var type = types[index];
                        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
                        {
                            Parameters.Add(new JavaObjectInstance
                            {
                                Value = param,
                                RunTimeType = "Array",
                                RunTimeArrayType = type.FullName
                            }); 
                        }
                        else
                        {
                            Parameters.Add(new JavaObjectInstance
                            {
                                Value = param,
                                RunTimeType = type.FullName
                            });
                        }
                    }
                    else
                    {
                        Parameters.Add(new JavaObjectInstance
                        {
                            Value = param
                        });
                    }
                    
                }
            }
        }

        #endregion

        #region Serialize Method

        public string Serialize()
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(JavaRequest), new List<Type> { typeof(JavaObjectInstance), typeof(List<JavaObjectInstance>) });
                using (var memoryStream = new MemoryStream())
                {
                    serializer.WriteObject(memoryStream, this);
                    byte[] json = memoryStream.ToArray();
                    return Encoding.UTF8.GetString(json, 0, json.Length);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw new InvalidOperationException(Resources.SerializationException, e);
            }
        }

        #endregion

    }
}
