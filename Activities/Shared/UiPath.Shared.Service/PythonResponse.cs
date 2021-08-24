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
    internal class PythonResponse
    {
        #region Data Members

        [DataMember(Name = "Guid", EmitDefaultValue = false)]
        public Guid Guid { get; set; }

        [DataMember(Name = "argument", EmitDefaultValue = false)]
        public Argument Argument { get; set; }

        [DataMember(Name = "result_state", EmitDefaultValue = false)]
        public string ResultStateString
        {
            get
            {
                return Enum.GetName(typeof(ResultState), this.ResultState);
            }

            set
            {
                this.ResultState = (ResultState)Enum.Parse(typeof(ResultState), value);
            }
        }

        public ResultState ResultState { get; set; }

        [DataMember(Name = "errors", EmitDefaultValue = false)]
        public List<string> Errors { get; set; }

        [DataMember(Name = "execution_errors", EmitDefaultValue = false)]
        public List<string> ExecutionErrors { get; set; }

        #endregion Data Members

        #region Deserialize Method

        public static PythonResponse Deserialize(string json)
        {
            try
            {
                using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(PythonResponse));
                    return serializer.ReadObject(memoryStream) as PythonResponse;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Deserialization error: {e}");
                throw new InvalidOperationException(UiPath_Python.DeserializationException, e);
            }
        }

        public string Serialize()
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(PythonResponse));
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

        #endregion Deserialize Method

        #region Throw Exception Methods

        /// <summary>
        /// Checks the result state and throws an aggregate exceptions if the state is not success.
        /// The exce
        /// </summary>
        public void ThrowExceptionIfNeeded()
        {
            var excetpionMessageBuilder = new StringBuilder();
            if (ResultState != ResultState.Successful)
            {
                switch (ResultState)
                {
                    case (ResultState.InstantiationException):
                        excetpionMessageBuilder.AppendLine(UiPath_Python.InstantiationException);
                        break;
                }
                if (ExecutionErrors?.Count > 0)
                {
                    excetpionMessageBuilder.AppendLine("Invocation target exceptions:");
                    excetpionMessageBuilder.AppendLine(GetErrorsAsText(ExecutionErrors));
                }

                if (Errors?.Count > 0)
                {
                    excetpionMessageBuilder.AppendLine("Python Invoker program exception:");
                    excetpionMessageBuilder.Append(GetErrorsAsText(Errors));
                }
                throw new InvalidOperationException(excetpionMessageBuilder.ToString());
            }
        }

        private string GetErrorsAsText(List<string> errors)
        {
            var sb = new StringBuilder();
            foreach (var error in errors)
            {
                sb.AppendLine(error);
            }
            return sb.ToString();
        }

        #endregion Throw Exception Methods
    }
}