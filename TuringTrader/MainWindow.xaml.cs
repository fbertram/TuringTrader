using FUB_TradingSim;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using Avalon.Windows.Dialogs;
using System.Windows.Threading;

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Algorithm _currentAlgorithm = null;
        private string messageUpdate;
        private DateTime lastLogUpdate;
        private DispatcherTimer _dispatcherTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            string path = GlobalSettings.DataPath;
            if (path == null)
            {
                MenuDataLocation_Click(null, null);
            }

            _dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(200);
            _dispatcherTimer.Start();
        }

        private void AlgoSelector_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Type algorithmType in AlgorithmLoader.GetAllAlgorithms().OrderBy(t => t.Name))
                AlgoSelector.Items.Add(algorithmType.Name);
        }

        private void AlgoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = false;
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            ReportButton.IsEnabled = false;
            messageUpdate = "";
            LogOutput.Text = "";

            string algorithmName = AlgoSelector.SelectedItem.ToString();
            _currentAlgorithm = AlgorithmLoader.InstantiateAlgorithm(algorithmName);

            if (_currentAlgorithm != null)
                await Task.Run(() =>
                {
                    DateTime timeStamp1 = DateTime.Now;

                    WriteEventHandler(
                        string.Format("running algorithm {0}", _currentAlgorithm.Name)
                        + Environment.NewLine);
                    _currentAlgorithm.Run();

                    DateTime timeStamp2 = DateTime.Now;
                    WriteEventHandler(
                        string.Format("finished algorithm {0} after {1:F1} seconds", _currentAlgorithm.Name, (timeStamp2 - timeStamp1).TotalSeconds)
                        + Environment.NewLine);
                    WriteEventHandler(""); // will force flush
                });

            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = true;
        }

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            _currentAlgorithm.Report();
        }

        private void WriteEventHandler(string message)
        {
            LogOutput.Dispatcher.BeginInvoke(new Action(() =>
            {
                messageUpdate += message;

                //DateTime timeNow = DateTime.Now;
                //if ((timeNow - lastLogUpdate).TotalMilliseconds < 200 && message.Length > 0)
                //    return;
                //lastLogUpdate = timeNow;

                //LogOutput.AppendText(messageUpdate);
                //messageUpdate = "";
            }));
        }

        private void LogOutput_Loaded(object sender, RoutedEventArgs e)
        {
            Output.WriteEvent += new Output.WriteEventDelegate(WriteEventHandler);
        }

        private void LogOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogOutput.ScrollToEnd();
        }

        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        private void MenuDataLocation_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.Title = "Select Data Location";

            bool? result = folderDialog.ShowDialog();
            if (result == true)
            {
                GlobalSettings.DataPath = folderDialog.SelectedPath;
            }
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            LogOutput.AppendText(messageUpdate);
            messageUpdate = "";
        }
    }
}
