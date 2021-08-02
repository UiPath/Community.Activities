using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using UiPath.Database.Activities.Properties;

namespace UiPath.Database.Activities
{
    public class ConnectionHelper
    {
        public static void ConnectionValidation(DatabaseConnection existingConnection, SecureString connSecureString = null, string connString = null, string provName = null)
        {
            if (existingConnection == null && connString == null && connSecureString == null && provName == null)
            {
                throw new ArgumentNullException(Resources.ValidationError_ConnectionMustNotBeNull);
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
                if (connString == null && existingConnection == null)
                {
                    throw new ArgumentNullException(Resources.ValidationError_ConnectionStringMustNotBeNull);
                }
            }
        }
    }
}
