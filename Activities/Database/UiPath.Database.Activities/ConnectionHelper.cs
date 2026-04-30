using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class ConnectionHelper
    {
        /// <summary>Rethrows the exception unless continueOnError is true.</summary>
        public static void HandleException(Exception ex, bool continueOnError)
        {
            if (continueOnError) return;
            throw ex;
        }

        /// <summary>Resolves activity Parameters into a typed dictionary using the current context.</summary>
        public static Dictionary<string, ParameterInfo> BuildParameters(Dictionary<string, Argument> activityParameters, AsyncCodeActivityContext context)
        {
            if (activityParameters == null) return null;
            var parameters = new Dictionary<string, ParameterInfo>();
            foreach (var param in activityParameters)
            {
                parameters.Add(param.Key, new ParameterInfo() { Value = param.Value.Get(context), Direction = param.Value.Direction, Type = param.Value.ArgumentType });
            }
            return parameters;
        }

        /// <summary>Writes Out/InOut parameter values back to the activity's argument bindings.</summary>
        public static void SetOutputParameters(AsyncCodeActivityContext context, Dictionary<string, Argument> activityParameters, Dictionary<string, ParameterInfo> parametersBind)
        {
            foreach (var param in parametersBind)
            {
                var currentParam = activityParameters[param.Key];
                if (currentParam.Direction == ArgumentDirection.Out || currentParam.Direction == ArgumentDirection.InOut)
                {
                    currentParam.Set(context, param.Value.Value);
                }
            }
        }

        /// <summary>Ensures the DbConnection is initialised, creating one from the connection string if needed.</summary>
        public static DatabaseConnection EnsureConnection(DatabaseConnection dbConnection, string connString, SecureString connSecureString, string provName)
        {
            if (dbConnection != null) return dbConnection;
            return new DatabaseConnection().Initialize(connString ?? new NetworkCredential("", connSecureString).Password, provName);
        }


        public static void ConnectionValidation(DatabaseConnection existingConnection, SecureString connSecureString = null, string connString = null, string provName = null)
        {
            if (existingConnection == null && connString == null && connSecureString == null && provName == null)
            {
                throw new ArgumentNullException(Resources.ValidationError_ConnectionNotValid);
            }
            if (existingConnection != null && (provName != null || connString != null || connSecureString != null))
            {
                throw new ArgumentException(Resources.ValidationError_ConnectionMustBeSet);
            }
            if (existingConnection == null)
            {
                if (provName == null)
                {
                    throw new ArgumentNullException(Resources.ValidationError_ProviderNull);
                }
                if (connString != null && connSecureString != null)
                {
                    throw new ArgumentException(Resources.ValidationError_ConnectionStringMustBeSet);
                }
                if (connString == null && connSecureString == null)
                {
                    throw new ArgumentNullException(Resources.ValidationError_ConnectionStringMustNotBeNull);
                }
            }
        }
    }
}
