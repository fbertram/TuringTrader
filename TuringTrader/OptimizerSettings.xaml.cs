//==============================================================================
// Project:     TuringTrader
// Name:        OptimizerSettings
// Description: optimizer settings window code-behind
// History:     2018ix10, FUB, created
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

#region Libraries
using System.Linq;
using System.Windows;
using System.Windows.Data;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for OptimizerSettings.xaml
    /// </summary>
    public partial class OptimizerSettings : Window
    {
        private IAlgorithm _algorithm;

        public OptimizerSettings(IAlgorithm algorithm)
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