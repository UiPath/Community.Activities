using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;

namespace UiPath.Shared.Activities
{
    public abstract class AsyncTaskNativeActivity : NativeActivity
    {
        private AsyncTaskNativeImplementation _impl = new AsyncTaskNativeImplementation();

        // Always true because we create bookmarks.
        protected override bool CanInduceIdle => true;

        protected override void Abort(NativeActivityAbortContext context)
        {
            base.Abort(context);
        }

        protected override void Cancel(NativeActivityContext context)
        {
            _impl.Cancel(context);

            // Called so that any outstanding bookmarks are removed.
            // Not the only side effect but it's what we're interested in here.
            base.Cancel(context);
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            _impl.CacheMetadata(metadata);
            base.CacheMetadata(metadata);
        }

        protected abstract Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken);

        protected override void Execute(NativeActivityContext context)
        {
            _impl.Execute(context, ExecuteAsync, BookmarkResumptionCallback);
        }

        protected virtual void BookmarkResumptionCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            _impl.BookmarkResumptionCallback(context, value);
        }
    }

    public abstract class AsyncTaskNativeActivity<T> : NativeActivity<T>
    {
        private AsyncTaskNativeImplementation _impl = new AsyncTaskNativeImplementation();

        protected override bool CanInduceIdle => true;

        protected override void Abort(NativeActivityAbortContext context)
        {
            base.Abort(context);
        }

        protected override void Cancel(NativeActivityContext context)
        {
            _impl.Cancel(context);
            base.Cancel(context);
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            _impl.CacheMetadata(metadata);
            base.CacheMetadata(metadata);
        }

        protected abstract Task<Action<NativeActivityContext>> ExecuteAsync(NativeActivityContext context, CancellationToken cancellationToken);

        protected override void Execute(NativeActivityContext context)
        {
            _impl.Execute(context, ExecuteAsync, BookmarkResumptionCallback);
        }

        protected virtual void BookmarkResumptionCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            _impl.BookmarkResumptionCallback(context, value);
        }
    }
}
