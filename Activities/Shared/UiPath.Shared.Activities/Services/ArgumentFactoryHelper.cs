using System;
using System.Activities;
using UiPath.Shared.Activities.Services.Interfaces;

namespace UiPath.Shared.Activities.Services
{
    /// <summary>
    /// Taken from System Activities
    /// </summary>
    public static class ArgumentFactoryHelper
    {
        private static ArgumentFactory _argumentFactory;

        public static Language ProjectLanguage { get; private set; } = Language.VisualBasic;

        public static ICSharpExpressionParser CSharpExpressionParser { get; private set; } = new DefaultCSharpExpressionParser();

        public static void SetProjectLanguage(int language)
        {
            if (language == (int)ProjectLanguage)
                return;

            ProjectLanguage = language == 0 ? Language.VisualBasic : Language.CSharp;

            // reset argument factory when project language changes
            _argumentFactory = null;
        }

        public static void SetCSharpExpressionParser(ICSharpExpressionParser csharpExpressionParser) =>
            CSharpExpressionParser = csharpExpressionParser ?? throw new ArgumentNullException(nameof(csharpExpressionParser));

        public static InArgument<T> CreateLiteralArgument<T>(T value) =>
            GetFactory().CreateLiteralArgument(value);

        public static InArgument<T> CreateValueArgument<T>(string expressionText) =>
            GetFactory().CreateValueArgument<T>(expressionText);

        public static Variable<T> CreateVariableWithDefaultValue<T>(string defaultValue, string name) =>
            GetFactory().CreateVariableWithDefaultValue<T>(defaultValue, name);

        public static OutArgument<T> CreateOutArgumentReference<T>(string name) =>
            GetFactory().CreateOutArgumentReference<T>(name);

        public static InOutArgument<T> CreateInOutArgumentReference<T>(string name) =>
            GetFactory().CreateInOutArgumentReference<T>(name);

        public static InArgument CreateLiteralWithReflection(object value) =>
            GetFactory().CreateLiteralWithReflection(value);

        public static InArgument CreateValueArgumentWithReflection(string expressionText, Type argumentType) =>
            GetFactory().CreateValueArgumentWithReflection(expressionText, argumentType);

        public static Type GetTypeOfReference() =>
            GetFactory().GetTypeOfReference();

        public static Type GetTypeOfValue() =>
            GetFactory().GetTypeOfValue();

        private static ArgumentFactory GetFactory() =>
            _argumentFactory ?? (_argumentFactory = new ArgumentFactory(ProjectLanguage));
    }

    public enum Language
    {
        VisualBasic,
        CSharp
    }
}
