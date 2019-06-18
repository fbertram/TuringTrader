//==============================================================================
// Project:     TuringTrader
// Name:        MainWindow
// Description: main window code-behind
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region Libraries
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TuringTrader.Simulator;
#endregion

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
using System.Windows.Navigation;
//using System.Windows.Shapes;

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
        private Algorithm _currentAlgorithm = null;
        private AlgorithmInfo _currentAlgorithmInfo = null;
        private DateTime _currentAlgorithmTimestamp = default(DateTime);
        private OptimizerGrid _optimizer = null;
        private bool _runningBacktest = false;
        private bool _runningOptimization = false;

        private string _messageUpdate;
        private DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        #endregion
        #region internal helpers
        private void Initialize_Once(object sender, EventArgs e)
        {
            _dispatcherTimer.Tick -= Initialize_Once;

            CheckSettings();
            PopulateAlgorithmMenu();
            LoadMostRecentAlgorithm();
        }
        private void CheckSettings()
        {
            //===== check home path
            string path = GlobalSettings.HomePath;

            if (path.Length == 0)
            {
                // create folder in user's documents
                string homePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TuringTrader");

                if (!Directory.Exists(homePath))
                    Directory.CreateDirectory(homePath);

                GlobalSettings.HomePath = homePath;

                // copy files from install directory

                string homeTemplate = Path.Combine(
                    Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName,
                    "Home");

                void copyFolderFiles(string srcPath, string dstPath)
                {
                    DirectoryInfo src = new DirectoryInfo(srcPath);

                    FileInfo[] srcFiles = src.GetFiles();
                    foreach (FileInfo srcFile in srcFiles)
                    {
                        File.Copy(srcFile.FullName, Path.Combine(dstPath, srcFile.Name));
                    }

                    DirectoryInfo[] srcDirs = src.GetDirectories();
                    foreach (DirectoryInfo srcDir in srcDirs)
                    {
                        string dstDir = Path.Combine(dstPath, srcDir.Name);
                        Directory.CreateDirectory(dstDir);
                        copyFolderFiles(srcDir.FullName, dstDir);
                    }
                }

                if (Directory.Exists(homeTemplate))
                    copyFolderFiles(homeTemplate, homePath);
            }
            else if (!Directory.Exists(path))
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
                    Header = "_" + algo.Name,
                    CommandParameter = algo,
                };

                parent.Add(newEntry);
            }
        }
        private void LoadMostRecentAlgorithm()
        {
            string mostRecentAlgorithm = GlobalSettings.MostRecentAlgorithm;
            SelectAlgo(mostRecentAlgorithm);
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
        private string AlgoLookupName(AlgorithmInfo a) => AlgoPathLookupName(a.DisplayPath.Concat(new List<string>() { a.Name }));
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

                Algo.Text = "Algorithm: " + _currentAlgorithm.Name; // this may be different than class name
            }
            else
            {
                Algo.Text = "Algorithm: n/a";
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
        #region private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion
        #region private void MenuEditSettings_Click(object sender, RoutedEventArgs e)
        private void MenuEditSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new Settings();
            settingsDialog.ShowDialog();
        }
        #endregion
        #region private void MenuEditAlgorithm_Click(object sender, RoutedEventArgs e)
        private void MenuEditAlgorithm_Click(object sender, RoutedEventArgs e)
        {
#if true
            // see https://stackoverflow.com/questions/10174156/open-file-with-associated-application
            new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo(_currentAlgorithmInfo.SourcePath)
                {
                    UseShellExecute = true
                }
            }.Start();
#else
            System.Diagnostics.Process.Start(_currentAlgorithmInfo.SourcePath);
#endif
        }
        #endregion
        #region private void MenuAlgorithm_Click(object sender, RoutedEventArgs e)
        private void MenuAlgorithm_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)e.OriginalSource;
            var commandParam = menuItem.CommandParameter;
            var algoType = commandParam as AlgorithmInfo;

            SelectAlgo(AlgoLookupName(algoType));
        }
        #endregion
        #region private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
        private void MenuHelpAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }
        #endregion
        #region private void MenuHelpView_Click(object sender, RoutedEventArgs e)
        private void MenuHelpView_Click(object sender, RoutedEventArgs e)
        {
            string helpFile = Path.Combine(
                Directory.GetParent(Assembly.GetEntryAssembly().Location).FullName,
                "TuringTrader.chm");

            if (File.Exists(helpFile))
                System.Diagnostics.Process.Start(helpFile);
        }
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
