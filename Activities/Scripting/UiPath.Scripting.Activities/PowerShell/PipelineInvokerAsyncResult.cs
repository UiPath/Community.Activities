using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace UiPath.Scripting.Activities.PowerShell
{
    // An instance of this object is returned to the runtime upon beginning
    // execution.  This also keeps a pointer to a callback function, which
    // it calls when it completes.
    public sealed class PipelineInvokerAsyncResult : IAsyncResult
    {
        /// <summary>
        /// The pointer to the callback function.
        /// </summary>
        private AsyncCallback callback;

        /// <summary>
        /// The state of the activity.
        /// </summary>
        private object asyncState;

        /// <summary>
        /// A handler for the waiting threads.
        /// </summary>
        private EventWaitHandle asyncWaitHandle;

        /// <summary>
        /// A collection of ErrroRecrods.
        /// </summary>
        private Collection<ErrorRecord> errorRecords;

        public Collection<ErrorRecord> ErrorRecords
        {
            get
            {
                if (this.errorRecords == null)
                {
                    this.errorRecords = new Collection<ErrorRecord>();
                }

                return this.errorRecords;
            }
        }

        /// <summary>
        /// The raised exception.
        /// </summary>
        public Exception Exception
        {
            get;
            set;
        }

        /// <summary>
        /// A collection of PSObjects returned by the execution of the command.
        /// </summary>
        public Collection<PSObject> PipelineOutput
        {
            get;
            set;
        }

        /// <summary>
        /// Declared for implementing the interface.
        /// </summary>
        public object AsyncState
        {
            get { return this.asyncState; }
        }

        /// <summary>
        /// Declared for implementing the interface.
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get { return this.asyncWaitHandle; }
        }

        /// <summary>
        /// Declared for implementing the interface.
        /// </summary>
        public bool CompletedSynchronously
        {
            get { return false; }
        }

        /// <summary>
        /// Declared for implementing the interface.
        /// </summary>
        public bool IsCompleted
        {
            get { return true; }
        }

        /// <summary>
        /// Invokes the pipeline.
        /// </summary>
        /// <param name="pipeline"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        public PipelineInvokerAsyncResult(Pipeline pipeline, AsyncCallback callback, object state)
        {
            try
            {
                this.asyncState = state;
                this.callback = callback;
                this.asyncWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                pipeline.StateChanged += new EventHandler<PipelineStateEventArgs>(OnStateChanged);
            }
            catch (Exception ex)
            {
                this.Exception = ex;
                Complete();
                return;
            }

            pipeline.InvokeAsync();
        }

        /// <summary>
        /// Completes the execution of the pipeline.
        /// </summary>
        private void Complete()
        {
            this.asyncWaitHandle.Set();
            if (this.callback != null)
            {
                this.callback(this);
            }
        }

        // Called by the underlying PowerShell pipeline object on state changes.
        private void OnStateChanged(object sender, PipelineStateEventArgs args)
        {
            try
            {
                PipelineState state = args.PipelineStateInfo.State;
                Pipeline pipeline = sender as Pipeline;

                if (state == PipelineState.Completed)
                {
                    this.PipelineOutput = pipeline.Output.ReadToEnd();
                    ReadErrorRecords(pipeline);
                    Complete();
                }
                else if (state == PipelineState.Failed)
                {
                    this.Exception = args.PipelineStateInfo.Reason;
                    ReadErrorRecords(pipeline);
                    Complete();
                }
                else if (state == PipelineState.Stopped)
                {
                    Complete();
                }
                else
                {
                    return; // nothing to do
                }
            }
            catch (Exception e)
            {
                this.Exception = e;
                Complete();
            }
        }

        /// <summary>
        /// Reads the ErrorRecords from the pipeline.
        /// </summary>
        /// <param name="pipeline"></param>
        private void ReadErrorRecords(Pipeline pipeline)
        {
            Collection<object> errorsRecords = pipeline.Error.ReadToEnd();
            if (errorsRecords.Count != 0)
            {
                foreach (PSObject item in errorsRecords)
                {
                    ErrorRecord errorRecord = item.BaseObject as ErrorRecord;
                    this.ErrorRecords.Add(errorRecord);
                }
            }
        }
    }
}
