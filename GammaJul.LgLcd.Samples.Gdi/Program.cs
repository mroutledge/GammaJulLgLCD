using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;

namespace GammaJul.LgLcd.Samples.Gdi {

	internal static class Program {
		private static readonly Random _random = new Random();
		private static readonly AutoResetEvent _waitAre = new AutoResetEvent(false);
		private static volatile bool _monoArrived;
		private static volatile bool _qvgaArrived;
		private static volatile bool _mustExit;

		/// <summary>
		/// Entry point of the program.
		/// </summary>
		[MTAThread]
		internal static void Main() {
			LcdApplet applet = new LcdApplet("GammaJul LgLcd GDI+ Sample", LcdAppletCapabilities.Both);

			// Register to events to know when a device arrives, then connects the applet to the LCD Manager
			applet.Configure += Applet_Configure;
			applet.DeviceArrival += Applet_DeviceArrival;
			applet.DeviceRemoval += Applet_DeviceRemoval;
			applet.IsEnabledChanged += Applet_IsEnabledChanged;
			applet.Connect();

			// We are waiting for the handler thread to warn us for device arrival
			LcdDeviceMonochrome monoDevice = null;
			_waitAre.WaitOne();

			do {

				// A monochrome device was connected: creates a monochrome device or reopens an old one
				if (_monoArrived) {
					if (monoDevice == null) {
                        monoDevice = (LcdDeviceMonochrome) applet.OpenDeviceByType(LcdDeviceType.Monochrome);
						monoDevice.SoftButtonsChanged += MonoDevice_SoftButtonsChanged;
						CreateMonochromeGdiPages(monoDevice);
					}
					else
						monoDevice.ReOpen();
					_monoArrived = false;
				}

				if (_qvgaArrived) {
					_qvgaArrived = false;
				}

				// We are calling DoUpdateAndDraw in this loop.
				// Note that updating and drawing only happens if the objects in a LcdGdiPage are modified.
				// Even if you call this method very quickly, update and draw will only occur at the frame
				// rate specified by LcdPage.DesiredFrameRate, which is 30 by default.
				if (applet.IsEnabled && monoDevice != null && !monoDevice.IsDisposed)
					monoDevice.DoUpdateAndDraw();

				Thread.Sleep(5);
			}
			while (!_mustExit);
		}

		/// <summary>
		/// Creates two new LcdGdiPages for a monochrome device.
		/// </summary>
		/// <param name="monoDevice">Device to use for page creation.</param>
		private static void CreateMonochromeGdiPages(LcdDevice monoDevice) {

			// Gets the test.bmp image from the assembly
			Image image;
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GammaJul.LgLcd.Samples.Gdi.test.bmp"))
				image = Image.FromStream(stream);

			// Creates first page
			LcdGdiPage page1 = new LcdGdiPage(monoDevice) {
				Children = {
					new LcdGdiImage(image),
					new LcdGdiScrollViewer {
						Child = new LcdGdiText("Hello there! Please press the fourth soft button to exit the program."),
						HorizontalAlignment = LcdGdiHorizontalAlignment.Stretch,
						VerticalAlignment = LcdGdiVerticalAlignment.Stretch,
						Margin = new MarginF(34.0f, 0.0f, 2.0f, 0.0f),
						AutoScrollX = true,
					},
					new LcdGdiProgressBar {
						HorizontalAlignment = LcdGdiHorizontalAlignment.Stretch,
						VerticalAlignment = LcdGdiVerticalAlignment.Top,
						Margin = new MarginF(34.0f, 14.0f, 2.0f, 0.0f),
						Size = new SizeF(0.0f, 7.0f)
					},
					new LcdGdiPolygon(Pens.Black, Brushes.White, new[] {
						new PointF(0.0f, 10.0f), new PointF(5.0f, 0.0f), new PointF(10.0f, 10.0f),
					}, false) {
						HorizontalAlignment = LcdGdiHorizontalAlignment.Center,
						VerticalAlignment = LcdGdiVerticalAlignment.Bottom,
						Margin = new MarginF(0.0f, 0.0f, 0.0f, 5.0f)
					}
				}
			};
			page1.Updating += Page_Updating;

			// Creates second page
			LcdGdiPage page2 = new LcdGdiPage(monoDevice) {
				Children = {
					new LcdGdiRectangle {
						Pen = Pens.Black,
						HorizontalAlignment = LcdGdiHorizontalAlignment.Stretch,
						VerticalAlignment = LcdGdiVerticalAlignment.Stretch
					},
					new LcdGdiLine(Pens.Black, new PointF(0.0f, 0.0f), new PointF(159.0f, 42.0f)) {
						HorizontalAlignment = LcdGdiHorizontalAlignment.Stretch,
						VerticalAlignment = LcdGdiVerticalAlignment.Stretch
					},
					new LcdGdiLine(Pens.Black, new PointF(0.0f, 42.0f), new PointF(159.0f, 0.0f)) {
						HorizontalAlignment = LcdGdiHorizontalAlignment.Stretch,
						VerticalAlignment = LcdGdiVerticalAlignment.Stretch
					}
				}
			};
			page2.GdiDrawing += Page2_GdiDrawing;

			// Adds page to the device's Pages collection (not mandatory, but helps for storing pages),
			// and sets the first page as the current page
			monoDevice.Pages.Add(page1);
			monoDevice.Pages.Add(page2);
			monoDevice.CurrentPage = page1;
		}

