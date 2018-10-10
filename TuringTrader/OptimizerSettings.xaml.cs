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
        private Algorithm _masterAlgorithm;

        public OptimizerSettings(Algorithm masterAlgorithm)
        {
            InitializeComponent();

            _masterAlgorithm = masterAlgorithm;
            var paramAttributes = _masterAlgorithm.GetParamNames()
                .Select(n => new
                {
                    Name = n,
                    Attribute = _masterAlgorithm.GetParamAttribute(n)
                })
                .ToList();

            ParamGrid.ItemsSource = paramAttributes;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
