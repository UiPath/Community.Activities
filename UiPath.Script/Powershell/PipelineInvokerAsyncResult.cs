using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace UiPath.Script.Powershell
{
    // An instance of this object is returned to the runtime upon beginning
    // execution.  This also keeps a pointer to a callback function, which
    // it calls when it completes.
    public class PipelineInvokerAsyncResult : IAsyncResult
    {
        private AsyncCallback callback;
        private object asyncState;
        private EventWaitHandle asyncWaitHandle;

        public AggregateException Exception
        {
            get;
            set;
        }

        public Collection<object> Output
        {
            get;
            set;
        }

        public object AsyncState
        {
            get { return this.asyncState; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return this.asyncWaitHandle; }
        }

        public bool CompletedSynchronously
        {
            get { return false; }
        }

        public bool IsCompleted
        {
            get { return true; }
        }

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
                this.Exception = new AggregateException(ex);
                Complete();
                return;
            }

            pipeline.InvokeAsync();
        }

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
                    var result = pipeline.Output.ReadToEnd();
                    if (result != null)
                    {
                        this.Output = new Collection<object>();
                        foreach (var res in result)
                        {
                            Output.Add(res.BaseObject);
                        }
                    }
                    ReadErrorRecords(pipeline);
                    Complete();
                }
                else if (state == PipelineState.Failed)
                {
                    this.Exception = new AggregateException(args.PipelineStateInfo.Reason);
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
                this.Exception = new AggregateException(e);
                Complete();
            }
        }

        private void ReadErrorRecords(Pipeline pipeline)
        {
            Collection<object> errorsRecords = pipeline.Error.ReadToEnd();
            if (errorsRecords.Count != 0)
            {
                var exceptionList = new List<Exception>();
                foreach (PSObject item in errorsRecords)
                {
                    ErrorRecord errorRecord = item.BaseObject as ErrorRecord;
                    exceptionList.Add(errorRecord.Exception);
                }
                this.Exception = new AggregateException(exceptionList);
            }
        }
    }
}
