//==============================================================================
// Project:     TuringTrader
// Name:        OptimizerResults
// Description: optimizer results window code-behind
// History:     2018ix10, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region Libraries
using FUB_TradingSim;
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