namespace UiPath.Shared.Activities.Services.Interfaces
{
    /// <summary>
    /// Taken from System Activities
    /// </summary>
    internal interface IAttachedDataService
    {
        bool SetAttachedData<TTarget, TData>(TTarget target, TData attachedData, string attachedDataKey = null)
            where TTarget : class;
        bool TryGetAttachedData<TTarget, TData>(TTarget target, out TData attachedData, string attachedDataKey = null)
            where TTarget : class;

        bool TryRemoveAttachedData<TData, TTarget>(TTarget target)
            where TTarget : class;

        bool TryRemoveAttachedData<TTarget>(TTarget target, string attachedDataKey)
            where TTarget : class;
    }
}
