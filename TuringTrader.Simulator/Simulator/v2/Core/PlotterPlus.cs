//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Plotter Plus
// Description: Trading calendar interface.
// History:     2022x31, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2022, Bertram Enterprises LLC
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

namespace TuringTrader.Simulator.v2
{
    public class PlotterPlus : Plotter
    {
        private Algorithm Algorithm;
        public PlotterPlus(Algorithm algorithm)
        {
            Algorithm = algorithm;
        }


        public void AddTradeLog()
        {
            var log = Algorithm.Account.TradeLog;

            SelectChart("Trade Log", "Date");
            foreach (var trade in Algorithm.Account.TradeLog)
            {
                SetX(string.Format("{0:MM/dd/yyyy}", trade.OrderTicket.SubmitDate));
                // Plot("action", entry.Action);
                // Plot("type", entry.InstrumentType);
                Plot("instr", Algorithm.Asset(trade.OrderTicket.Name).Ticker);
                Plot("qty", string.Format("{0:P2}", trade.OrderSize));
                Plot("fill", string.Format("{0:C2}", trade.FillPrice));
                Plot("gross", string.Format("{0:C2}", trade.OrderAmount + trade.FrictionAmount));
                Plot("friction", string.Format("{0:C2}", trade.FrictionAmount));
                Plot("net", string.Format("{0:C2}", trade.OrderAmount));
                //plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
            }
        }
    }
}

//==============================================================================
// end of file
