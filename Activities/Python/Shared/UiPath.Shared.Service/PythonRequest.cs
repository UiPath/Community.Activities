using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using UiPath.Python.Properties;
using UiPath.Python.Service;

namespace UiPath.Shared.Service
{
    [DataContract]
    internal class PythonRequest
    {
        #region Data Members

        [DataMember(Name = "script_path", EmitDefaultValue = false)]
        public string ScriptPath { get; set; }

        [DataMember(Name = "library_path", EmitDefaultValue = false)]
        public string LibraryPath { get; set; }

        [DataMember(Name = "python_version", EmitDefaultValue = false)]
        public string PythonVersion { get; set; }

        [DataMember(Name = "working_folder", EmitDefaultValue = false)]
        public string WorkingFolder { get; set; }

        [DataMember(Name = "code", EmitDefaultValue = false)]
        public string Code { get; set; }

        [DataMember(Name = "instance", EmitDefaultValue = false)]
        public Guid Instance { get; set; }

        [DataMember(Name = "method", EmitDefaultValue = false)]
        public string Method { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "arguments", EmitDefaultValue = false)]
        public IEnumerable<Argument> Arguments { get; set; }

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

        #endregion Data Members

        #region Serialize Method

        public string Serialize()
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(PythonRequest));
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
                throw new InvalidOperationException(UiPath_Python.SerializationException, e);
            }
        }

        internal static PythonRequest Deserialize(string json)
        {
            try
            {
                using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(PythonRequest));
                    return serializer.ReadObject(memoryStream) as PythonRequest;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Deserialization error: {e}");
                throw new InvalidOperationException(UiPath_Python.DeserializationException, e);
            }
        }

        #endregion Serialize Method
    }
}