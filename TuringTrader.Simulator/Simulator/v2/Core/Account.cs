//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Account
// Description: Account class.
// History:     2022x25, FUB, created
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuringTrader.Simulator.v2
{
    /// <summary>
    /// OrderType is an enumeration of orders supported by the
    /// simulator.
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// buy or sell assets on this bar's close
        /// </summary>
        MarketThisClose,
        /// <summary>
        /// buy or sell assets on the next bar's open
        /// </summary>
        MarketNextOpen,
        // SellStopNextBar,
        // BuyLimitNextBar,
    }

    /// <summary>
    /// The OrderTicket class bundles the information for an order.
    /// This class uses an abstract order concept, where the direction of
    /// the trade is typically determined by the difference between the
    /// current and the target allocation.
    /// </summary>
    public class OrderTicket
    {
        /// <summary>
        /// Ticker symbol for this order.
        /// </summary>
        public readonly string Symbol;
        /// <summary>
        /// Target allocation to achieve with this order
        /// </summary>
        public readonly double TargetAllocation;
        /// <summary>
        /// Order type to use for this order.
        /// </summary>
        public readonly OrderType OrderType;

        /// <summary>
        /// Create new order ticket.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="orderType"></param>
        /// <param name="targetAllocation"></param>
        public OrderTicket(string symbol, double targetAllocation, OrderType orderType)
        {
            Symbol = symbol;
            TargetAllocation = targetAllocation;
            OrderType = orderType;
        }
    }

    /// <summary>
    /// The Account class maintains the status of the trading account.
    /// </summary>
    public class Account
    {
        private readonly Algorithm Algorithm;
        private List<OrderTicket> OrderQueue = new List<OrderTicket>();
        private Dictionary<string, double> _Positions = new Dictionary<string, double>();
        private double _Cash = 1000;

        public Account(Algorithm algorithm)
        {
            Algorithm = algorithm;
        }

        public void SubmitOrder(OrderTicket orderTicket)
        {
            OrderQueue.Add(orderTicket);
        }

        public void ProcessOrders()
        {
            var orderTypes = new List<OrderType> { OrderType.MarketThisClose, OrderType.MarketNextOpen };
            foreach (var orderType in orderTypes)
            {
                foreach (var order in OrderQueue.Where(o => o.OrderType == orderType))
                {
                    // FIXME: right now, this code is long-only
                    var atNextOpen = orderType != OrderType.MarketThisClose;
                    var price = atNextOpen
                        ? Algorithm.Asset(order.Symbol).Open[-1]
                        : Algorithm.Asset(order.Symbol).Close[0];
                    var nav = CalcNetAssetValue(atNextOpen);
                    var currentShares = _Positions.ContainsKey(order.Symbol) ? _Positions[order.Symbol] : 0;
                    var currentAlloc = currentShares * price / nav;
                    var isBuy = currentAlloc < order.TargetAllocation;
                    var targetShares = isBuy
                        ? currentShares + nav * (order.TargetAllocation - currentAlloc) / (price * (1.0 + Commission))
                        : (order.TargetAllocation > MinPosition ? nav * order.TargetAllocation / price : 0.0);

                    if (targetShares > 0)
                        _Positions[order.Symbol] = targetShares;
                    else
                        _Positions.Remove(order.Symbol);

                    _Cash -= (targetShares - currentShares) * price;
                    _Cash -= Math.Abs(targetShares - currentShares) * price * Commission;
                }
            }

            OrderQueue.Clear();
        }

        public IEnumerable<KeyValuePair<string, double>> Positions
        {
            get
            {
                foreach (var kv in _Positions)
                    yield return kv;
            }
        }

        public double NetAssetValue
        {
            get => CalcNetAssetValue();
        }

        public double CalcNetAssetValue(bool atNextOpen = false)
        {
            return Positions
                .Sum(kv => kv.Value 
                    * (atNextOpen == false 
                        ? Algorithm.Asset(kv.Key).Close[0] 
                        : Algorithm.Asset(kv.Key).Open[-1]))
                + _Cash;
        }

        public double Commission { get; set; } = 0.005; // 0.5% per transaction
        public double MinPosition { get; set; } = 0.001; // 0.1%  minimum position
    }
}

//==============================================================================
// end of file
