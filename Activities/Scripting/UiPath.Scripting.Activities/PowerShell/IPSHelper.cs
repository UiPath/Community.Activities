using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using UiPath.Scripting.Activities.Properties;

namespace UiPath.Scripting.Activities.PowerShell
{
    public sealed class IPSHelper
    {
        /// <summary>
        /// A helper used for binding the properties of the InovkePowerShellCore Activity.
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="input"></param>
        /// <param name="errors"></param>
        /// <param name="commandText"></param>
        /// <param name="typeName"></param>
        /// <param name="displayName"></param>
        /// <param name="variables"></param>
        /// <param name="parameters"></param>
        /// <param name="childVariables"></param>
        /// <param name="childParameters"></param>
        public static void CacheMetadataHelper(
            ActivityMetadata metadata, InArgument<Collection<PSObject>> input, OutArgument<Collection<ErrorRecord>> errors, InArgument<string> commandText, string typeName,
            string displayName, IDictionary<string, Argument> variables, IDictionary<string, InArgument> parameters,
            IDictionary<string, Argument> childVariables, IDictionary<string, InArgument> childParameters)
        {
            childVariables.Clear();
            childParameters.Clear();
            RuntimeArgument inputArgument = new RuntimeArgument("Input", typeof(Collection<PSObject>), ArgumentDirection.In);
            metadata.Bind(input, inputArgument);
            metadata.AddArgument(inputArgument);

            RuntimeArgument errorArgument = new RuntimeArgument("Errors", typeof(Collection<ErrorRecord>), ArgumentDirection.Out);
            metadata.Bind(errors, errorArgument);
            metadata.AddArgument(errorArgument);

            // Validation error for the required Command field
            if (commandText == null)
            {
                metadata.AddValidationError(string.Format(Resources.PowerShellRequiresCommandException, displayName));
            }

            foreach (KeyValuePair<string, Argument> variable in variables)
            {
                string name = variable.Key;
                Argument argument = variable.Value;
                RuntimeArgument ra = new RuntimeArgument(name, argument.ArgumentType, argument.Direction, true);
                metadata.Bind(argument, ra);
                metadata.AddArgument(ra);

                childVariables.Add(name, Argument.CreateReference(argument, name));
            }

            foreach (KeyValuePair<string, InArgument> parameter in parameters)
            {
                string name = parameter.Key;
                InArgument argument = parameter.Value;
                RuntimeArgument ra;
                if (argument.ArgumentType == typeof(bool))
                {
                    ra = new RuntimeArgument(name, argument.ArgumentType, argument.Direction, false);
                }
                else
                {
                    ra = new RuntimeArgument(name, argument.ArgumentType, argument.Direction, true);
                }
                metadata.Bind(argument, ra);
                metadata.AddArgument(ra);

                childParameters.Add(name, Argument.CreateReference(argument, name) as InArgument);
            }
        }
    }
}
