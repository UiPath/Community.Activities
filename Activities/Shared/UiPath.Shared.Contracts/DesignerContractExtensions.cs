#if NETFRAMEWORK
using System.Activities.Presentation;
using UiPath.Shared.Contracts;

namespace System.Activities
{
    internal static class DesignerContractExtensions
    {
        internal static DesignerContract DesignerContract(this EditingContext ctx, string featureName)
        {
            return new DesignerContract(ctx, featureName);
        }
    }
}
#endif