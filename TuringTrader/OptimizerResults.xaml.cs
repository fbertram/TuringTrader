//==============================================================================
// Project:     TuringTrader
// Name:        OptimizerResults
// Description: optimizer results window code-behind
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TuringTrader.Optimizer;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for OptimizerResults.xaml
    /// </summary>
    public partial class OptimizerResults : Window
    {
        private OptimizerGrid _optimizer;

        public OptimizerResults(OptimizerGrid optimizer)
        {
            InitializeComponent();

            _optimizer = optimizer;
            ResultGrid.ItemsSource = _optimizer.Results;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            _optimizer.SetParametersFromResult((OptimizerResult)row.Item);

            DialogResult = true;
            Close();
        }
    }
}

//==============================================================================
// end of file