		// We are custom drawing 10 random lines on the second page
		private static void Page2_GdiDrawing(object sender, GdiDrawingEventArgs e) {
			for (int i = 0; i < 10; ++i)
				e.Graphics.DrawLine(Pens.Black, _random.Next(160), _random.Next(43), _random.Next(160), _random.Next(43));
			// We have to invalidate the page in order to be able to custom draw the next frame
			((LcdGdiPage) sender).Invalidate();
		}


		/// <summary>
		/// This event handler is called whenever the Configure button is clicked for this applet in the LCD Manager.
		/// </summary>
		private static void Applet_Configure(object sender, EventArgs e) {
			Console.WriteLine("Configure button clicked!");
		}

		/// <summary>
		/// This event handler is called whenever the soft buttons are pressed or released.
		/// </summary>
		private static void MonoDevice_SoftButtonsChanged(object sender, LcdSoftButtonsEventArgs e) {
			LcdDevice device = (LcdDevice) sender;
			Console.WriteLine(e.SoftButtons);

			// First button (remember that buttons start at index 0) is pressed, switch to page one
			if ((e.SoftButtons & LcdSoftButtons.Button0) == LcdSoftButtons.Button0)
				device.CurrentPage = device.Pages[0];

			// Second button is pressed, switch to page two
			if ((e.SoftButtons & LcdSoftButtons.Button1) == LcdSoftButtons.Button1)
				device.CurrentPage = device.Pages[1];

			// Third button is pressed, do a garbage collection (for testing purpose only!)
			if ((e.SoftButtons & LcdSoftButtons.Button2) == LcdSoftButtons.Button2)
				GC.Collect();

			// Fourth button is pressed, exit
			if ((e.SoftButtons & LcdSoftButtons.Button3) == LcdSoftButtons.Button3)
				_mustExit = true;
		}

		/// <summary>
		/// This event handler is called before the page starts its update.
		/// </summary>
		private static void Page_Updating(object sender, UpdateEventArgs e) {
			LcdGdiPage page = (LcdGdiPage) sender;

			// Makes the progress bar fill 10% per second
			LcdGdiProgressBar progressBar = (LcdGdiProgressBar) page.Children[2];
			progressBar.Value = (int) ((e.ElapsedTotalTime.TotalSeconds % 10.0) * 10.0);

			// Makes the polygon blink every 0.5 second
			LcdGdiPolygon polygon = (LcdGdiPolygon) page.Children[3];
			polygon.Brush = e.ElapsedTotalTime.Milliseconds < 500 ? Brushes.White : Brushes.Black;
		}


		/// <summary>
		/// This event handler will be called whenever the user enables or disables the applet
		/// in the LCD Manager's Programs panel.
		/// </summary>
		private static void Applet_IsEnabledChanged(object sender, EventArgs e) {
			Console.WriteLine(((LcdApplet) sender).IsEnabled ? "Applet was enabled." : "Applet was disabled");
		}
        
		/// <summary>
		/// This event handler will be called whenever a new device of a given type arrives in the system.
		/// This is where you should open the device where you want to show the applet.
		/// Take special care for thread-safety as the SDK calls this handler in another thread.
		/// </summary>
		private static void Applet_DeviceArrival(object sender, LcdDeviceTypeEventArgs e) {
			Console.WriteLine("A device of type " + e.DeviceType + " was added.");
			switch (e.DeviceType) {

				// A monochrome device (G13/G15/Z10) was connected
				case LcdDeviceType.Monochrome:
					_monoArrived = true;
					break;

			}
			_waitAre.Set();
		}

		/// <summary>
		/// This event handler will be called whenever every device of a given type are disconnected from the system.
		/// You should stop using the device here.
		/// </summary>
		private static void Applet_DeviceRemoval(object sender, LcdDeviceTypeEventArgs e) {
			Console.WriteLine("A device of type " + e.DeviceType + " was removed.");
		}
        
	}

}
