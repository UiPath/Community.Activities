using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;

namespace UiPath.Shared.Activities
{
    internal struct AsyncTaskNativeImplementation
    {
        private Variable<NoPersistHandle> _noPersistHandle;

        // The token fron this source should be passed around to any async tasks.
        private Variable<CancellationTokenSource> _cancellationTokenSource;

        private Variable<bool> _bookmarkResumed;

        public void Cancel(NativeActivityContext context)
        {
            bool bookmarkResumed = _bookmarkResumed.Get(context);

            if (!bookmarkResumed)
            {
                CancellationTokenSource cancellationTokenSource = _cancellationTokenSource.Get(context);

                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }

            context.MarkCanceled();
            // Overriding the Cancel method inhibits the propagation of cancellation requests to children.
            context.CancelChildren();

            if (!bookmarkResumed)
            {
                _noPersistHandle.Get(context).Exit(context);
            }
        }

        public void CacheMetadata(NativeActivityMetadata metadata)
        {
            _noPersistHandle = new Variable<NoPersistHandle>();
            _cancellationTokenSource = new Variable<CancellationTokenSource>();
            _bookmarkResumed = new Variable<bool>();

            metadata.AddImplementationVariable(_noPersistHandle);
            metadata.AddImplementationVariable(_cancellationTokenSource);
            metadata.AddImplementationVariable(_bookmarkResumed);

            metadata.RequireExtension<BookmarkResumptionHelper>();
            metadata.AddDefaultExtensionProvider(BookmarkResumptionHelper.Create);
        }

        public void Execute(NativeActivityContext context
            , Func<NativeActivityContext, CancellationToken, Task<Action<NativeActivityContext>>> onExecute
            , BookmarkCallback callback)
        {
            _noPersistHandle.Get(context).Enter(context);

            Bookmark bookmark = context.CreateBookmark(callback);
            BookmarkResumptionHelper bookmarkHelper = context.GetExtension<BookmarkResumptionHelper>();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Set(context, cancellationTokenSource);

            _bookmarkResumed.Set(context, false);

            // TODO: Review the continuation.
            onExecute(context, cancellationTokenSource.Token).ContinueWith(t =>
            {
                // We resume the bookmark only if the activity wasn't
                // cancelled since the cancellation removes any bookmarks.
                if (cancellationTokenSource.IsCancellationRequested == false)
                {
                    object executionResult = null;

                    if (t.IsFaulted)
                    {
                        executionResult = t.Exception.InnerException;
                    }
                    else
                    {
                        executionResult = t.Result;
                    }

                    bookmarkHelper.ResumeBookmark(bookmark, executionResult);
                }
            });
        }

        public void BookmarkResumptionCallback(NativeActivityContext context, object value)
        {
            if (value is Exception ex)
            {
                throw ex;
            }

            _noPersistHandle.Get(context).Exit(context);

            Action<NativeActivityContext> executeCallback = value as Action<NativeActivityContext>;
            executeCallback?.Invoke(context);

            // TODO: The original author says: "unnecessary since it's not multiple resume:".
            //context.RemoveBookmark(bookmark);

            _cancellationTokenSource.Get(context)?.Dispose();

            _bookmarkResumed.Set(context, true);
        }
    }
}
