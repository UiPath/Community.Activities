#if NET
using System.Activities.DesignViewModels;
#endif
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UiPath.Cryptography.Enums;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Services;

namespace UiPath.Cryptography.Activities.Helpers
{
    public class EncodingHelpers
    {
#if NET
        public static DataSource<string> ConfigureEncodingDataSource()
        {
            return DataSourceBuilder<string>
                .WithId(s => s.ToString())
                .WithLabel(s => GetCodePageName((CodePages)int.Parse(s)))
                .WithSingleItemConverter(s => ArgumentFactoryHelper.CreateLiteralArgument(s),
                    s =>
                    {
                        if (s.TryGetInArgumentValue(out var value))
                        {
                            return value;
                        }
                        return null;
                    }).Build();
        }

        public static List<string> GetAvailableEncodings()
        {
            var availableEncodingsList = new List<string>() { ((int)CodePages.Default).ToString() };
            availableEncodingsList.AddRange(Encoding.GetEncodings().Select(e => e.CodePage.ToString()).ToList());
            return availableEncodingsList;
        }

#endif
        public static string GetCodePageName(CodePages value)
        {
            try
            {
                var enumName = typeof(CodePages).GetEnumName(value);
                var field = typeof(CodePages).GetField(enumName);

                var displayNameAttribute = field?.GetCustomAttribute<DisplayNameAttribute>();

                return displayNameAttribute?.DisplayName ?? enumName;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static Encoding KeyEncodingOrString(Encoding keyEncoding, string keyEncodingString)
        {
            if (keyEncodingString != null)
            {
                //for modern and cross projects - use the string code page to get the encoding
                keyEncoding = int.TryParse(keyEncodingString, out int codePage)
                    ? System.Text.Encoding.GetEncoding(codePage)
                    : System.Text.Encoding.GetEncoding(keyEncodingString);
            }

            return keyEncoding;
        }
    }
}
