using Google.Apis.Sheets.v4;
using System;
using System.Activities;
using System.Threading.Tasks;

namespace GoogleSpreadsheet.Activities
{
    /// <summary>
    /// Google Interop Activity with result type <T>
    /// </summary>
    public abstract class GoogleInteropActivity<T> : AsyncCodeActivity<T>
    {
        public string SpreadsheetId { get; private set; }

        protected GoogleInteropActivity()
        {
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var property = context.DataContext.GetProperties()[GoogleSheetApplicationScope.GoogleSheetPropertyTag];
            var googleSheetProperty = property.GetValue(context.DataContext) as GoogleSheetProperty;

            var sheetsService = googleSheetProperty.SheetsService;
            SpreadsheetId = googleSheetProperty.SpreadsheetId;

            var task = ExecuteAsync(context, sheetsService);
            var tcs = new TaskCompletionSource<T>(state);

            task.ContinueWith(t =>
            {
                if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled) tcs.TrySetCanceled();
                else tcs.TrySetResult(t.Result);
                if (callback != null) callback(tcs.Task);
            });

            return tcs.Task;
        }

        protected override T EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = result as Task<T>;

            if (task.IsFaulted) throw task.Exception.InnerException;
            if (task.IsCanceled || context.IsCancellationRequested)
            {
                context.MarkCanceled();
            }

            return task.Result;
        }

        protected abstract Task<T> ExecuteAsync(AsyncCodeActivityContext context, SheetsService sheetService);
    }

    /// <summary>
    /// Google Interop Activity without result type <T>
    /// </summary>
    public abstract class GoogleInteropActivity : AsyncCodeActivity
    {
        public string SpreadsheetId { get; private set; }

        protected GoogleInteropActivity()
        {
        }
        
        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var property = context.DataContext.GetProperties()[GoogleSheetApplicationScope.GoogleSheetPropertyTag];
            var googleSheetProperty = property.GetValue(context.DataContext) as GoogleSheetProperty;

            var sheetsService = googleSheetProperty.SheetsService;
            SpreadsheetId = googleSheetProperty.SpreadsheetId;

            var task = ExecuteAsync(context, sheetsService);
            var tcs = new TaskCompletionSource<object>(state);

            task.ContinueWith(t =>
            {
                if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled) tcs.TrySetCanceled();
                
                if (callback != null) callback(tcs.Task);
            });

            return tcs.Task;
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = result as Task;

            if (task.IsFaulted) throw task.Exception.InnerException;
            if (task.IsCanceled || context.IsCancellationRequested)
            {
                context.MarkCanceled();
            }
        }

        protected abstract Task ExecuteAsync(AsyncCodeActivityContext context, SheetsService sheetService);
    }
}
