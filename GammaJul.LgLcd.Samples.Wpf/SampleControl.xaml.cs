using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GammaJul.LgLcd.Samples.Wpf {

	/// <summary>
	/// This simple controls loads images from the %windir%\Web\Wallpaper directory
	/// and provides function to switch between them.
	/// </summary>
	public partial class SampleControl {
		private readonly List<ImageSource> _images = new List<ImageSource>();
		private int _currentIndex = -1;

		/// <summary>
		/// Switch to the previous image.
		/// </summary>
		public void PreviousImage() {
			if (_images.Count == 0)
				return;
			if (--_currentIndex < 0)
				_currentIndex = _images.Count - 1;
			Img.Source = _images[_currentIndex];
		}

		/// <summary>
		/// Switch to the next image.
		/// </summary>
		public void NextImage() {
			if (_images.Count == 0)
				return;
			if (++_currentIndex >= _images.Count)
				_currentIndex = 0;
			Img.Source = _images[_currentIndex];
		}

		/// <summary>
		/// Creates a new <see cref="SampleControl"/>.
		/// </summary>
		public SampleControl() {
			InitializeComponent();
			string wallpaperPath = Path.Combine(Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.System)), "Web\\Wallpaper");
			foreach (string file in Directory.GetFiles(wallpaperPath, "*.jpg", SearchOption.AllDirectories))
				_images.Add(new BitmapImage(new Uri(file, UriKind.Absolute)));
			NextImage();
		}
	}

}
