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

            if (_wrappedValue is Array array)
            {
                if (TryGetDictionaryKeyValueTypes(type, out Type keyType, out Type valueType))
                {
                    return UnwrapDictionary(array, keyType, valueType);
                }
                if (TryGetEnumerableElementType(type, out Type elementType))
                {
                    return UnwrapList(array, elementType);
                }
            }

            // fix for Json serialize issue (ex: byte -> int)
            if (type != _wrappedValue.GetType())
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
            if (obj is IEnumerable enumerable && !(obj is string))
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

        private static IList UnwrapList(Array items, Type elementType)
        {
            var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            foreach (var item in items)
            {
                typedList.Add(ChangeTypeOrNull(item, elementType));
            }
            return typedList;
        }

        private static object ChangeTypeOrNull(object value, Type targetType)
        {
            return value == null ? null : Convert.ChangeType(value, targetType);
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
