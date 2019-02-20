//==============================================================================
// Project:     TuringTrader
// Name:        OptimizerSettings
// Description: optimizer settings window code-behind
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
#endregion

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
            DialogResult = true;
            Close();
        }

        private void ParamGrid_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            NumIterations.Text = string.Format("Total # of Iterations: {0}", OptimizerGrid.NumIterations(_algorithm));
        }
    }
}

//==============================================================================
// end of file