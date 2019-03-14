//==============================================================================
// Project:     TuringTrader
// Name:        MainWindow
// Description: main window code-behind
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region Libraries
using TuringTrader.Simulator;
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
#endregion

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region internal data
        private Algorithm _currentAlgorithm = null;
        private OptimizerGrid _optimizer = null;
        private bool _runningBacktest = false;
        private bool _runningOptimization = false;

        private string _messageUpdate;
        private DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        #endregion
        #region internal helpers
        private void UpdateParameterDisplay()
        {
            AlgoParameters.Text = "Parameters: "
                + (_currentAlgorithm.OptimizerParams.Count > 0
                    ? _currentAlgorithm.OptimizerParamsAsString
                    : "n/a");
        }
        private void ClearLog()
        {
            _messageUpdate = "";
            LogOutput.Text = "";
        }
        #endregion

        #region public MainWindow()
        public MainWindow()
        {
            InitializeComponent();

            //--- set data location
            string path = GlobalSettings.HomePath;
            if (path == null)
            {
                MenuDataLocation_Click(null, null);
            }

            //--- set timer event
            _dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(200);
            _dispatcherTimer.Start();

            //--- initialize algorithm selector
            var allAlgorithms = TuringTrader.Simulator.AlgorithmLoader.GetAllAlgorithms();

            foreach (Type algorithm in allAlgorithms)
                AlgoSelector.Items.Add(algorithm.Name);

            // attempt to recover most-recent algo
            string mostRecentAlgorithm = GlobalSettings.MostRecentAlgorithm;
            if (mostRecentAlgorithm != default(string))
            {
                AlgoSelector.SelectedIndex = allAlgorithms
                    .FindIndex(t => t.Name == mostRecentAlgorithm);
            }

            //--- redirect log output
            Output.WriteEvent += WriteEventHandler;
        }
        #endregion

        //----- log
        #region private void WriteEventHandler(string message)
        private void WriteEventHandler(string message)
        {
            LogOutput.Dispatcher.BeginInvoke(new Action(() =>
            {
                _messageUpdate += message;

                //DateTime timeNow = DateTime.Now;
                //if ((timeNow - lastLogUpdate).TotalMilliseconds < 200 && message.Length > 0)
                //    return;
                //lastLogUpdate = timeNow;

                //LogOutput.AppendText(messageUpdate);
                //messageUpdate = "";
            }));
        }
        #endregion
        #region private void DispatcherTimer_Tick(object sender, EventArgs e)
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            LogOutput.AppendText(_messageUpdate);
            _messageUpdate = "";

            if (_runningBacktest)
            {
                Progress.Visibility = Visibility.Visible;
                Progress.Value = _currentAlgorithm.Progress;
            }
            else if (_runningOptimization)
            {
                Progress.Visibility = Visibility.Visible;
                Progress.Value = _optimizer.Progress;
            }
            else
            {
                Progress.Visibility = Visibility.Hidden;
            }
        }
        #endregion
        #region private void LogOutput_TextChanged(object sender, TextChangedEventArgs e)
        private void LogOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogOutput.ScrollToEnd();
        }
        #endregion

        //----- menu
        #region private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion
        #region private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
        private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }
        #endregion
        #region private void MenuDataLocation_Click(object sender, RoutedEventArgs e)
        private void MenuDataLocation_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog()
            {
                Title = "Select TuringTrader Home Location"
            };

            bool? result = folderDialog.ShowDialog();
            if (result == true)
            {
                GlobalSettings.HomePath = folderDialog.SelectedPath;
            }
        }
        #endregion

        //----- buttons
        #region private void AlgoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        private void AlgoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string algorithmName = AlgoSelector.SelectedItem.ToString();
            _currentAlgorithm = AlgorithmLoader.InstantiateAlgorithm(algorithmName);
            _optimizer = null;

            UpdateParameterDisplay();
            ClearLog();

            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = false;
            OptimizerButton.IsEnabled = _currentAlgorithm.OptimizerParams.Count > 0;
            ResultsButton.IsEnabled = false;
        }
        #endregion
        #region private async void RunButton_Click(object sender, RoutedEventArgs e)
        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            ReportButton.IsEnabled = false;
            bool saveOptimizerButton = OptimizerButton.IsEnabled;
            OptimizerButton.IsEnabled = false;
            bool saveResultsButton = ResultsButton.IsEnabled;
            ResultsButton.IsEnabled = false;
            AlgoSelector.IsEnabled = false;
            _runningBacktest = true;

            ClearLog();

            if (_currentAlgorithm != null)
                await Task.Run(() =>
                {
                    DateTime timeStamp1 = DateTime.Now;

                    WriteEventHandler(
                        string.Format("running algorithm {0}", _currentAlgorithm.Name)
                        + Environment.NewLine);
                    try
                    {
                        _currentAlgorithm.Run();
                    }
                    catch (Exception exception)
                    {
                        WriteEventHandler(
                            string.Format("EXCEPTION: {0}{1}", exception.Message, exception.StackTrace)
                            + Environment.NewLine);
                    }

                    DateTime timeStamp2 = DateTime.Now;
                    WriteEventHandler(
                        string.Format("finished algorithm {0} after {1:F1} seconds", _currentAlgorithm.Name, (timeStamp2 - timeStamp1).TotalSeconds)
                        + Environment.NewLine);
                    //WriteEventHandler(""); // will force flush
                });

            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = true;
            OptimizerButton.IsEnabled = saveOptimizerButton;
            ResultsButton.IsEnabled = saveResultsButton;
            AlgoSelector.IsEnabled = true;
            _runningBacktest = false;

            ReportButton_Click(null, null);
        }
        #endregion
        #region private void ReportButton_Click(object sender, RoutedEventArgs e)
        private async void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentAlgorithm != null)
                await Task.Run(() =>
                {
                    try
                    {
                        _currentAlgorithm.Report();
                    }
                    catch (Exception exception)
                    {
                        WriteEventHandler(
                            string.Format("EXCEPTION: {0}{1}", exception.Message, exception.StackTrace)
                            + Environment.NewLine);
                    }

                });
        }
        #endregion
        #region private async void OptimizeButton_Click(object sender, RoutedEventArgs e)
        private async void OptimizeButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            ReportButton.IsEnabled = false;
            OptimizerButton.IsEnabled = false;
            ResultsButton.IsEnabled = false;
            AlgoSelector.IsEnabled = false;

            ClearLog();

            var optimizerSettings = new OptimizerSettings(_currentAlgorithm);
            if (optimizerSettings.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    _optimizer = new OptimizerGrid(_currentAlgorithm);
                    _runningOptimization = true;

                    _optimizer.Run();

                    LogOutput.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        RunButton.IsEnabled = true;
                        ReportButton.IsEnabled = false;
                        OptimizerButton.IsEnabled = true;
                        ResultsButton.IsEnabled = true;
                        AlgoSelector.IsEnabled = true;
                        _runningOptimization = false;

                        ResultsButton_Click(null, null);
                    }));
                });
            }
            else
            {
                RunButton.IsEnabled = true;
                ReportButton.IsEnabled = false;
                OptimizerButton.IsEnabled = true;
                ResultsButton.IsEnabled = false;
                AlgoSelector.IsEnabled = true;
                _runningOptimization = false;
            }
        }
        #endregion
        #region private void ResultsButton_Click(object sender, RoutedEventArgs e)
        private void ResultsButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            ReportButton.IsEnabled = false;
            OptimizerButton.IsEnabled = false;
            ResultsButton.IsEnabled = false;
            AlgoSelector.IsEnabled = false;

            var optimizerResults = new OptimizerResults(_optimizer);

            bool paramsChanged = optimizerResults.ShowDialog() == true;

            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = false;
            OptimizerButton.IsEnabled = true;
            ResultsButton.IsEnabled = true;
            AlgoSelector.IsEnabled = true;

            if (paramsChanged)
            {
                UpdateParameterDisplay();

                RunButton_Click(null, null);
            }
        }
        #endregion

        private void MenuDefaultExtensionXlsm_Click(object sender, RoutedEventArgs e)
        {
            GlobalSettings.DefaultTemplateExtension = ".xlsm";
        }

        private void MenuDefaultExtensionR_Click(object sender, RoutedEventArgs e)
        {
            GlobalSettings.DefaultTemplateExtension = ".r";
        }

        private void MenuDefaultExtensionRmd_Click(object sender, RoutedEventArgs e)
        {
            GlobalSettings.DefaultTemplateExtension = ".rmd";
        }
    }
}

//==============================================================================
// end of file