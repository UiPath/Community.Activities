using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace UiPath.Shared.Activities
{
    /// <summary>
    /// Taken from System Activities
    /// </summary>
    internal static class ActivityArgumentHelper
    {
        private const string NewArgumentPrefix = "Arg";

        /// <summary>
        /// Registers an argument with the metadata. This is used for non generic In/OutArguments
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="argumentName"></param>
        /// <param name="metadata"></param>
        /// <param name="direction"></param>
        internal static void AddNonGenericArgumentToMetadata(this Argument argument, string argumentName, CodeActivityMetadata metadata, ArgumentDirection direction)
        {
            var toType = argument?.ArgumentType ?? typeof(object);

            var runtimeText = new RuntimeArgument(argumentName, toType, direction);
            metadata.Bind(argument, runtimeText);
            metadata.AddArgument(runtimeText);
        }

        /// <summary>
        /// Registers an argument with the metadata. This is used for non generic In/OutArguments
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="argumentName"></param>
        /// <param name="metadata"></param>
        /// <param name="direction"></param>
        internal static void AddNonGenericArgumentToMetadata(this Argument argument, string argumentName, NativeActivityMetadata metadata, ArgumentDirection direction)
        {
            var toType = argument?.ArgumentType ?? typeof(object);

            var runtimeText = new RuntimeArgument(argumentName, toType, direction);
            metadata.Bind(argument, runtimeText);
            metadata.AddArgument(runtimeText);
        }

        /// <summary>
        /// Registers a list of arguments with the metadata. This is used for non generic In/OutArguments 
        /// </summary>
        internal static void AddMultipleNonGenericArgumentsToMetadata<T>(this IEnumerable<T> argumentList, NativeActivityMetadata metadata, string argumentPrefix = null) where T : Argument
        {
            // C# won't do an implicit cast to Argument for a collection so we can't use AddNonGenericArgumentToMetadata directly
            var argList = argumentList.Cast<Argument>().ToList();

            if (argumentPrefix == null)
                argumentPrefix = NewArgumentPrefix;

            var counter = 0;
            foreach (var argument in argList)
            {
                argument?.AddNonGenericArgumentToMetadata($"{argumentPrefix}{counter}", metadata, ArgumentDirection.In);
                counter++;
            }
        }

        /// <summary>
        /// Registers a list of arguments with the metadata. This is used for non generic In/OutArguments 
        /// </summary>
        internal static void AddMultipleNonGenericArgumentsToMetadata<T>(this IEnumerable<T> argumentList, CodeActivityMetadata metadata, string argumentPrefix = null) where T : Argument
        {
            // C# won't do an implicit cast to Argument for a collection so we can't use AddNonGenericArgumentToMetadata directly
            var argList = argumentList.Cast<Argument>().ToList();

            if (argumentPrefix == null)
                argumentPrefix = NewArgumentPrefix;

            var counter = 0;
            foreach (var argument in argList)
            {
                argument?.AddNonGenericArgumentToMetadata($"{argumentPrefix}{counter}", metadata, ArgumentDirection.In);
                counter++;
            }
        }

        public static void AddMultipleGenericInArgumentsToMetadata<T>(this IEnumerable<InArgument<T>> arguments, CodeActivityMetadata metadata, string argumentPrefix = null)
        {
            if (argumentPrefix == null)
            {
                argumentPrefix = NewArgumentPrefix;
            }

            if (arguments == null)
            {
                arguments = Enumerable.Empty<InArgument<T>>();
            }

            // Register the List of Arguments to the runtime
            var counter = 0;
            foreach (var item in arguments)
            {
                var runtimeArg = new RuntimeArgument($"{argumentPrefix}{counter++}", typeof(T), ArgumentDirection.In);
                metadata.Bind(item, runtimeArg);
                metadata.AddArgument(runtimeArg);
            }
        }

        /// <summary>
        /// Sets a value to an argument for its resumeContext
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="resumeContext"></param>
        /// <param name="value"></param>
        internal static void SetArgumentResult(this Argument argument, ActivityContext resumeContext, object value)
        {
            if (argument == null)
            {
                return;
            }

            try
            {
                argument.Set(resumeContext, value);
            }
            catch (InvalidOperationException)
            {
                argument.Set(resumeContext, TypeDescriptor.GetConverter(argument.ArgumentType).ConvertFrom(value));
            }
        }

        /// <summary>
        /// Tries to get an argument from the context. Throws an ArgumentNullException if its value is null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inArgument"></param>
        /// <param name="context"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        internal static T GetNotNullOrThrow<T>(this InArgument<T> inArgument, ActivityContext context, string errorMessage)
        {
            return TryGetValueWithPredicate(inArgument, context, x => x != null, errorMessage, msg => new ArgumentNullException(msg));
        }

        /// <summary>
        /// Tries to get a string argument from the context. Throws an ArgumentNullException if it's null or empty
        /// </summary>
        /// <param name="inArgument"></param>
        /// <param name="context"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        internal static string GetNotNullOrEmptyOrThrow(this InArgument<string> inArgument, ActivityContext context, string errorMessage)
        {
            return TryGetValueWithPredicate(inArgument, context, x => !string.IsNullOrEmpty(x), errorMessage, msg => new ArgumentNullException(msg));
        }

        internal static int GetPositiveIntegerOrThrow(this InArgument<int> inArgument, ActivityContext context,
            string errorMessage)
        {
            return TryGetValueWithPredicate(inArgument, context, x => x > 0, errorMessage, msg => new ArgumentException(msg));
        }

        /// <summary>
        /// Tries to get an absolute path from an InArgument<string>.
        /// Throws if the value is null or if the path is invalid (e.g. it contains a folder that does not exist).
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="context"></param>
        /// <param name="emptyArgumentErrorMessage"></param>
        /// <param name="nonexistentPathErrorMessageTemplate"></param>
        /// <param name="invalidCharactersInPathErrorMessage"></param>
        /// <param name="fileDoesNotExistMessageTemplate">Should be set if checkFileExistsOrThrow is true</param>
        /// <param name="checkFileExistsOrThrow">True if it should throw invalid path error when the file doesn't exist. Otherwise only throws if the directory of the file does not exist.</param>
        /// <returns></returns>
        internal static string GetValidAbsolutePath(this InArgument<string> filePath,
                                                    ActivityContext context,
                                                    string emptyArgumentErrorMessage,
                                                    string nonexistentPathErrorMessageTemplate,
                                                    string invalidCharactersInPathErrorMessage,
                                                    string fileDoesNotExistMessageTemplate,
                                                    bool checkFileExistsOrThrow = false)
        {
            var path = filePath.GetNotNullOrEmptyOrThrow(context, emptyArgumentErrorMessage);

            // If the output folder is the same as the project folder, the activity will only receive the file name as an input.
            // This needs to be translated into a full path, since Excel considers the root of MyDocuments to be the default folder.
            var pathFileInfo = new FileInfo(path);
            if (pathFileInfo.Directory == null || !pathFileInfo.Directory.Exists)
            {
                if (nonexistentPathErrorMessageTemplate == null)
                    nonexistentPathErrorMessageTemplate = string.Empty;

                throw new ArgumentException(string.Format(nonexistentPathErrorMessageTemplate, pathFileInfo.Directory?.FullName));
            }

            if (!path.IsPathValid())
            {
                throw new ArgumentException(invalidCharactersInPathErrorMessage);
            }

            if (checkFileExistsOrThrow && !pathFileInfo.Exists)
            {
                throw new FileNotFoundException(string.Format(fileDoesNotExistMessageTemplate, pathFileInfo.FullName));
            }

            return pathFileInfo.FullName;
        }

        internal static bool IsValidHttpLink(string path)
        {
            return Uri.TryCreate(path, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp ||
                       uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Tries to get a usable path from what the user passed in.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="getLocalPathFunc"></param>
        /// <param name="getSharePointUrlFunc"></param>
        /// <returns></returns>
        internal static string GetValidPath(string path, Func<string> getLocalPathFunc, Func<string> getSharePointUrlFunc)
        {
            try
            {
                return getLocalPathFunc();
            }
            catch
            {
                // If we couldn't parse the path as local, try to interpret it as a SharePoint path
                if (IsValidHttpLink(path))
                {
                    return getSharePointUrlFunc();
                }

                throw;
            }
        }

        internal static T TryGetValueWithPredicate<T>(
           this InArgument<T> inArgument,
           ActivityContext context,
           Predicate<T> validityCondition,
           string errorMessage)
        {
            return TryGetValueWithPredicate(inArgument, context, validityCondition,
                errorMessage, err => new ArgumentNullException(err));
        }

        internal static T TryGetValueWithPredicate<T>(
            this InArgument<T> inArgument,
            ActivityContext context,
            Predicate<T> validityCondition,
            string errorMessage,
            Func<string, Exception> exceptionFunc)
        {
            var value = inArgument.Get(context);
            if (!validityCondition(value))
            {
                throw exceptionFunc(errorMessage);
            }

            return value;
        }

        public static bool IsMatchingArgumentsDictionary(this Dictionary<string, Argument> arguments,
            Dictionary<string, Argument> dictionary)
        {
            return arguments != null
                && arguments.Keys.Count == dictionary.Keys.Count
                && arguments.Keys.All(k => dictionary.ContainsKey(k) && AreArgumentsEqual(dictionary[k], arguments[k]));
        }

        internal static bool AreArgumentsEqual(Argument arg1, Argument arg2)
        {
            return arg1.ArgumentType == arg2.ArgumentType
                && arg1.Direction == arg2.Direction
                && arg1.Expression == arg2.Expression
                && arg1.EvaluationOrder == arg2.EvaluationOrder;
        }
    }
}