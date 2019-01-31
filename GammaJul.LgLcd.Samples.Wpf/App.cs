using GammaJul.LgLcd.Wpf;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace GammaJul.LgLcd.Samples.Wpf
{
    /// <summary>
    /// Application class.
    /// </summary>
    internal class App : Application
    {
        private LcdApplet _applet;
        private DispatcherTimer _timer;
        private SampleControl _sampleControl;
        private LcdDeviceQvga _qvgaDevice;

        private delegate void Action();

        /// <summary>
        /// On startup, we are creation a new Applet.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _applet = new LcdApplet("GammaJul LgLcd WPF Sample", LcdAppletCapabilities.Qvga);

            // Register to events to know when a device arrives, then connects the applet to the LCD Manager
            _applet.DeviceArrival += Applet_DeviceArrival;
            _applet.Connect();
        }

        /// <summary>
        /// Simple utility method to always executes a method on the Application's thread.
        /// </summary>
        /// <param name="action">Method to execute.</param>
        private void Invoke(Action action)
        {
            if (CheckAccess())
                action();
            else
                Dispatcher.BeginInvoke(action, DispatcherPriority.Render);
        }

        /// <summary>
        /// This event handler will be called whenever a new device of a given type arrives in the system.
        /// This is where you should opens the device you want to shows the applet on.
        /// Take special care for thread-safety as the SDK calls this handler in another thread.
        /// </summary>
        private void Applet_DeviceArrival(object sender, LcdDeviceTypeEventArgs e)
        {
            // since with specified LcdAppletCapabilities.Qvga at the Applet's creation,
            // we will only receive QVGA arrival notifications.
            Debug.Assert(e.DeviceType == LcdDeviceType.Qvga);
            Invoke(OnQvgaDeviceArrived);
        }

        private void OnQvgaDeviceArrived()
        {

            // First device arrival, creates the device
            if (_qvgaDevice == null)
            {
                _qvgaDevice = (LcdDeviceQvga)_applet.OpenDeviceByType(LcdDeviceType.Qvga);
                _sampleControl = new SampleControl();
                _qvgaDevice.CurrentPage = new LcdWpfPage(_qvgaDevice)
                {
                    Element = _sampleControl
                };
                _qvgaDevice.SoftButtonsChanged += QvgaDevice_SoftButtonsChanged;

                // Starts a timer to update the screen
                _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(5.0), DispatcherPriority.Render, Timer_Tick, Dispatcher.CurrentDispatcher);
            }

            // Subsequent device arrival means the device was removed and replugged, simply reopens it
            else
                _qvgaDevice.ReOpen();
            _qvgaDevice.DoUpdateAndDraw();
        }

        /// <summary>
        /// Updates the LCD screen.
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_applet.IsEnabled && _qvgaDevice != null && !_qvgaDevice.IsDisposed)
                _qvgaDevice.DoUpdateAndDraw();
        }

        /// <summary>
        /// When soft buttons are pressed, switch to previous image if left arrow button was clicked,
        /// switch to next if the right arrow button was clicked, or closes the application if
        /// the cancel button was clicked.
        /// </summary>
        private void QvgaDevice_SoftButtonsChanged(object sender, LcdSoftButtonsEventArgs e)
        {
            if ((e.SoftButtons & LcdSoftButtons.Cancel) == LcdSoftButtons.Cancel)
                Invoke(Shutdown);
            else if ((e.SoftButtons & LcdSoftButtons.Left) == LcdSoftButtons.Left)
                Invoke(_sampleControl.PreviousImage);
            else if ((e.SoftButtons & LcdSoftButtons.Right) == LcdSoftButtons.Right)
                Invoke(_sampleControl.NextImage);
        }

        [STAThread]
        internal static void Main()
        {
            App app = new App();
            app.Run();
        }
    }

}