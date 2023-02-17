//==============================================================================
// Project:     TuringTrader
// Name:        Settings
// Description: settings dialog code-behind
// History:     2019v13, FUB, created
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

using Avalon.Windows.Dialogs;
using System.Windows;
using TuringTrader.Simulator;

namespace TuringTrader
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        #region data model
        public string HomePath
        {
            get
            {
                string homePath = GlobalSettings.HomePath ?? "";
                if (homePath.Length > 40)
                {
                    string left = homePath.Substring(0, 15);
                    string right = homePath.Substring(homePath.Length - 25, 25);
                    homePath = left + "..." + right;
                }
                return homePath;
            }
        }
        public string TiingoApiKey
        {
            get => GlobalSettings.TiingoApiKey;
            set => GlobalSettings.TiingoApiKey = value;
        }

        public string DefaultDataSource
        {
            get => GlobalSettings.DefaultDataFeed;
            set => GlobalSettings.DefaultDataFeed = value;
        }
        public string DefaultTemplateExtension
        {
            get => GlobalSettings.DefaultTemplateExtension;
            set => GlobalSettings.DefaultTemplateExtension = value;
        }

        public string ConsoleMode
        {
            get => SimulatorV2.GlobalSettings.ConsoleMode;
            set => SimulatorV2.GlobalSettings.ConsoleMode = value;
        }
        #endregion

        public Settings()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void HomePathButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new FolderBrowserDialog()
            {
                Title = "Select TuringTrader Home Location",
                //RootSpecialFolder = System.Environment.SpecialFolder.MyDocuments,
                RootSpecialFolder = System.Environment.SpecialFolder.UserProfile,
                //RootSpecialFolder = System.Environment.SpecialFolder.MyComputer,
            };

            var result = folderDialog.ShowDialog();
            if (result == true)
            {
                GlobalSettings.HomePath = folderDialog.SelectedPath;
            }

            LabelHomePath.Content = HomePath;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

//==============================================================================
// end of file