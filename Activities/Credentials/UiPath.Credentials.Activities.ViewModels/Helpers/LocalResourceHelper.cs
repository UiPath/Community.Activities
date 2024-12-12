using System.Activities.DesignViewModels;
using System.Activities;

namespace UiPath.Activities.Credentials.ViewModels.Helpers
{
    //We need to reference System.Activities in order for the view models to work
    internal static class LocalResourceHelper
    {
        public static object GetNormalizedLocalPath(string updatedPropertyName, string targetPropertyName, object value)
        {
            if (targetPropertyName.Equals(updatedPropertyName))
            {
                var argument = value as InArgument<string>;
                return argument?.GetNormalizedPath();
            }
            return value;
        }

        public static InArgument<string> GetNormalizedPath(this InArgument<string> argument)
        {
            if (argument == null || argument.Expression == null || !argument.Expression.IsLiteral())
            {
                return argument;
            }

            string path = argument.Expression.ToString();
            return path.GetNormalizedPathInternal();
        }

        public static InArgument<string> GetNormalizedPath(this DesignInArgument<string> argument)
        {
            if (!argument.TryGetLiteralValue(out var value))
            {
                return argument.Value;
            }

            string path = (string)value;
            return path.GetNormalizedPathInternal();
        }

        private static InArgument<string> GetNormalizedPathInternal(this string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                path = path.Replace('\\', '/').Replace("//", "/");
            }
            return path;
        }
    }
}
