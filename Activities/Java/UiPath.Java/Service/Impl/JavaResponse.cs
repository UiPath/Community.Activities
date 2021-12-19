using System;
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
    internal class JavaResponse
    {

        #region Data Members

        [DataMember(Name = "result_state")]
        public string ResultStateString
        {
            get
            {
                return Enum.GetName(typeof(ResultState), this.ResultState);
            }

            set
            {
                this.ResultState = (ResultState) Enum.Parse(typeof(ResultState), value);
            }
        }

        public ResultState ResultState { get; set; }


        [DataMember (Name = "result")]
        public JavaObjectInstance Result { get; set; }

        [DataMember (Name = "errors")]
        public List<string> Errors { get; set; }

        [DataMember (Name = "execution_errors")]
        public List<string> ExecutionErrors { get; set; }

        #endregion

        #region Deserialize Method

        public static JavaResponse Deserialize(string json)
        {
            try
            {
                using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(JavaResponse), new List<Type> { typeof(JavaObjectInstance), typeof(List<JavaObjectInstance>) });
                    return serializer.ReadObject(memoryStream) as JavaResponse;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"Deserialization error: {e}");
                throw new InvalidOperationException(Resources.DeserializationException, e);
            }
        }

        #endregion

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
                    case (ResultState.JarNotLoaded):
                        excetpionMessageBuilder.AppendLine(Resources.JarNotLoadedException);
                        break;
                    case (ResultState.JarAlreadyLoaded):
                        excetpionMessageBuilder.AppendLine(Resources.JarAlreadyLoadedException);
                        break;
                    case (ResultState.JarNotFound):
                        excetpionMessageBuilder.AppendLine(Resources.JarNotFoundException);
                        break;
                    case (ResultState.ClassNotFound):
                        excetpionMessageBuilder.AppendLine(Resources.ClassNotFoundException);
                        break;
                    case (ResultState.ConstructorNotFound):
                        excetpionMessageBuilder.AppendLine(Resources.ConstructorNotFoundException);
                        break;
                    case (ResultState.InstanceNotFound):
                        excetpionMessageBuilder.AppendLine(Resources.InstanceNotFoundException);
                        break;
                    case (ResultState.InstantiationException):
                        excetpionMessageBuilder.AppendLine(Resources.InstantiationException);
                        break;
                    case (ResultState.IllegalAccess):
                        excetpionMessageBuilder.AppendLine(Resources.IllegalAccessException);
                        break;
                    case (ResultState.IllegalArguments):
                        excetpionMessageBuilder.AppendLine(Resources.IllegalArgumentsException);
                        break;
                    case (ResultState.InvocationTarget):
                        excetpionMessageBuilder.AppendLine(Resources.InvocationTargetException);
                        break;
                    case (ResultState.MethodNotFound):
                        excetpionMessageBuilder.AppendLine(Resources.MethodNotFoundException);
                        break;
                    case (ResultState.FieldNotFound):
                        excetpionMessageBuilder.AppendLine(Resources.FieldNotFoundException);
                        break;
                    case (ResultState.UnknownException):
                        excetpionMessageBuilder.AppendLine(Resources.UnknowException);
                        break;
                }
                if (ExecutionErrors?.Count > 0)
                {
                    excetpionMessageBuilder.AppendLine("Invocation target exceptions:");
                    excetpionMessageBuilder.AppendLine(GetErrorsAsText(ExecutionErrors));
                }

                if (Errors?.Count > 0)
                {
                    excetpionMessageBuilder.AppendLine("Java Invoker program exception:");
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

        #endregion

    }
}
