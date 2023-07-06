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
    public static class EncodingHelpers
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
            var availableEncodingsList = new List<int>() { ((int)CodePages.Default) };
            availableEncodingsList.AddRange(Encoding.GetEncodings().Select(e => e.CodePage).ToList());
            availableEncodingsList = availableEncodingsList.OrderBy(e => GetEncodingOrderIndex(e)).ToList();
            
            return availableEncodingsList.Select(e => e.ToString()).ToList();
        }

        /// <summary>
        /// Calculates an orderIndex which respects exceptions included in "exceptionDictioanry"
        /// </summary>
        private static int GetEncodingOrderIndex(int codePage)
        {
            var exceptionDictionary = new Dictionary<int, int> 
            { 
                {65001, 1 } // UNICODE UTF-8 https://uipath.atlassian.net/browse/STUD-64202
            };

            return exceptionDictionary.GetValueOrDefault(codePage, codePage * 10);
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
