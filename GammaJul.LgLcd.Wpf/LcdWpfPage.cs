using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GammaJul.LgLcd.Wpf
{

    /// <summary>
    /// A <see cref="LcdPage"/> that can be used to show any WPF element.
    /// </summary>
    public class LcdWpfPage : LcdPage
    {
        private readonly RenderTargetBitmap _bitmap;
        private readonly byte[] _32BppPixels;
        private readonly Size _deviceSize;
        private readonly Rect _deviceRect;

        /// <summary>
        /// Gets or sets the element to show.
        /// </summary>
        public FrameworkElement Element { get; set; }


        /// <summary>
        /// Updates the page content.
        /// </summary>
        /// <param name="elapsedTotalTime">Time elapsed since the device creation.</param>
        /// <param name="elapsedTimeSinceLastFrame">Time elapsed since last frame update.</param>
        /// <returns><c>true</c> if the update has done something and a redraw is required.</returns>
        protected override bool UpdateCore(TimeSpan elapsedTotalTime, TimeSpan elapsedTimeSinceLastFrame)
        {
            if (Element == null)
                return false;
            Element.InvalidateVisual();
            Element.Measure(_deviceSize);
            Element.Arrange(_deviceRect);
            return true;
        }

        /// <summary>
        /// Draws the page element.
        /// </summary>
        /// <returns>Implementors must return a pixel array conforming to
        /// <see cref="LcdPage.Device"/>'s <see cref="LcdDevice.DeviceType"/>.</returns>
        protected override byte[] DrawCore()
        {
            _bitmap.Clear();
            if (Element != null)
                _bitmap.Render(Element);
            _bitmap.CopyPixels(_32BppPixels, Device.PixelWidth * 4, 0);
            return _32BppPixels;
        }

        /// <summary>
        /// Creates a new <see cref="LcdWpfPage"/> for a given QVGA device.
        /// This type of page is not supported on a monochrome device.
        /// </summary>
        /// <param name="device">Device on which to create the page.</param>
        public LcdWpfPage(LcdDevice device)
            : base(device)
        {
            if (device.BitsPerPixel != 32)
                throw new NotSupportedException("LcdWpfPage is only supported on 32-bpp devices.");
            _32BppPixels = new byte[device.PixelWidth * device.PixelHeight * 4];
            _bitmap = new RenderTargetBitmap(device.PixelWidth, device.PixelHeight, 96.0, 96.0, PixelFormats.Pbgra32);
            _deviceSize = new Size(device.PixelWidth, device.PixelHeight);
            _deviceRect = new Rect(new Point(), _deviceSize);
        }
    }

}