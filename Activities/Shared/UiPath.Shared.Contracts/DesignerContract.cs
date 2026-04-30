using System;
using UiPath.Studio.Activities.Api;

namespace UiPath.Shared.Contracts
{
    internal class DesignerContract : RuntimeContractWrapper<DesignerContract>
    {
        private IWorkflowDesignApi _contract;
        private readonly object _contractObj;

        public DesignerContract(object api, string featureName) : base(featureName)
        {
            _contractObj = api;
        }

        public DesignerContract With(string featureName)
        {
            var result = new DesignerContract(_contractObj, featureName);
            return result;
        }

        protected override object GetContractInstance()
        {
            _contract = _contract ?? _contractObj as IWorkflowDesignApi;

            return _contract;
        }

        protected override Type GetContractType()
        {
            return typeof(IWorkflowDesignApi);
        }

        protected override bool HasFeature(string featureName)
        {
            return _contract.HasFeature(featureName);
        }

        internal IWorkflowDesignApi UnSafe()
        {
            return _contract;
        }
    }
}
