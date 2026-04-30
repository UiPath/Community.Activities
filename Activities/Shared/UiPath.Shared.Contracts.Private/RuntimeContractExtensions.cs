using UiPath.Shared.Contracts;

namespace System.Activities
{
    internal static class RuntimeContractExtensions
    {
        internal static RuntimeContract RuntimeContract(this ActivityContext ctx)
        {
            return new RuntimeContract(ctx);
        }

        internal static RuntimeContract RuntimeContract(this ActivityContext ctx, string featureName)
        {
            return new RuntimeContract(ctx, featureName);
        }
    }
}