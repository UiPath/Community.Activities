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
    public sealed class EncodingHelpers
    {
#if NET
        private EncodingHelpers()
        {
        }

        static EncodingHelpers()
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch
            {
                // Redundant try-catch to ignore encoding provider init errors if any exist
            }
        }

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
            availableEncodingsList.AddRange(Encoding.GetEncodings()
                .Select(e => e.CodePage)
                .OrderBy(e => GetEncodingOrderIndex(e))
                .Select(e => e.ToString())
                .ToList());

            return availableEncodingsList;
        }

        /// <summary>
        /// Calculates an orderIndex which respects exceptions included in "exceptionDictioanry"
        /// </summary>
        private static int GetEncodingOrderIndex(int codePage)
        {
            var exceptionDictionary = new Dictionary<int, int> 
            { 
                {(int)CodePages.UTF_8, 1 }, // https://uipath.atlassian.net/browse/STUD-64202
                {(int)CodePages.UTF_16, 2 },
                {(int)CodePages.UTF_16BE, 3 },
                {(int)CodePages.UTF_32, 4 },
                {(int)CodePages.UTF_32BE, 5 },
                {(int)CodePages.US_ASCII, 6 },
                {(int)CodePages.ISO_8859_1, 7 },
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
