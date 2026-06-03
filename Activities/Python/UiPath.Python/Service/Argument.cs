using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace UiPath.Python.Service
{
    [DataContract]
    public class Argument
    {
        [DataMember]
        private object _wrappedValue;

        [DataMember]
        private string _typeName;

        public Argument(object obj)
        {
            Type type = obj?.GetType() ?? typeof(object);
            _typeName = type.AssemblyQualifiedName;
            _wrappedValue = WrapForSerialization(obj, type);
        }

        public object Unwrap()
        {
            if (_wrappedValue == null)
            {
                return null;
            }

            Type type = Type.GetType(_typeName);
            Debug.Assert(null != type);

            if (null == type)
            {
                return _wrappedValue;
            }

            if (type.IsArray && !type.Equals(typeof(object[])))
            {
                return UnwrapArray(type, (Array)_wrappedValue);
            }

            if (!type.IsArray && _wrappedValue is Array array)
            {
                if (TryGetDictionaryKeyValueTypes(type, out Type keyType, out Type valueType))
                {
                    return UnwrapDictionary(array, keyType, valueType);
                }
                if (TryGetEnumerableElementType(type, out Type elementType))
                {
                    return UnwrapEnumerable(type, array, elementType);
                }
            }

            // fix for Json serialize issue (ex: byte -> int)
            if (!type.IsArray && type != _wrappedValue.GetType())
            {
                _wrappedValue = Convert.ChangeType(_wrappedValue, type);
            }

            return _wrappedValue;
        }

        // Keep the declared type of _wrappedValue as object by flattening collections that
        // DataContractJsonSerializer would otherwise need a KnownType for.
        private static object WrapForSerialization(object obj, Type type)
        {
            if (type.IsArray && !type.Equals(typeof(object[])))
            {
                return WrapArray((Array)obj);
            }
            if (obj is IDictionary dict && TryGetDictionaryKeyValueTypes(type, out _, out _))
            {
                return WrapDictionary(dict);
            }
            if (obj is IEnumerable enumerable && !(obj is string) && TryGetEnumerableElementType(type, out _))
            {
                return WrapEnumerable(enumerable);
            }
            return obj;
        }

        private static object[] WrapArray(Array array)
        {
            return array.Cast<object>().ToArray();
        }

        private static object[] WrapDictionary(IDictionary dict)
        {
            var pairs = new object[dict.Count];
            int i = 0;
            foreach (DictionaryEntry entry in dict)
            {
                pairs[i++] = new object[] { entry.Key, entry.Value };
            }
            return pairs;
        }

        private static object[] WrapEnumerable(IEnumerable enumerable)
        {
            return enumerable.Cast<object>().ToArray();
        }

        // fix for Json serialize issue (ex: byte[] -> int[])
        private static Array UnwrapArray(Type type, Array array)
        {
            Type elementType = type.GetElementType();
            Debug.Assert(null != elementType);
            var typedArray = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                typedArray.SetValue(Convert.ChangeType(array.GetValue(i), elementType), i);
            }
            return typedArray;
        }

        private static IDictionary UnwrapDictionary(Array pairs, Type keyType, Type valueType)
        {
            var typedDict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
            foreach (object entry in pairs)
            {
                var pair = (Array)entry;
                typedDict.Add(
                    ChangeTypeOrNull(pair.GetValue(0), keyType),
                    ChangeTypeOrNull(pair.GetValue(1), valueType));
            }
            return typedDict;
        }

        private static object UnwrapEnumerable(Type targetType, Array items, Type elementType)
        {
            Type listOfT = typeof(List<>).MakeGenericType(elementType);

            if (targetType.IsInterface || targetType == listOfT)
            {
                return BuildTypedList(items, elementType, listOfT);
            }

            Type ienumerableOfT = typeof(IEnumerable<>).MakeGenericType(elementType);

            // Stack<T> enumerates top-first, but Stack<T>(IEnumerable<T>) preserves iteration
            // order — round-tripping through it would invert Pop ordering. Reverse so the
            // rebuilt Stack pops elements in the same order as the original.
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Stack<>))
            {
                var reversed = (IList)Activator.CreateInstance(listOfT);
                for (int i = items.Length - 1; i >= 0; i--)
                {
                    reversed.Add(ChangeTypeOrNull(items.GetValue(i), elementType));
                }
                var stackCtor = targetType.GetConstructor(new[] { ienumerableOfT });
                if (stackCtor != null)
                {
                    return stackCtor.Invoke(new object[] { reversed });
                }
            }

            var ctorIenum = targetType.GetConstructor(new[] { ienumerableOfT });
            if (ctorIenum != null)
            {
                return ctorIenum.Invoke(new object[] { BuildTypedList(items, elementType, listOfT) });
            }

            Type icollectionOfT = typeof(ICollection<>).MakeGenericType(elementType);
            if (icollectionOfT.IsAssignableFrom(targetType) && targetType.GetConstructor(Type.EmptyTypes) != null)
            {
                var instance = Activator.CreateInstance(targetType);
                var addMethod = icollectionOfT.GetMethod("Add");
                foreach (var item in items)
                {
                    addMethod.Invoke(instance, new[] { ChangeTypeOrNull(item, elementType) });
                }
                return instance;
            }

            var ctorIList = targetType.GetConstructor(new[] { typeof(IList<>).MakeGenericType(elementType) });
            if (ctorIList != null)
            {
                return ctorIList.Invoke(new object[] { BuildTypedList(items, elementType, listOfT) });
            }

            return BuildTypedList(items, elementType, listOfT);
        }

        private static IList BuildTypedList(Array items, Type elementType, Type listOfT)
        {
            var list = (IList)Activator.CreateInstance(listOfT);
            foreach (var item in items)
            {
                list.Add(ChangeTypeOrNull(item, elementType));
            }
            return list;
        }

        private static object ChangeTypeOrNull(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }
            if (targetType == typeof(object) || targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }
            return Convert.ChangeType(value, targetType);
        }

        private static bool TryGetEnumerableElementType(Type type, out Type elementType)
        {
            elementType = null;
            if (type == typeof(string) || typeof(IDictionary).IsAssignableFrom(type))
            {
                return false;
            }

            var ienum = FindGenericInterface(type, typeof(IEnumerable<>));
            if (ienum == null)
            {
                return false;
            }
            elementType = ienum.GetGenericArguments()[0];
            return true;
        }

        private static bool TryGetDictionaryKeyValueTypes(Type type, out Type keyType, out Type valueType)
        {
            keyType = null;
            valueType = null;
            var iDict = FindGenericInterface(type, typeof(IDictionary<,>))
                     ?? FindGenericInterface(type, typeof(IReadOnlyDictionary<,>));
            if (iDict == null)
            {
                return false;
            }
            var args = iDict.GetGenericArguments();
            keyType = args[0];
            valueType = args[1];
            return true;
        }

        private static Type FindGenericInterface(Type type, Type genericDefinition)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericDefinition)
            {
                return type;
            }
            return type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == genericDefinition);
        }
    }
}
