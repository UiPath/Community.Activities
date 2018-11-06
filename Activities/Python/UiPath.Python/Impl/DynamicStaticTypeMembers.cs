// <copyright file="DynamicStaticTypeMembers.cs" company="Nito Programs">
//     Copyright (c) 2010-2011 Nito Programs.
// </copyright>

namespace Nito.KitchenSink.Dynamic
{
    // See these blog posts for details on how this class works:
    //   http://blogs.msdn.com/davidebb/archive/2009/10/23/using-c-dynamic-to-call-static-members.aspx
    //   http://nitoprograms.blogspot.com/2010/04/dynamically-binding-to-static-class.html
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Dynamic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// A dynamic object that allows access to a type's static members, resolved dynamically at runtime.
    /// </summary>
    internal sealed class DynamicStaticTypeMembers : DynamicObject
    {
        /// <summary>
        /// The trace source for failed binding messages.
        /// </summary>
        private static readonly TraceSource Trace = new TraceSource("Nito.KitchenSink.Dynamic");

        /// <summary>
        /// The underlying type.
        /// </summary>
        private readonly Type type;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicStaticTypeMembers"/> class wrapping the specified type.
        /// </summary>
        /// <param name="type">The underlying type to wrap.</param>
        private DynamicStaticTypeMembers(Type type)
        {
            Contract.Requires(type != null);
            this.type = type;
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(this.type != null);
        }

        /// <summary>
        /// Gets a value for a static property defined by the wrapped type.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
        /// </returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            Contract.Assume(binder != null);
            Contract.Assume(binder.Name != null);

            Trace.TraceEvent(TraceEventType.Verbose, 0, "Getting the value of static property " + binder.Name + " on type " + this.type.Name);

            var prop = this.type.GetProperty(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public);
            if (prop == null)
            {
                Trace.TraceEvent(TraceEventType.Error, 0, "Could not find static property " + binder.Name + " on type " + this.type.Name);
                result = null;
                return false;
            }

            result = prop.GetValue(null, null);
            return true;
        }

        /// <summary>
        /// Sets a value for a static property defined by the wrapped type.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, the <paramref name="value"/> is "Test".</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Contract.Assume(binder != null);
            Contract.Assume(binder.Name != null);

            Trace.TraceEvent(TraceEventType.Verbose, 0, "Setting the value of static property " + binder.Name + " on type " + this.type.Name);

            var prop = this.type.GetProperty(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public);
            if (prop == null)
            {
                Trace.TraceEvent(TraceEventType.Error, 0, "Could not find static property " + binder.Name + " on type " + this.type.Name);
                return false;
            }

            prop.SetValue(null, value, null);
            return true;
        }

        /// <summary>
        /// Calls a static method defined by the wrapped type.
        /// </summary>
        /// <param name="binder">Provides information about the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleMethod". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
        /// <param name="args">The arguments that are passed to the object member during the invoke operation. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <c>args[0]</c> is equal to 100.</param>
        /// <param name="result">The result of the member invocation.</param>
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
        /// </returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            Contract.Assume(binder != null);
            Contract.Assume(binder.Name != null);
            Contract.Assume(args != null);

            Trace.TraceEvent(TraceEventType.Verbose, 0, "Invoking static method " + binder.Name + " on type " + this.type.Name + " with argument types { " + string.Join(", ", args.Select(x => x == null ? "<unknown>" : x.GetType().Name)) + " }");

            // Convert any RefOutArg arguments into ref/out arguments
            var refArguments = new RefOutArg[args.Length];
            for (int i = 0; i != args.Length; ++i)
            {
                refArguments[i] = args[i] as RefOutArg;
                if (refArguments[i] != null)
                {
                    args[i] = refArguments[i].ValueAsObject;
                }
            }

            // Resolve the method
            const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public;
            object state;
            MethodBase method;
            try
            {
                var methods = this.type.GetMethods(flags).Where(x => x.Name == binder.Name);
                Contract.Assume(Type.DefaultBinder != null);
                method = Type.DefaultBinder.BindToMethod(flags, methods.ToArray(), ref args, null, null, null, out state);
                Contract.Assume(method != null);
                Contract.Assume(args != null);
            }
            catch (Exception ex)
            {
                Trace.TraceEvent(TraceEventType.Error, 0, "Could not find static method " + binder.Name + " on type " + this.type.Name + ": [" + ex.GetType() + "] " + ex.Message);
                throw;
            }

            // Ensure that all ref/out arguments were properly wrapped
            if (method.GetParameters().Count(x => x.ParameterType.IsByRef) != refArguments.Count(x => x != null))
            {
                throw new ArgumentException("ref/out parameters need a RefOutArg wrapper when invoking " + this.type.Name + "." + binder.Name + ".");
            }

            // Invoke the method, allowing exceptions to propogate
            try
            {
                result = method.Invoke(null, args);
            }
            finally
            {
                if (state != null)
                {
                    Type.DefaultBinder.ReorderArgumentArray(ref args, state);
                    Contract.Assume(args != null);
                }

                // Convert any ref/out arguments into RefOutArg results
                Contract.Assume(args.Length == refArguments.Length);
                for (int i = 0; i != args.Length; ++i)
                {
                    if (refArguments[i] != null)
                    {
                        refArguments[i].ValueAsObject = args[i];
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DynamicStaticTypeMembers"/> class wrapping the specified type.
        /// </summary>
        /// <param name="type">The underlying type to wrap. May not be <c>null</c>.</param>
        /// <returns>An instance of <see cref="DynamicStaticTypeMembers"/>, as a dynamic type.</returns>
        public static dynamic Create(Type type)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.Result<object>() != null);
            return new DynamicStaticTypeMembers(type);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DynamicStaticTypeMembers"/> class wrapping the specified type.
        /// </summary>
        /// <typeparam name="T">The underlying type to wrap.</typeparam>
        /// <returns>An instance of <see cref="DynamicStaticTypeMembers"/>, as a dynamic type.</returns>
        public static dynamic Create<T>()
        {
            Contract.Ensures(Contract.Result<object>() != null);
            return new DynamicStaticTypeMembers(typeof(T));
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return this.type.Name;
        }
    }
}
