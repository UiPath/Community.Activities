using System;
#if NETFRAMEWORK
using System.Activities.Presentation;
#endif
using UiPath.Studio.Activities.Api;

namespace UiPath.Shared.Contracts
{
    internal class DesignerContract : RuntimeContractWrapper<DesignerContract>
    {
        private IWorkflowDesignApi _contract;
        private readonly object _contractObj;

#if NETFRAMEWORK
        private EditingContext _ctx;

        public DesignerContract(EditingContext ctx)
           : this(ctx, null)
        {
        }

        public DesignerContract(EditingContext ctx, string featureName)
           : base(featureName)
        {
            _ctx = ctx;
        }
#endif
        public DesignerContract(object api, string featureName) : base(featureName)
        {
            _contractObj = api;
        }

        public DesignerContract With(string featureName)
        {
            var result = new DesignerContract(_contractObj, featureName);
#if NEWTRAMEWORK
            result._ctx = _ctx;
#endif
            return result;
        }

        protected override object GetContractInstance()
        {
#if NETFRAMEWORK
            _contract = _ctx?.Services.GetService<IWorkflowDesignApi>();
#endif
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
