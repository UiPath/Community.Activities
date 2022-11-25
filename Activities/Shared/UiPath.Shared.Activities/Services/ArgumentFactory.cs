using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Diagnostics;

namespace UiPath.Shared.Activities.Services
{
    /// <summary>
    /// Needs to be in sync with UiPath.Obsolete.Activities.Design.Shared.Services
    /// to offer same functionality
    /// Taken from System Activities
    /// </summary>
    internal class ArgumentFactory
    {
        private readonly Language _language;


        public ArgumentFactory(Language language)
        {
            _language = language;
        }

        public InArgument<T> CreateLiteralArgument<T>(T value) =>
            new InArgument<T>(new Literal<T>(value));

        public InArgument<T> CreateValueArgument<T>(string expressionText)
        {
            if (_language == Language.VisualBasic)
            {
                return new InArgument<T>
                {
                    Expression = new VisualBasicValue<T>(expressionText)
                };
            }

            return new InArgument<T>
            {
                Expression = new CSharpValue<T>(expressionText)
            };
        }

        public Variable<T> CreateVariableWithDefaultValue<T>(string defaultValue, string name)
        {
            if (_language == Language.VisualBasic)
            {
                return new Variable<T>()
                {
                    Name = name,
                    Default = new VisualBasicValue<T>(defaultValue)
                };
            }

            return new Variable<T>()
            {
                Name = name,
                Default = new CSharpValue<T>(defaultValue)
            };
        }

        public InArgument CreateLiteralWithReflection(object value)
        {
            if (value == null)
            {
                return null;
            }
            try
            {
                var literalType = typeof(Literal<>).MakeGenericType(value.GetType());
                var literal = Activator.CreateInstance(literalType, value);
                var inArgumentType = typeof(InArgument<>).MakeGenericType(value.GetType());
                return Activator.CreateInstance(inArgumentType, literal) as InArgument;
            }
            catch (Exception e)
            {
                Trace.TraceWarning(e.ToString());
            }

            return null;
        }

        public InArgument CreateValueArgumentWithReflection(string expressionText, Type argumentType)
        {
            if (string.IsNullOrWhiteSpace(expressionText))
            {
                return null;
            }

            try
            {
                var genericType = _language == Language.VisualBasic ? typeof(VisualBasicValue<>) : typeof(CSharpValue<>);
                var valueType = genericType.MakeGenericType(argumentType);
                var value = Activator.CreateInstance(valueType, expressionText);
                var inArgumentType = typeof(InArgument<>).MakeGenericType(argumentType);
                return Activator.CreateInstance(inArgumentType, value) as InArgument;
            }
            catch (Exception e)
            {
                Trace.TraceWarning(e.ToString());
            }

            return null;
        }

        public OutArgument<T> CreateOutArgumentReference<T>(string name) => _language == Language.VisualBasic ? (OutArgument<T>)new VisualBasicReference<T>(name) : new CSharpReference<T>(name);

        public InOutArgument<T> CreateInOutArgumentReference<T>(string name) => _language == Language.VisualBasic ? (InOutArgument<T>)new VisualBasicReference<T>(name) : new CSharpReference<T>(name);

        public Type GetTypeOfReference() => _language == Language.VisualBasic ? typeof(VisualBasicReference<>) : typeof(CSharpReference<>);

        public Type GetTypeOfValue() => _language == Language.VisualBasic ? typeof(VisualBasicValue<>) : typeof(CSharpValue<>);
    }
}
