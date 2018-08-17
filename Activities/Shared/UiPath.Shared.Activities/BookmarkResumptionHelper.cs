using System.Activities;
using System.Activities.Hosting;
using System.Collections.Generic;

namespace UiPath.Shared.Activities
{
    internal sealed class BookmarkResumptionHelper : IWorkflowInstanceExtension
    {
        private WorkflowInstanceProxy _workflowInstance;

        public static BookmarkResumptionHelper Create()
        {
            return new BookmarkResumptionHelper();
        }

        internal BookmarkResumptionResult ResumeBookmark(Bookmark bookmark, object value)
        {
            return _workflowInstance.EndResumeBookmark(_workflowInstance.BeginResumeBookmark(bookmark, value, null, null));
        }

        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
        {
            yield break;
        }

        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)
        {
            _workflowInstance = instance;
        }
    }
}
