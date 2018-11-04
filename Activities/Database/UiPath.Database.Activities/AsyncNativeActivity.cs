using System;
using System.Activities;
using System.Activities.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UiPath.Database.Activities
{
    internal class BookmarkResumptionHelper : IWorkflowInstanceExtension
    {
        private WorkflowInstanceProxy instance;

        public void ResumeBookmark(Bookmark bookmark, object value)
        {
            this.instance.EndResumeBookmark(
                this.instance.BeginResumeBookmark(bookmark, value, null, null));
        }

        public void BeginResumeBookmark(Bookmark bookmark, object value)
        {
            this.instance.BeginResumeBookmark(bookmark, value, null, null);
        }

        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
        {
            yield break;
        }

        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)
        {
            this.instance = instance;
        }
    }

    public abstract class AsyncNativeActivity : NativeActivity
    {
        private Variable<NoPersistHandle> NoPersistHandle
        {
            get;
            set;
        }
        private Variable<Bookmark> Bookmark
        {
            get;
            set;
        }
        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
        protected abstract System.IAsyncResult BeginExecute(NativeActivityContext context, System.AsyncCallback callback, object state);
        protected abstract void EndExecute(NativeActivityContext context, System.IAsyncResult result);
        protected override void Execute(NativeActivityContext context)
        {
            NoPersistHandle noPersistHandle = this.NoPersistHandle.Get(context);
            noPersistHandle.Enter(context);
            Bookmark bookmark = context.CreateBookmark(new BookmarkCallback(this.BookmarkResumptionCallback));
            this.Bookmark.Set(context, bookmark);
            BookmarkResumptionHelper helper = context.GetExtension<BookmarkResumptionHelper>();
            System.Action<System.IAsyncResult> state = delegate(System.IAsyncResult result)
            {
                helper.ResumeBookmark(bookmark, result);
            };
            System.IAsyncResult asyncResult = this.BeginExecute(context, new System.AsyncCallback(this.AsyncCompletionCallback), state);
            if (asyncResult.CompletedSynchronously)
            {
                noPersistHandle.Exit(context);
                context.RemoveBookmark(bookmark);
                this.EndExecute(context, asyncResult);
            }
        }

        private void AsyncCompletionCallback(System.IAsyncResult asyncResult)
        {
            if (!asyncResult.CompletedSynchronously)
            {
                System.Action<System.IAsyncResult> action = asyncResult.AsyncState as System.Action<System.IAsyncResult>;
                action(asyncResult);
            }
        }
        private void BookmarkResumptionCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            NoPersistHandle noPersistHandle = this.NoPersistHandle.Get(context);
            noPersistHandle.Exit(context);
            System.IAsyncResult result = value as System.IAsyncResult;
            this.EndExecute(context, result);
        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            this.NoPersistHandle = new Variable<NoPersistHandle>();
            this.Bookmark = new Variable<Bookmark>();
            metadata.AddImplementationVariable(this.NoPersistHandle);
            metadata.AddImplementationVariable(this.Bookmark);
            metadata.RequireExtension<BookmarkResumptionHelper>();
            metadata.AddDefaultExtensionProvider<BookmarkResumptionHelper>(() => new BookmarkResumptionHelper());
            base.CacheMetadata(metadata);
        }
    }
}
