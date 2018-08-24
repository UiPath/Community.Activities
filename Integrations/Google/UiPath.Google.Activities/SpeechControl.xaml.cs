using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace UiPath.Google.Activities
{
    public partial class SpeechControl : Window
    {
        private double confidence;
        private string language;
        private string serviceAcc;

        public SpeechControl(double confidence, string language, string serviceAcc)
        {
            SourceInitialized += MainWindow_SourceInitialized;
            InitializeComponent();
            stopButton.IsEnabled = false;
            this.confidence = confidence;
            this.language = language;
            this.serviceAcc = serviceAcc;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;

            Recognize.StartRecordingAsync(confidence, language, serviceAcc);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
            this.Close();
        }

        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            WindowInteropHelper wih = new WindowInteropHelper(this);
            int style = GetWindowLong(wih.Handle, GWL_STYLE);
            SetWindowLong(wih.Handle, GWL_STYLE, style & ~WS_SYSMENU);
        }

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x00080000;

        [DllImport("user32.dll")]
        private extern static int SetWindowLong(IntPtr hwnd, int index, int value);
        [DllImport("user32.dll")]
        private extern static int GetWindowLong(IntPtr hwnd, int index);
    }
}
