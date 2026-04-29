using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Activities.API.Models;

namespace UiPath.Java.Activities.API
{
    /// <summary>
    /// Extension methods for Java coded workflow operations.
    /// </summary>
    public static class JavaOperations
    {
        /// <summary>
        /// Loads a JAR file into the Java scope.
        /// </summary>
        /// <param name="javaScope">The Java scope handle.</param>
        /// <param name="jarPath">Path to the JAR file to load.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task LoadJar(this IJavaScopeHandle javaScope, string jarPath, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(javaScope);
            return javaScope.GetInvoker().LoadJar(jarPath, ct);
        }

        /// <summary>
        /// Invokes an instance method on a Java object.
        /// </summary>
        /// <param name="javaScope">The Java scope handle.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="targetObject">The Java object instance on which to invoke the method.</param>
        /// <param name="parameters">Optional parameters to pass to the method.</param>
        /// <param name="parameterTypes">Optional explicit parameter types.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="JavaObject"/> representing the result. For Java <c>void</c> methods, the returned <see cref="JavaObject"/> has <c>IsNull()</c> returning <c>true</c>.</returns>
        public static Task<JavaObject> InvokeMethod(this IJavaScopeHandle javaScope, string methodName, JavaObject targetObject, List<object> parameters = null, List<Type> parameterTypes = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(javaScope);
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("methodName must not be null or whitespace.", nameof(methodName));
            ArgumentNullException.ThrowIfNull(targetObject);
            var resolvedParams = parameters ?? new List<object>();
            var resolvedTypes = parameterTypes ?? ResolveTypes(resolvedParams);
            return javaScope.GetInvoker().InvokeMethod(methodName, null, targetObject, resolvedParams, resolvedTypes, ct);
        }

        /// <summary>
        /// Invokes a static method on a Java class.
        /// </summary>
        /// <param name="javaScope">The Java scope handle.</param>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="className">The fully-qualified Java class name.</param>
        /// <param name="parameters">Optional parameters to pass to the method.</param>
        /// <param name="parameterTypes">Optional explicit parameter types.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="JavaObject"/> representing the result. For Java <c>void</c> methods, the returned <see cref="JavaObject"/> has <c>IsNull()</c> returning <c>true</c>.</returns>
        public static Task<JavaObject> InvokeStaticMethod(this IJavaScopeHandle javaScope, string methodName, string className, List<object> parameters = null, List<Type> parameterTypes = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(javaScope);
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("methodName must not be null or whitespace.", nameof(methodName));
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("className must not be null or whitespace.", nameof(className));
            var resolvedParams = parameters ?? new List<object>();
            var resolvedTypes = parameterTypes ?? ResolveTypes(resolvedParams);
            return javaScope.GetInvoker().InvokeMethod(methodName, className, null, resolvedParams, resolvedTypes, ct);
        }

        /// <summary>
        /// Creates a new Java object by invoking its constructor.
        /// </summary>
        /// <param name="javaScope">The Java scope handle.</param>
        /// <param name="className">The fully-qualified Java class name to instantiate.</param>
        /// <param name="parameters">Optional constructor parameters.</param>
        /// <param name="parameterTypes">Optional explicit parameter types.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="JavaObject"/> representing the new instance.</returns>
        public static Task<JavaObject> CreateObject(this IJavaScopeHandle javaScope, string className, List<object> parameters = null, List<Type> parameterTypes = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(javaScope);
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("className must not be null or whitespace.", nameof(className));
            var resolvedParams = parameters ?? new List<object>();
            var resolvedTypes = parameterTypes ?? ResolveTypes(resolvedParams);
            return javaScope.GetInvoker().InvokeConstructor(className, resolvedParams, resolvedTypes, ct);
        }

        /// <summary>
        /// Gets the value of an instance field on a Java object.
        /// </summary>
        /// <param name="javaScope">The Java scope handle.</param>
        /// <param name="fieldName">The name of the field to retrieve.</param>
        /// <param name="targetObject">The Java object instance.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="JavaObject"/> representing the field value.</returns>
        public static Task<JavaObject> GetField(this IJavaScopeHandle javaScope, string fieldName, JavaObject targetObject, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(javaScope);
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("fieldName must not be null or whitespace.", nameof(fieldName));
            ArgumentNullException.ThrowIfNull(targetObject);
            return javaScope.GetInvoker().InvokeGetField(targetObject, fieldName, null, ct);
        }

        /// <summary>
        /// Gets the value of a static field on a Java class.
        /// </summary>
        /// <param name="javaScope">The Java scope handle.</param>
        /// <param name="fieldName">The name of the field to retrieve.</param>
        /// <param name="className">The fully-qualified Java class name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="JavaObject"/> representing the field value.</returns>
        public static Task<JavaObject> GetStaticField(this IJavaScopeHandle javaScope, string fieldName, string className, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(javaScope);
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("fieldName must not be null or whitespace.", nameof(fieldName));
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("className must not be null or whitespace.", nameof(className));
            return javaScope.GetInvoker().InvokeGetField(null, fieldName, className, ct);
        }

        /// <summary>
        /// Converts a <see cref="JavaObject"/> to the specified .NET type.
        /// </summary>
        /// <typeparam name="T">The target .NET type.</typeparam>
        /// <param name="javaScope">The Java scope handle.</param>
        /// <param name="javaObject">The Java object to convert.</param>
        /// <returns>The converted .NET value.</returns>
        public static T ConvertObject<T>(this IJavaScopeHandle javaScope, JavaObject javaObject)
        {
            ArgumentNullException.ThrowIfNull(javaScope);
            ArgumentNullException.ThrowIfNull(javaObject);
            return javaObject.Convert<T>();
        }

        internal static IInvoker GetInvoker(this IJavaScopeHandle handle) => handle.Invoker;

        private static List<Type> ResolveTypes(List<object> parameters)
        {
            var types = new List<Type>(parameters.Count);
            foreach (var p in parameters)
                types.Add(p?.GetType() ?? typeof(object));
            return types;
        }
    }
}
