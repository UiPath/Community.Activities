using System;
using System.Activities;
using UiPath.Robot.Activities.Api;

namespace UiPath.Shared.Contracts
{
    internal class RuntimeContract : RuntimeContractWrapper<RuntimeContract>
    {
        private readonly ActivityContext _ctx;
        private IExecutorRuntime _contract;

        public RuntimeContract(ActivityContext ctx)
            : this(ctx, null)
        {
        }

        public RuntimeContract(ActivityContext ctx, string featureName)
            : base(featureName)
        {
            _ctx = ctx;
        }

        public RuntimeContract(IExecutorRuntime runtime, string featureName)
            : base(featureName)
        {
            _contract = runtime;
        }

        public RuntimeContract With(string featureName)
        {
            if (_contract != null)
            {
                return new RuntimeContract(_contract, featureName);
            }
            return new RuntimeContract(_ctx, featureName);
        }

        protected override object GetContractInstance()
        {
            if (_contract == null)
            {
                _contract = _ctx?.GetExtension<IExecutorRuntime>();
            }
            return _contract;
        }

        protected override Type GetContractType()
        {
            return typeof(IExecutorRuntime);
        }

        protected override bool HasFeature(string featureName)
        {
            return _contract.HasFeature(featureName);
        }

        internal virtual IExecutorRuntime UnSafe()
        {
            return _contract;
        }
    }
}
