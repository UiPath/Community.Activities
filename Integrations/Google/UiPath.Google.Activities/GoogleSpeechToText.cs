using System;
using System.Activities;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace UiPath.Google.Activities
{
    public class GoogleSpeechToText : AsyncCodeActivity
    {
        [Category("Input")]
        [Description("Set a confidence level between 0 and 1")]
        [RequiredArgument]
        public InArgument<double> Confidence { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> Language { get; set; }

        [Category("Input")]
        [Description("The service account json file generated from Google Cloud Platform")]
        [RequiredArgument]
        public InArgument<string> ServiceAccountFile { get; set; }

        [Category("Output")]
        public OutArgument<string> Text { get; set; }

        private SpeechControl speechDesign;

        public GoogleSpeechToText()  {  }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var confidence = Confidence.Get(context);
            var language = Language.Get(context);
            var serviceAcc = ServiceAccountFile.Get(context);

            var task = ExecuteAsync(context, confidence, language, serviceAcc);
            var tacs = new TaskCompletionSource<string>(state);

            task.ContinueWith(t =>
            {
                if (t.IsFaulted) tacs.TrySetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled) tacs.TrySetCanceled();
                else tacs.TrySetResult(t.Result);
                callback?.Invoke(tacs.Task);
            });

            return tacs.Task;
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = result as Task<string>;

            if (task.IsFaulted) throw task.Exception.InnerException;
            if (task.IsCanceled || context.IsCancellationRequested)
            {
                context.MarkCanceled();
                return;
            }

            try
            {
                if (Text != null && Text.Expression != null)
                {
                    try
                    {
                        Text.Set(context, task.Result);
                    }
                    catch (InvalidOperationException)
                    {
                        Text.Set(context, TypeDescriptor.GetConverter(Text.ArgumentType).ConvertFrom(task.Result));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                context.MarkCanceled();
            }
        }

        private Task<string> ExecuteAsync(AsyncCodeActivityContext context, double confidence, string language, string serviceAcc)
        {
            speechDesign = new SpeechControl(confidence, language, serviceAcc);
            speechDesign.ShowDialog();

            speechDesign.stopButton.Click += new RoutedEventHandler(StopClickedAsync);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            return Recognize.StopRecording(confidence);
        }

        private void StopClickedAsync(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Stop Pressed");
        }
    }
}
