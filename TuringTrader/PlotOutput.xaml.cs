using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TuringTrader.Simulator;

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for PlotOutput.xaml
    /// </summary>
    public partial class PlotOutput : Window
    {
        private readonly Algorithm _algorithm;
        private double _width = double.NaN;
        private double _height = double.NaN;

        public PlotOutput(Algorithm algorithm)
        {
            InitializeComponent();
            _algorithm = algorithm;
        }

        public async Task<bool> RenderReport()
        {
            double newWidth = PlotImage.ActualWidth != 0
                ? PlotImage.ActualWidth
                : 1920;
            double newHeight = PlotImage.ActualHeight != 0
                ? PlotImage.ActualHeight
                : 1080;

            if (_width == newWidth
            && _height == newHeight)
                return true;

            // render report
            _width = newWidth;
            _height = newHeight;
            int dpi = 96; // device independent pixels are 96 dpi
            byte[] bitmapData = await Task.Run(() =>
            {
                return _algorithm.Report((int)_width, (int)_height, dpi);
            });

            // check, if we rendered an image
            if (bitmapData == null)
                return false;

            // convert to Bitmap:
            // var bitmap = new Bitmap(new MemoryStream(data));

            // set image control to rendered image, and show dialog
            using (var memory = new MemoryStream(bitmapData))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                PlotImage.Source = bitmapImage;
                PlotImage.Stretch = Stretch.Fill;

                //PlotImage.Background = new ImageBrush(bitmapImage);
                //PlotImage.Child = bitmapImage;
            }

            return true;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("w = {0}, h = {1}", PlotImage.ActualWidth, PlotImage.ActualHeight));
        }
    }
}
