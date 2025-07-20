using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace UiPath.Cryptography.Activities.Tests
{
    public class TestingHelper
    {
        public static SecureString StringToSecureString(string s)
        {
            return new NetworkCredential("", s).SecurePassword;
        }
        public static string SecureStringToString(SecureString s)
        {
            return new NetworkCredential("", s).Password;
        }
    }
}
