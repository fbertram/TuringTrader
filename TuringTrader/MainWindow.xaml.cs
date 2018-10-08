using FUB_TradingSim;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Algorithm _currentAlgorithm = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AlgoSelector_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Type algorithmType in AlgorithmLoader.GetAllAlgorithms())
                AlgoSelector.Items.Add(algorithmType.Name);
        }

        private void AlgoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = false;
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            ReportButton.IsEnabled = false;

            string algorithmName = AlgoSelector.SelectedItem.ToString();
            _currentAlgorithm = AlgorithmLoader.InstantiateAlgorithm(algorithmName);

            if (_currentAlgorithm != null)
                _currentAlgorithm.Run();

            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = true;
        }

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            _currentAlgorithm.Report();
        }
    }
}
