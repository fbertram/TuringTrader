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
        closeThisBar,
        /// <summary>
        /// buy or sell assets on the next bar's open
        /// </summary>
        openNextBar,
        // SellStopNextBar,
        // BuyLimitNextBar,
    }

    /// <summary>
    /// The Account class maintains the status of the trading account.
    /// </summary>
    public class Account
    {
        #region internal stuff
        private readonly Algorithm Algorithm;
        private List<OrderTicket> OrderQueue = new List<OrderTicket>();
        private Dictionary<string, double> _Positions = new Dictionary<string, double>();
        private const double INITIAL_CAPITAL = 1000.00;
        private double _Cash = INITIAL_CAPITAL;

        private class OrderTicket
        {
            public readonly string Symbol;
            public readonly double TargetAllocation;
            public readonly OrderType OrderType;

            public OrderTicket(string symbol, double targetAllocation, OrderType orderType)
            {
                Symbol = symbol;
                TargetAllocation = targetAllocation;
                OrderType = orderType;
            }

        }
        private enum NavType
        {
            openThisBar,
            closeThisBar,
            openNextBar,
        }

        private double CalcNetAssetValue(NavType navType = NavType.closeThisBar)
        {
            // FIXME: need to verify that the assets trade on current date.
            // Otherwise, we should probably remove them and issue a warning.

            return _Positions
                .Sum(kv => kv.Value
                    * navType switch
                    {
                        NavType.openThisBar => Algorithm.Asset(kv.Key).Open[0],
                        NavType.closeThisBar => Algorithm.Asset(kv.Key).Close[0],
                        NavType.openNextBar => Algorithm.Asset(kv.Key).Open[-1],
                        _ => throw new ArgumentOutOfRangeException(nameof(navType), $"Unexpected NAV type value: {navType}")
                    })
                + _Cash;
        }
        #endregion

        /// <summary>
        /// Create new account.
        /// </summary>
        /// <param name="algorithm">parent algorithm, to get access to assets and pricing</param>
        public Account(Algorithm algorithm)
        {
            Algorithm = algorithm;
        }

        /// <summary>
        /// Submit order to account
        /// </summary>
        /// <param name="Name">asset name</param>
        /// <param name="weight">asset target allocation</param>
        /// <param name="orderType">order type</param>
        public void SubmitOrder(string Name, double weight, OrderType orderType)
        {
            OrderQueue.Add(
                new OrderTicket(
                    Name, weight, orderType));
        }

        /// <summary>
        /// Process orders in order queue. This should be called at the end of each bar.
        /// </summary>
        public void ProcessOrders()
        {
            foreach (var orderType in new List<OrderType> {
                OrderType.closeThisBar,
                OrderType.openNextBar })
            {
                foreach (var order in OrderQueue.Where(o => o.OrderType == orderType))
                {
                    // FIXME: this code has not been tested with short positions
                    // it is likely that the logic around isBuy and price2 needs to change
                    var price = orderType switch
                    {
                        OrderType.closeThisBar => Algorithm.Asset(order.Symbol).Close[0],
                        OrderType.openNextBar => Algorithm.Asset(order.Symbol).Open[-1],
                        _ => 0.0, // this should never happen
                    };
                    var nav = CalcNetAssetValue(orderType switch
                    {
                        OrderType.closeThisBar => NavType.closeThisBar,
                        OrderType.openNextBar => NavType.openNextBar,
                        _ => 0.0, // this should never happen
                    });

                    var currentShares = _Positions.ContainsKey(order.Symbol) ? _Positions[order.Symbol] : 0;
                    var currentAlloc = currentShares * price / nav;
                    var targetAlloc = Math.Abs(order.TargetAllocation) >= MinPosition ? order.TargetAllocation : 0.0;
                    var isBuy = currentAlloc < targetAlloc;

                    var price2 = isBuy
                        ? price * (1.0 + Friction) // when buying, we reduce the # of shares to cover for commissions
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
                    _Cash -= Math.Abs(deltaShares) * price * Friction;
                }
            }

            OrderQueue.Clear();
        }

        /// <summary>
        /// Return net asset value, relative to initial value.
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
        /// Friction to model commissions, fees, and slippage.
        /// Expressed as percentage of traded value.
        /// </summary>
        public double Friction { get; set; } = 0; // FIXME: 0.0025; // 0.25% per transaction

        /// <summary>
        /// Minimum position size, as percentage of total.
        /// </summary>
        public double MinPosition { get; set; } = 0.001; // 0.1%  minimum position
    }
}

//==============================================================================
// end of file
