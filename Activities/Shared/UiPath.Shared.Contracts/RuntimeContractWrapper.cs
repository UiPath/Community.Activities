using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace UiPath.Shared.Contracts
{
    internal abstract class RuntimeContractWrapper<T>
        where T : RuntimeContractWrapper<T>
    {
        [SuppressMessage("SonarQube", "S2743: A static field in a generic type is not shared among instances of different close constructed types.", Justification = "By design")]
        private static bool? _contractTypeExists;

        private readonly string _featureName;
        private bool? _isSupported = null;

        protected RuntimeContractWrapper(string featureName)
        {
            _featureName = featureName;
        }

        protected RuntimeContractWrapper()
            : this(null)
        {
        }

        [SuppressMessage("SonarQube", "S2696: Make the enclosing instance method 'static' or remove this set on the 'static' field.", Justification = "By design")]
        private object TryGetContractInstance()
        {
            if (_contractTypeExists == null)
            {
                _contractTypeExists = CheckContractType();
            }
            return _contractTypeExists.Value ? GetContractInstance() : null;
        }

        protected abstract object GetContractInstance();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool CheckContractType()
        {
            try
            {
                var type = GetContractType();
                return type != null;
            }
            catch (Exception ex)
            {
                Trace.TraceInformation(ex.ToString());
                return false;
            }
        }

        protected abstract Type GetContractType();

        public bool IsSupported()
        {
            if (_isSupported != null)
            {
                return _isSupported.Value;
            }
            var contract = TryGetContractInstance();
            if (contract == null)
            {
                _isSupported = false;
                return _isSupported.Value;
            }
            if (string.IsNullOrEmpty(_featureName))
            {
                _isSupported = true;
                return _isSupported.Value;
            }

            _isSupported = HasFeature(_featureName);
            return _isSupported.Value;
        }

        protected abstract bool HasFeature(string featureName);

        public TResult Invoke<TResult>(Func<T, TResult> func)
        {
            return Invoke(func, true);
        }

        protected TResult Invoke<TResult>(Func<T, TResult> func, bool throwIfNotSupported)
        {
            if (!IsSupported())
            {
                if (throwIfNotSupported)
                {
                    throw new NotSupportedException();
                }
                return default(TResult);
            }
            return func(GetInvokeInstance());
        }

        protected virtual T GetInvokeInstance()
        {
            return (T)this;
        }

        public TResult TryInvoke<TResult>(Func<T, TResult> func)
        {
            return Invoke(func, false);
        }

        public void TryInvoke(Action<T> action)
        {
            Invoke(c =>
            {
                action(c);
                return true;
            }, false);
        }
    }
}
