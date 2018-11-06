// <copyright file="RefOutArg.cs" company="Nito Programs">
//     Copyright (c) 2010-2011 Nito Programs.
// </copyright>

namespace Nito.KitchenSink.Dynamic
{
    using System;
    using System.Diagnostics.Contracts;

    /// <summary>
    /// A wrapper around a "ref" or "out" argument invoked dynamically.
    /// </summary>
    internal sealed class RefOutArg
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RefOutArg"/> class.
        /// </summary>
        private RefOutArg()
        {
        }

        /// <summary>
        /// Gets or sets the wrapped value as an object.
        /// </summary>
        public object ValueAsObject { get; set; }

        /// <summary>
        /// Gets or sets the wrapped value.
        /// </summary>
        public dynamic Value
        {
            get
            {
                return this.ValueAsObject;
            }

            set
            {
                this.ValueAsObject = value;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RefOutArg"/> class wrapping the default value of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to wrap.</typeparam>
        /// <returns>A new instance of the <see cref="RefOutArg"/> class wrapping the default value of <typeparamref name="T"/>.</returns>
        public static RefOutArg Create<T>()
        {
            Contract.Ensures(Contract.Result<RefOutArg>() != null);
            return new RefOutArg { ValueAsObject = default(T) };
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RefOutArg"/> class wrapping the specified value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        /// <returns>A new instance of the <see cref="RefOutArg"/> class wrapping the specified value.</returns>
        public static RefOutArg Create(object value = null)
        {
            Contract.Ensures(Contract.Result<RefOutArg>() != null);
            Contract.Ensures(Contract.Result<RefOutArg>().ValueAsObject == value);
            return new RefOutArg { ValueAsObject = value };
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return Convert.ToString(this.ValueAsObject);
        }
    }
}