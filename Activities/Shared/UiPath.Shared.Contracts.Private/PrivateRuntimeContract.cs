using System;
using System.Activities;
using UiPath.Activities.Contracts;

namespace UiPath.Shared.Contracts.Private
{
    internal class PrivateRuntimeContract : RuntimeContractWrapper<PrivateRuntimeContract>
    {
        private readonly ActivityContext _ctx;
        private IWorkflowRuntime _contract;
        private object _contractObj;

        public PrivateRuntimeContract(ActivityContext ctx)
            : this(ctx, null)
        {
        }

        public PrivateRuntimeContract(ActivityContext ctx, string featureName)
            : base(featureName)
        {
            _ctx = ctx;
        }

        public PrivateRuntimeContract(object api)
            : base(null)
        {
            _contractObj = api;
        }

        public PrivateRuntimeContract With(string featureName)
        {
            return new PrivateRuntimeContract(_ctx, featureName)
            {
                _contractObj = _contractObj
            };
        }

        protected override object GetContractInstance()
        {
            _contract = _ctx?.GetExtension<IWorkflowRuntime>() ?? _contractObj as IWorkflowRuntime;
            return _contract;
        }

        protected override Type GetContractType()
        {
            return typeof(IWorkflowRuntime);
        }

        protected override bool HasFeature(string featureName)
        {
            return _contract.HasFeature(featureName);
        }

        internal virtual IWorkflowRuntime UnSafe()
        {
            return _contract;
        }
    }
}
