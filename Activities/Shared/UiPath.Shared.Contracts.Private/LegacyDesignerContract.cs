using System;
using UiPath.Activities.Contracts;

namespace UiPath.Shared.Contracts.Private
{
    internal class LegacyDesignerContract : RuntimeContractWrapper<LegacyDesignerContract>
    {
        private IWorkflowDesignerContract _contract;

        public LegacyDesignerContract()
           : base()
        {
        }

        public LegacyDesignerContract(string featureName)
            : base(featureName)
        {
        }

        public LegacyDesignerContract With(string featureName)
        {
            return new LegacyDesignerContract(featureName);
        }

        protected override object GetContractInstance()
        {
            _contract = WorkflowDesignerContractRegistry.Instance;
            return _contract;
        }

        protected override Type GetContractType()
        {
            return typeof(IWorkflowDesignerContract);
        }

        protected override bool HasFeature(string featureName)
        {
            return _contract.HasFeature(featureName);
        }

        internal IWorkflowDesignerContract UnSafe()
        {
            return _contract;
        }
    }
}
