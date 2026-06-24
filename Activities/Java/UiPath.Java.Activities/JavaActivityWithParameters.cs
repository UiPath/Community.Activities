using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.Java.Activities.Properties;

namespace UiPath.Java.Activities
{
    public abstract class JavaActivityWithParameters : JavaActivity
    {
        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.ParametersDisplayName))]
        [LocalizedDescription(nameof(Resources.ParametersDescription))]
        public List<InArgument> Parameters { get; set; } = new List<InArgument>();

        [LocalizedCategory(nameof(Resources.Input))]
        [LocalizedDisplayName(nameof(Resources.ParametersListDisplayName))]
        public InArgument<List<object>> ParametersList { get; set; }

        private static int counter = 0;

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (Parameters.Count > 0 && ParametersList != null)
            {
                metadata.AddValidationError(string.Format(Resources.ParametersSetException, Resources.ParametersDisplayName,
                                                          Resources.ParametersListDisplayName));
                base.CacheMetadata(metadata);
                return;
            }

            base.CacheMetadata(metadata);
            foreach (var param in Parameters)
            {
                var toType = param?.ArgumentType ?? typeof(object);
                var toArgument = new RuntimeArgument("Arg" + counter, toType, ArgumentDirection.In);
                metadata.Bind(param, toArgument);
                metadata.AddArgument(toArgument);
                ++counter;
            }
        }

        protected (List<object> parameters, List<Type> types) GetParametersAndTypes(AsyncCodeActivityContext context)
        {
            if (ParametersList != null)
            {
                var parametersList = ParametersList.Get(context) ?? new List<object>();
                var types = parametersList.Select(p => p?.GetType() ?? typeof(object)).ToList();
                return (parametersList, types);
            }

            var parameters = new List<object>();
            var parameterTypes = new List<Type>();
            foreach (var param in Parameters)
            {
                var value = param.Get(context);
                parameters.Add(value);
                parameterTypes.Add(value?.GetType() ?? param?.ArgumentType ?? typeof(object));
            }
            return (parameters, parameterTypes);
        }
    }
}
