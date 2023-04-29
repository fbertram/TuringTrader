﻿//==============================================================================
// Project:     TuringTrader
// Name:        MainWindow
// Description: main window code-behind
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

#region Libraries
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TuringTrader.Optimizer;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region data model
        public class MenuItemViewModel
        {
            //private readonly ICommand _command;

            public MenuItemViewModel()
            {
                //_command = new CommandViewModel(Execute);
            }

            public string Header { get; set; }
            public object CommandParameter { get; set; }

            public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }

            //public ICommand Command
            //{
            //    get
            //    {
            //        return _command;
            //    }
            //}

            //private void Execute()
            //{
            //    // (NOTE: In a view model, you normally should not use MessageBox.Show()).
            //    MessageBox.Show("Clicked at " + Header);
            //}
        }
        /*public class CommandViewModel : ICommand
        {
            private readonly Action _action;

            public CommandViewModel(Action action)
            {
                _action = action;
            }

            public void Execute(object o)
            {
                _action();
            }

            public bool CanExecute(object o)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { }
                remove { }
            }
        }*/
        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }
        #endregion
        #region internal data
        private IAlgorithm _currentAlgorithm = null;
        private AlgorithmInfo _currentAlgorithmInfo = null;
        private DateTime _currentAlgorithmTimestamp = default(DateTime);
        private OptimizerGrid _optimizer = null;
        private bool _runningBacktest = false;
        private bool _runningOptimization = false;

        private string _messageUpdate;
        private DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        #endregion
        #region internal helpers
        public static void OpenWithShell(string fileOrUrl)
        {
            try
            {
#if true
                // see https://stackoverflow.com/questions/10174156/open-file-with-associated-application
                new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo(fileOrUrl)
                    {
                        UseShellExecute = true
                    }
                }.Start();
#else
            System.Diagnostics.Process.Start(fileOrUrl);
#endif
            }
            catch
            {
                Output.WriteLine("Can't open {0}", fileOrUrl);
            }
        }
        private void Initialize_Once(object sender, EventArgs e)
        {
            _dispatcherTimer.Tick -= Initialize_Once;

            CheckSettings();

            WriteEventHandler(string.Format("Version App = {0}, Engine = {1}\n", GitInfo.Version, SimulatorV2.GlobalSettings.Version));
            WriteEventHandler(string.Format("Home Path = {0}\n", GlobalSettings.HomePath));
            WriteEventHandler(string.Format("Console Mode = {0}\n\n", GlobalSettings.ConsoleMode));

            UpdateHomeDir();
            PopulateAlgorithmMenu();
            LoadMostRecentAlgorithm();

            // register various plotter renderers
            PlotterRenderExcel.Register();
            PlotterRenderCSharp.Register();
            PlotterRenderR.Register();
            PlotterRenderRMarkdown.Register();
        }

        private void UpdateHomeDir()
        {
            //===== initialize home directory

            // we can only initialize home directory and home template exist
#if true
            var homeTemplate = Path.Combine(
                Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName,
                "..",
                "Home");
#else
            // debugging only
            var homeTemplate = @"C:\Program Files\TuringTrader\Home";
#endif

            if (Directory.Exists(GlobalSettings.HomePath) && Directory.Exists(homeTemplate))
            {
                var codeVersion = GitInfo.Version;
                var versionFile = Path.Combine(GlobalSettings.HomePath, "home-version.txt");
                var homeVersion = File.Exists(versionFile) ? File.ReadAllText(versionFile) : "n/a";

                if (codeVersion != homeVersion)
                {
                    WriteEventHandler(string.Format("updating home directory from {0} to {1}\n", homeVersion, codeVersion));
                    File.WriteAllText(versionFile, codeVersion); // write back version

                    // --- cleanup phase
                    // (1) go through list with file checksums and delete each file unless
                    //     (a) file checksum different than list
                    //     (b) file missing
                    // --- update phase
                    // (2) copy all files from app, unless
                    //     (a) they already exist in the destination
                    //     (b) they were found missing in step (1)
                    // (3) for each file copied, save version number to list

                    var checksumFile = Path.Combine(GlobalSettings.HomePath, "file-checksums.txt");
                    var fileChecksums = new Dictionary<string, string>();
                    var fileDeleted = new List<string>();

                    // load checksums
                    if (File.Exists(checksumFile))
                    {
                        fileChecksums = File.ReadLines(checksumFile)
                            .ToDictionary(
                                line => line.Substring(0, line.IndexOf("=")),
                                line => line.Substring(line.IndexOf("=") + 1));
                    }

                    // calculate checksums
                    string CalculateMD5(string filePath)
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(filePath))
                            {
                                var hash = md5.ComputeHash(stream);
                                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            }
                        }
                    }

                    // cleanup phase: delete files with matching checksums
                    foreach (var keyValue in fileChecksums)
                    {
                        var filePath = Path.Combine(GlobalSettings.HomePath, keyValue.Key);

                        if (File.Exists(filePath))
                        {
                            var checksum = CalculateMD5(filePath);

                            if (checksum == keyValue.Value)
                                File.Delete(filePath);
                            else
                                WriteEventHandler(string.Format("    keeping modified file {0}\n", filePath));
                        }
                        else
                        {
                            WriteEventHandler(string.Format("    skipping deleted file {0}\n", filePath));
                            fileDeleted.Add(keyValue.Key);
                        }
                    }

                    //--- update phase: copy files from install folder
                    void copyFolderFiles(string relPath = null)
                    {
                        var srcPath = relPath != null ? Path.Combine(homeTemplate, relPath) : homeTemplate;
                        var dstPath = relPath != null ? Path.Combine(GlobalSettings.HomePath, relPath) : GlobalSettings.HomePath;
                        var srcInfo = new DirectoryInfo(srcPath);

                        var srcFiles = srcInfo.GetFiles();
                        foreach (var src in srcFiles)
                        {
                            var relFile = relPath != null ? Path.Combine(relPath, src.Name) : src.Name;
                            var srcFile = Path.Combine(srcPath, src.Name);
                            var dstFile = Path.Combine(dstPath, src.Name);

                            // NOTE: it is important we keep a checksum entry for
                            //       each file that should be there, so that we may
                            //       reset any modifications we made at some point
                            fileChecksums[relFile] = CalculateMD5(srcFile);

                            if (!File.Exists(dstFile) && !fileDeleted.Contains(relFile))
                            {
                                if (relPath != null && !Directory.Exists(dstPath))
                                    Directory.CreateDirectory(dstPath);

                                File.Copy(srcFile, dstFile);
                            }
                        }

                        var srcDirs = srcInfo.GetDirectories();
                        foreach (var src in srcDirs)
                        {
                            var nextRelPath = relPath != null ? Path.Combine(relPath, src.Name) : src.Name;
                            copyFolderFiles(nextRelPath);
                        }
                    }

                    fileChecksums.Clear();
                    copyFolderFiles();

                    File.WriteAllLines(checksumFile, fileChecksums.Select(kv => string.Format("{0}={1}", kv.Key, kv.Value)));
                }
            }
        }
        private void CheckSettings()
        {
            //===== check home path

            // on first launch, create home directory in default location
            if (GlobalSettings.HomePath.Length == 0)
            {
                GlobalSettings.HomePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TuringTrader");

                if (!Directory.Exists(GlobalSettings.HomePath))
                    Directory.CreateDirectory(GlobalSettings.HomePath);
            }

            // if home directory doesn't exist, ask user to set new one
            if (!Directory.Exists(GlobalSettings.HomePath))
            {
                MessageBox.Show("Please set TuringTrader's home folder");
                MenuEditSettings_Click(null, null);
            }

            //===== check Tiingo API key
            if (GlobalSettings.DefaultDataFeed == "Tiingo" && GlobalSettings.TiingoApiKey.Length < 10)
            {
                MessageBox.Show("Please set Tiingo API key");
                MenuEditSettings_Click(null, null);
            }
        }
        private void PopulateAlgorithmMenu()
        {
            var allAlgorithms = TuringTrader.Simulator.AlgorithmLoader.GetAllAlgorithms();

            // NOTE: this is done in the MainWindow constructor
            //MenuItems = new ObservableCollection<MenuItemViewModel>();

            var map = new Dictionary<string, ObservableCollection<MenuItemViewModel>>();
            map["/"] = MenuItems;

            // 1) create sub-menu structure
            foreach (var algo in allAlgorithms)
            {
                string algoPath = AlgoPathLookupName(algo.DisplayPath);

                if (!map.ContainsKey(algoPath))
                {
                    for (int i = 1; i <= algo.DisplayPath.Count; i++)
                    {
                        var parentPath = AlgoPathLookupName(algo.DisplayPath.Take(i - 1));
                        var newPath = AlgoPathLookupName(algo.DisplayPath.Take(i));

                        var newEntry = new MenuItemViewModel
                        {
                            Header = algo.DisplayPath[i - 1],
                            MenuItems = new ObservableCollection<MenuItemViewModel>(),
                        };

                        if (!map.ContainsKey(newPath))
                        {
                            map[newPath] = newEntry.MenuItems;
                            map[parentPath].Add(newEntry);
                        }
                    }
                }
            }

            // 2) add individual entries
            foreach (var algo in allAlgorithms)
            {
                var parent = map[AlgoPathLookupName(algo.DisplayPath)];
                var newEntry = new MenuItemViewModel
                {
#if true
                    // only display algorithm name
                    Header = "_" + algo.Name,
#else
                    // display algorithm name along with simulator engine version
                    Header = "_" + algo.Name + (algo.IsV2Algorithm ? " (V2)" : " (V1)"),
#endif
                    CommandParameter = algo,
                };

                parent.Add(newEntry);
            }
        }
        private void LoadMostRecentAlgorithm()
        {
            string mostRecentAlgorithm = GlobalSettings.MostRecentAlgorithm;

            try
            {
                SelectAlgo(mostRecentAlgorithm);
            }
            catch (Exception /*e*/)
            {
                Output.WriteLine("Failed to instantiate {0}", mostRecentAlgorithm);
            }
        }
        private void UpdateParameterDisplay()
        {
            if (_currentAlgorithm == null)
            {
                AlgoParameters.Text = "Parameters: n/a";
                return;
            }

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
        private string AlgoPathLookupName(IEnumerable<string> dp) => dp.Aggregate("/", (p, n) => p + n + "/");
        private string AlgoLookupName(AlgorithmInfo a) => a != null
            ? AlgoPathLookupName(a.DisplayPath.Concat(new List<string>() { a.Name }))
            : "";
        private void SelectAlgo(string algoLookupName)
        {
            ClearLog();

            _currentAlgorithm = null;
            _currentAlgorithmInfo = null;
            _optimizer = null;

            RunButton.IsEnabled = false;
            MenuEditAlgorithm.IsEnabled = false;
            ReportButton.IsEnabled = false;
            OptimizerButton.IsEnabled = false;
            ResultsButton.IsEnabled = false;

            var allAlgorithms = TuringTrader.Simulator.AlgorithmLoader.GetAllAlgorithms();

            var matchedAlgorithms = allAlgorithms
                .Where(t => AlgoLookupName(t) == algoLookupName)
                .ToList();

            if (matchedAlgorithms.Count == 1)
            {
                AlgorithmInfo algoInfo = matchedAlgorithms.First();
                _currentAlgorithmInfo = algoInfo;

                GlobalSettings.MostRecentAlgorithm = algoLookupName;

                _currentAlgorithm = AlgorithmLoader.InstantiateAlgorithm(algoInfo);

                _currentAlgorithmTimestamp = _currentAlgorithmInfo.SourcePath != null
                    ? (new FileInfo(_currentAlgorithmInfo.SourcePath).LastWriteTime)
                    : default(DateTime);
            }

            UpdateParameterDisplay();

            MenuEditAlgorithm.IsEnabled = _currentAlgorithmInfo != null && _currentAlgorithmInfo.SourcePath != null;

            if (_currentAlgorithm != null)
            {
                RunButton.IsEnabled = true;
                ReportButton.IsEnabled = false;
                OptimizerButton.IsEnabled = _currentAlgorithm.OptimizerParams.Count > 0;
                ResultsButton.IsEnabled = false;

                Title = "TuringTrader - " + _currentAlgorithm.Name;
                WriteEventHandler(string.Format("loaded algorithm {0}\n", _currentAlgorithm.Name));
            }
            else
            {
                Title = "TuringTrader";
            }
        }
        private bool ControlPressed => Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        #endregion

        #region public MainWindow()
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            MenuItems = new ObservableCollection<MenuItemViewModel>();

            //--- redirect log output
            Output.WriteEvent += WriteEventHandler;

            //--- set timer event
            _dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            _dispatcherTimer.Tick += new EventHandler(Initialize_Once);
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(200);
            _dispatcherTimer.Start();
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
            // set Run button label
            RunButton.Content = ControlPressed
                ? "Debug"
                : "Run";

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
        #region menu file/ exit
        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();
        #endregion
        #region menu file/ algorithm/ ...
        private void MenuAlgorithm_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)e.OriginalSource;
            var commandParam = menuItem.CommandParameter;
            var algoType = commandParam as AlgorithmInfo;

            try
            {
                SelectAlgo(AlgoLookupName(algoType));
            }
            catch (Exception /*exc*/)
            {
                Output.WriteLine("failed to instantiate {0}", algoType);
            }
        }
        #endregion
        #region menu edit/ settings
        private void MenuEditSettings_Click(object sender, RoutedEventArgs e)
            => new Settings().ShowDialog();
        #endregion
        #region menu edit/ algorithm source code
        private void MenuEditAlgorithm_Click(object sender, RoutedEventArgs e)
            => OpenWithShell(_currentAlgorithmInfo.SourcePath);
        #endregion
        #region menu edit/ home directory
        private void MenuEditHome_Click(object sender, RoutedEventArgs e)
            => OpenWithShell(GlobalSettings.HomePath);
        #endregion
        #region menu help/ about
        private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
            => new AboutBox().ShowDialog();
        #endregion
        #region menu help/ check for updates
        private void MenuHelpUpdate_Click(object sender, RoutedEventArgs e)
            => OpenWithShell("https://www.turingtrader.org/download/");
        #endregion
        #region menu help/ view online help
        private void MenuHelpView_Click(object sender, RoutedEventArgs e)
            => OpenWithShell("https://www.turingtrader.org/help/");
        #endregion

        //----- buttons
        #region private async void RunButton_Click(object sender, RoutedEventArgs e)
        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            ReportButton.IsEnabled = false;
            bool saveOptimizerButton = OptimizerButton.IsEnabled;
            OptimizerButton.IsEnabled = false;
            bool saveResultsButton = ResultsButton.IsEnabled;
            ResultsButton.IsEnabled = false;
            AlgoMenu.IsEnabled = false;
            _runningBacktest = true;

            bool debug = ControlPressed;

            DateTime algorithmTimeStamp = _currentAlgorithmInfo.SourcePath != null
                ? (new FileInfo(_currentAlgorithmInfo.SourcePath).LastWriteTime)
                : default(DateTime);

            if (algorithmTimeStamp != _currentAlgorithmTimestamp)
            {
                // algorithm instance out of date. re-instantiate!
                SelectAlgo(AlgoLookupName(_currentAlgorithmInfo));
            }

            ClearLog();

            if (_currentAlgorithm != null)
                await Task.Run(() =>
                {
                    void uiThread()
                    {
                        DateTime timeStamp1 = DateTime.Now;

#if true
                        // replace current instance with a freshly cloned instance
                        // this helps run poorly initialized algorithms
                        var clonedAlgorithm = _currentAlgorithm.Clone();
                        _currentAlgorithm = clonedAlgorithm;

                        if (_optimizer != null)
                            _optimizer.MasterInstance = _currentAlgorithm;
#endif

                        WriteEventHandler(
                            string.Format("running algorithm {0}", _currentAlgorithm.Name)
                            + Environment.NewLine);
                        try
                        {
#if true
                            if (debug)
                                System.Diagnostics.Debugger.Launch();
#endif
                            // make sure to enter the extended Run method
                            // the default implementation will forward
                            // to the simple Run method, if required
                            // also, we need to convert the result to a list,
                            // in order to circumvent lazy execution
                            //var noLazyExec = _currentAlgorithm.Run(null, null)
                            //    .ToList();
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
                    }
#if false
                    // run on same thread as ui
                    uiThread();
#else
                    // run each simulation on new thread
                    Thread thread = new Thread(uiThread);
                    thread.Start();
                    thread.Join(); // wait for window to close
#endif
                });

            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = true;
            OptimizerButton.IsEnabled = saveOptimizerButton;
            ResultsButton.IsEnabled = saveResultsButton;
            AlgoMenu.IsEnabled = true;
            _runningBacktest = false;

            ReportButton_Click(null, null);
        }
        #endregion
        #region private void ReportButton_Click(object sender, RoutedEventArgs e)
        private async void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            ReportButton.IsEnabled = false;
            bool saveOptimizerButton = OptimizerButton.IsEnabled;
            OptimizerButton.IsEnabled = false;
            bool saveResultsButton = ResultsButton.IsEnabled;
            ResultsButton.IsEnabled = false;
            AlgoMenu.IsEnabled = false;
            _runningBacktest = true;

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

            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = true;
            OptimizerButton.IsEnabled = saveOptimizerButton;
            ResultsButton.IsEnabled = saveResultsButton;
            AlgoMenu.IsEnabled = true;
            _runningBacktest = false;
        }
        #endregion
        #region private async void OptimizeButton_Click(object sender, RoutedEventArgs e)
        private async void OptimizeButton_Click(object sender, RoutedEventArgs e)
        {
            RunButton.IsEnabled = false;
            ReportButton.IsEnabled = false;
            OptimizerButton.IsEnabled = false;
            ResultsButton.IsEnabled = false;
            AlgoMenu.IsEnabled = false;

            ClearLog();

            var optimizerSettings = new OptimizerSettings(_currentAlgorithm);
            if (optimizerSettings.ShowDialog() == true)
            {
                if (OptimizerGrid.NumIterations(_currentAlgorithm) == 1)
                {
                    // just a single iteration, no need to run optimizer
                    RunButton.IsEnabled = true;
                    ReportButton.IsEnabled = false;
                    OptimizerButton.IsEnabled = true;
                    ResultsButton.IsEnabled = true;
                    AlgoMenu.IsEnabled = true;
                    _runningOptimization = false;

                    UpdateParameterDisplay();

                    RunButton_Click(null, null);
                }
                else
                {
                    // run  optimizer in background
                    await Task.Run(() =>
                    {
                        _optimizer = new OptimizerGrid(_currentAlgorithm);
                        _runningOptimization = true;

                        try
                        {
                            _optimizer.Run();
                        }
                        catch (Exception exception)
                        {
                            WriteEventHandler(
                                string.Format("EXCEPTION: {0}{1}", exception.Message, exception.StackTrace)
                                + Environment.NewLine);
                        }

                        LogOutput.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            RunButton.IsEnabled = true;
                            ReportButton.IsEnabled = false;
                            OptimizerButton.IsEnabled = true;
                            ResultsButton.IsEnabled = true;
                            AlgoMenu.IsEnabled = true;
                            _runningOptimization = false;

                            ResultsButton_Click(null, null);
                        }));
                    });
                }
            }
            else
            {
                RunButton.IsEnabled = true;
                ReportButton.IsEnabled = false;
                OptimizerButton.IsEnabled = true;
                ResultsButton.IsEnabled = false;
                AlgoMenu.IsEnabled = true;
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
            AlgoMenu.IsEnabled = false;

            var optimizerResults = new OptimizerResults(_optimizer);

            bool paramsChanged = optimizerResults.ShowDialog() == true;

            RunButton.IsEnabled = true;
            ReportButton.IsEnabled = false;
            OptimizerButton.IsEnabled = true;
            ResultsButton.IsEnabled = true;
            AlgoMenu.IsEnabled = true;

            if (paramsChanged)
            {
                UpdateParameterDisplay();

                RunButton_Click(null, null);
            }
        }
        #endregion
    }
}

//==============================================================================
// end of file
