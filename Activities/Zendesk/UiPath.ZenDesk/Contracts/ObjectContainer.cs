using System;
using System.Collections.Generic;
using System.Linq;

namespace UiPath.ZenDesk.Contracts
{
    /// <summary>
    /// A simple container for objects meant to be shared accross the application space.
    /// </summary>
    public class ObjectContainer : IObjectContainer
    {
        /// <summary>
        /// Gets the object holder.
        /// </summary>
        /// <value>
        /// The object holder.
        /// </value>
        private readonly Dictionary<Type, object> _objectHolder = new Dictionary<Type, object>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectContainer"/> class.
        /// </summary>
        public ObjectContainer()
        {
        }

        /// <summary>
        /// Stores a reference to a object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="object"></param>
        public virtual void Add<T>(T objectToAdd) where T : class
        {
            _objectHolder[typeof(T)] = objectToAdd;
        }

        /// <summary>
        /// Gets the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T Get<T>() where T : class
        {
            return (T)_objectHolder[typeof(T)];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public virtual IEnumerable<object> Where(Func<object, bool> filter) => _objectHolder.Values.Where(filter);

        /// <summary>
        /// Remove a object from the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public virtual void Remove<T>() where T : class
        {
            _objectHolder.Remove(typeof(T));
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        ///   <c>true</c> if this instance contains object; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool Contains<T>() where T : class
        {
            return _objectHolder.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public virtual void Clear()
        {
            _objectHolder.Clear();
        }
    }
}
