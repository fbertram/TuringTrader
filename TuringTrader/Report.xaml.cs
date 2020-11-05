//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Report.xaml.cs
// Description: Report window code-behind
// History:     2019v28, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
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

#region libraries
using Microsoft.Win32;
using OxyPlot;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Interaction logic for Report.xaml
    /// </summary>
    public partial class Report : Window
    {
        #region internal data
        private ReportTemplate _template;
        private string _selectedChart = null;
        #endregion

        #region public IEnumerable<string> AvailableCharts
        /// <summary>
        /// Property w/ available charts, data-source to chart selector combo-box
        /// </summary>
        public IEnumerable<string> AvailableCharts
        {
            get => _template.AvailableCharts;
        }
        #endregion
        #region public string SelectedChart
        /// <summary>
        /// Property w/ selected chart, data-source to chart selector combo-box
        /// </summary>
        public string SelectedChart
        {
            get
            {
                if (_selectedChart == null && AvailableCharts.Count() > 0)
                    SelectedChart = AvailableCharts.First();

                return _selectedChart;
            }

            set
            {
                _selectedChart = value;

                try
                {
                    object model = _template.GetModel(_selectedChart);

                    if (model.GetType() == typeof(PlotModel))
                    {
                        // OxyPlot model
                        PlotModel plotModel = (PlotModel)model;

                        Chart.Visibility = Visibility.Visible;
                        ChartSave.IsEnabled = true;
                        Chart.Model = plotModel;

                        Table.Visibility = Visibility.Hidden;
                        TableSave.IsEnabled = false;
                        Table.Columns.Clear();
                        Table.Items.Clear();
                    }
                    else
                    {
                        // List<Dictionary<string, object>>
                        List<Dictionary<string, object>> tableModel = (List<Dictionary<string, object>>)model;

                        Chart.Visibility = Visibility.Hidden;
                        ChartSave.IsEnabled = false;
                        Chart.Model = null;

                        Table.Visibility = Visibility.Visible;
                        TableSave.IsEnabled = true;
                        Table.Columns.Clear();
                        Table.Items.Clear();

                        var columns = tableModel
                            .SelectMany(row => row.Keys)
                            .Distinct()
                            .ToList();

                        foreach (var c in columns)
                            Table.Columns.Add(new DataGridTextColumn()
                            {
                                Header = c,
                                Binding = new Binding(string.Format("[{0}]", c)),
                            });

                        foreach (var r in tableModel)
                            Table.Items.Add(r);
                    }
                }
                catch
                {
                    Output.WriteLine("can't render {0}", value);
                }
            }
        }
        #endregion

        #region public Report(ReportTemplate template)
        /// <summary>
        ///  Report window constructor
        /// </summary>
        /// <param name="template">report template instance</param>
        public Report(ReportTemplate template)
        {
            _template = template;

            InitializeComponent();
            DataContext = this;

            // TODO: this needs clean up
            string tmp = GlobalSettings.MostRecentAlgorithm.Substring(0, GlobalSettings.MostRecentAlgorithm.Length - 1);
            string strategyName = tmp.Substring(tmp.LastIndexOf('/') + 1);
            Title = "Strategy Report - " + strategyName;
        }
        #endregion

        #region private void SaveAsPng(object sender, RoutedEventArgs e)
        private void SaveAsPng(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG file (*.png)|*.png";

            if (saveFileDialog.ShowDialog() == true)
                _template.SaveAsPng(SelectedChart, saveFileDialog.FileName);
        }
        #endregion
        #region private void SaveAsCsv(object sender, RoutedEventArgs e)
        private void SaveAsCsv(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV file (*.csv)|*.csv";

            if (saveFileDialog.ShowDialog() == true)
                _template.SaveAsCsv(SelectedChart, saveFileDialog.FileName);
        }
        #endregion
    }
}

//==============================================================================
// end of file