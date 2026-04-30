using UiPath.Shared.Contracts.Private;

namespace System.Activities
{
    internal static class PrivateContractExtensions
    {
        internal static PrivateRuntimeContract PrivateContract(this ActivityContext ctx, string featureName)
        {
            return new PrivateRuntimeContract(ctx, featureName);
        }
    }
}