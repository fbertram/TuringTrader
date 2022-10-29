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
        private const double INITIAL_CAPITAL = 1000.00;
        private double _Cash = INITIAL_CAPITAL;

        /// <summary>
        /// Create new account.
        /// </summary>
        /// <param name="algorithm">parent algorithm, to get access to assets and pricing</param>
        public Account(Algorithm algorithm)
        {
            Algorithm = algorithm;
        }

        /// <summary>
        /// Submit order to account.
        /// </summary>
        /// <param name="orderTicket">order ticket</param>
        public void SubmitOrder(OrderTicket orderTicket)
        {
            OrderQueue.Add(orderTicket);
        }

        /// <summary>
        /// Process orders in order queue. This should be called at the end of each bar.
        /// </summary>
        public void ProcessOrders()
        {
            foreach (var orderType in new List<OrderType> {
                OrderType.MarketThisClose,
                OrderType.MarketNextOpen })
            {
                foreach (var order in OrderQueue.Where(o => o.OrderType == orderType))
                {
                    // FIXME: this code has not been tested with short positions
                    // it is likely that the logic around isBuy and price2 needs to change
                    var atNextOpen = orderType != OrderType.MarketThisClose;
                    var price = atNextOpen
                        ? Algorithm.Asset(order.Symbol).Open[-1]
                        : Algorithm.Asset(order.Symbol).Close[0];
                    var nav = CalcNetAssetValue(atNextOpen);

                    var currentShares = _Positions.ContainsKey(order.Symbol) ? _Positions[order.Symbol] : 0;
                    var currentAlloc = currentShares * price / nav;
                    var targetAlloc = Math.Abs(order.TargetAllocation) >= MinPosition ? order.TargetAllocation : 0.0;
                    var isBuy = currentAlloc < targetAlloc;

                    var price2 = isBuy
                        ? price * (1.0 + Commission) // when buying, we reduce the # of shares to cover for commissions
                        : price;
                    var deltaShares = nav * (targetAlloc - currentAlloc) / price2;

                    if (targetAlloc != 0.0)
                    {
                        var targetShares = currentShares + deltaShares;
                        _Positions[order.Symbol] = targetShares;
                    }
                    else
                    {
                        _Positions.Remove(order.Symbol);
                    }

                    _Cash -= deltaShares * price;
                    _Cash -= Math.Abs(deltaShares) * price * Commission;
                }
            }

            OrderQueue.Clear();
        }

        private double CalcNetAssetValue(bool atNextOpen = false)
        {
            return _Positions
                .Sum(kv => kv.Value
                    * (atNextOpen == false
                        ? Algorithm.Asset(kv.Key).Close[0]
                        : Algorithm.Asset(kv.Key).Open[-1]))
                + _Cash;
        }

        /// <summary>
        /// Return net asset value, relative to initial value
        /// </summary>
        public double NetAssetValue
        {
            get => CalcNetAssetValue() / INITIAL_CAPITAL;
        }

        /// <summary>
        /// Return positions, as fraction of net asset value.
        /// </summary>
        public Dictionary<string, double> Positions
        {
            get
            {
                var nav = CalcNetAssetValue();
                var result = new Dictionary<string, double>();
                foreach (var kv in _Positions)
                {
                    result[kv.Key] = kv.Value * Algorithm.Asset(kv.Key).Close[0] / nav;
                }
                return result;
            }
        }

        /// <summary>
        /// Return of cash available, as fraction of net asset value.
        /// </summary>
        public double Cash { get => _Cash / CalcNetAssetValue(); }

        /// <summary>
        /// Commission, as percentage of traded value.
        /// </summary>
        public double Commission { get; set; } = 0.005; // 0.5% per transaction

        /// <summary>
        /// Minimum position size, as percentage of total.
        /// </summary>
        public double MinPosition { get; set; } = 0.001; // 0.1%  minimum position
    }
}

//==============================================================================
// end of file
