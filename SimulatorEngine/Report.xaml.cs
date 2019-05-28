//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Report.xaml.cs
// Description: Report window code-behind
// History:     2019v28, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

#region libraries
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
using System.Windows.Shapes;
using OxyPlot;
using OxyPlot.Series;
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
                if (_selectedChart == null)
                    SelectedChart = AvailableCharts.First();

                return _selectedChart;
            }

            set
            {
                _selectedChart = value;
                Chart.Model = _template.RenderChart(_selectedChart);
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
        }
        #endregion
    }
}

//==============================================================================
// end of file