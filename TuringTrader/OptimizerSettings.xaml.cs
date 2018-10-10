using FUB_TradingSim;
using System;
using System.Collections.Generic;
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

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for OptimizerSettings.xaml
    /// </summary>
    public partial class OptimizerSettings : Window
    {
        private Algorithm _algorithm;

        public OptimizerSettings(Algorithm algorithm)
        {
            InitializeComponent();

            _algorithm = algorithm;
            ParamGrid.ItemsSource = _algorithm.OptimizerParams.Values.ToList();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
