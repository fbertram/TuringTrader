//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Account
// Description: Account class.
// History:     2022x25, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
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

namespace TuringTrader.SimulatorV2
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
        private readonly Algorithm _algorithm;
        private List<OrderTicket> _orderQueue = new List<OrderTicket>();
        private List<OrderReceipt> _tradeLog = new List<OrderReceipt>();
        private Dictionary<string, double> _positions = new Dictionary<string, double>();
        private const double INITIAL_CAPITAL = 1000.00;
        private double _cash = INITIAL_CAPITAL;
        private double _navNextOpen = 0.0;
        private DateTime _firstDate = default(DateTime);
        private DateTime _lastDate = default(DateTime);
        private double _navMax = 0.0;
        private double _mdd = 0.0;
        private const double DEFAULT_FRICTION = 0.0005; // $100.00 x 0.05% = $0.05
        private double _friction = DEFAULT_FRICTION;

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

            return _positions
                .Sum(kv => kv.Value
                    * navType switch
                    {
                        NavType.openThisBar => _algorithm.Asset(kv.Key).Open[0],
                        NavType.closeThisBar => _algorithm.Asset(kv.Key).Close[0],
                        NavType.openNextBar => _algorithm.Asset(kv.Key).Open[-1],
                        _ => throw new ArgumentOutOfRangeException(nameof(navType), $"Unexpected NAV type value: {navType}")
                    })
                + _cash;
        }
        #endregion

        /// <summary>
        /// Create new account.
        /// </summary>
        /// <param name="algorithm">parent algorithm, to get access to assets and pricing</param>
        public Account(Algorithm algorithm)
        {
            _algorithm = algorithm;
        }

        /// <summary>
        /// Submit and queue order.
        /// </summary>
        /// <param name="Name">asset name</param>
        /// <param name="weight">asset target allocation</param>
        /// <param name="orderType">order type</param>
        public void SubmitOrder(string Name, double weight, OrderType orderType)
        {
            _orderQueue.Add(
                new OrderTicket(
                    Name, weight, orderType, _algorithm.SimDate));
        }

        /// <summary>
        /// Process bar. This method will loop through the queued
        /// orders, execute them as required, and return a bar
        /// representing the strategy's NAV.
        /// </summary>
        /// <returns></returns>
        public OHLCV ProcessBar()
        {
            if (_firstDate == default)
                _firstDate = _algorithm.SimDate;
            if (_lastDate < _algorithm.SimDate)
                _lastDate = _algorithm.SimDate;

            var navOpen = 0.0; ;
            var navClose = 0.0;

            foreach (var orderType in new List<OrderType> {
                OrderType.closeThisBar,
                OrderType.openNextBar })
            {
                var navType = orderType switch
                {
                    OrderType.closeThisBar => NavType.closeThisBar,
                    _ => _algorithm.IsLastBar
                        ? NavType.closeThisBar
                        : NavType.openNextBar,
                };

                var execDate = orderType switch
                {
                    OrderType.closeThisBar => _algorithm.SimDate,
                    _ => _algorithm.NextSimDate,
                };

                //----- process orders
                foreach (var order in _orderQueue.Where(o => o.OrderType == orderType))
                {
                    // FIXME: this code has not been tested with short positions
                    // it is likely that the logic around isBuy and price2 needs to change
                    var price = orderType switch
                    {
                        OrderType.closeThisBar => _algorithm.Asset(order.Name).Close[0],
                        // for all other order types, we assume they are executed
                        // at the opening price. this will not be true for limit
                        // and stop orders, but we see no better alternative, as
                        // the asset's high and low prices are not aligned in time.
                        _ => _algorithm.IsLastBar
                            ? _algorithm.Asset(order.Name).Close[0]
                            : _algorithm.Asset(order.Name).Open[-1],
                    };

                    // we need to calculate the nav every time we get
                    // here due to trading friction.
                    var nav = CalcNetAssetValue(navType);

                    var currentShares = _positions.ContainsKey(order.Name) ? _positions[order.Name] : 0;
                    var currentAlloc = currentShares * price / nav;
                    var targetAlloc = Math.Abs(order.TargetAllocation) >= MinPosition ? order.TargetAllocation : 0.0;

#if false
                    // TODO: determine if this is helpful
                    if (Math.Abs(targetAlloc - currentAlloc) < MinPosition && targetAlloc != 0.0)
                        continue;
#endif

                    if (currentAlloc == 0.0 && targetAlloc == 0.0)
                        continue;

                    {
                        // TODO: move this block to a virtual function
                        //       which we can overload to implement
                        //       alternative fill models.
                        // parameters
                        //   - order ticket
                        //   - nominal fill price
                        //   - current shares
                        //   - current allocation
                        //   - target allocation
                        // required operation
                        //   - adjust _Positions
                        //   - adjust _Cash
                        //   - add to _TradeLog

                        var isBuy = currentAlloc < targetAlloc;

                        var price2 = isBuy
                            ? price * (1.0 + Friction) // when buying, we reduce the # of shares to cover for commissions
                            : price;
                        var deltaShares = nav * (targetAlloc - currentAlloc) / price2;

                        if (targetAlloc != 0.0)
                        {
                            var targetShares = currentShares + deltaShares;
                            _positions[order.Name] = targetShares;
                        }
                        else
                        {
                            _positions.Remove(order.Name);
                        }

                        var orderAmount = deltaShares * price;
                        var frictionAmount = Math.Abs(deltaShares) * price * Friction;
                        _cash -= orderAmount;
                        _cash -= frictionAmount;

                        if (!_algorithm.IsOptimizing)
                            _tradeLog.Add(new OrderReceipt(
                                order,
                                execDate,
                                targetAlloc - currentAlloc,
                                price,
                                orderAmount,
                                frictionAmount));
                    }
                }

                //----- save NAV
                switch (orderType)
                {
                    case OrderType.closeThisBar:
                        navClose = CalcNetAssetValue(NavType.closeThisBar);
                        break;
                    case OrderType.openNextBar:
                        navOpen = _algorithm.IsFirstBar ? INITIAL_CAPITAL : _navNextOpen;
                        _navNextOpen = CalcNetAssetValue(NavType.openNextBar);
                        break;
                }
            }

            _orderQueue.Clear();

            //----- calculate NAV at open and close
            // FIXME: as we calculate this after trades on the next
            // day have been executed, the asset allocation used
            // is that after tomorrow's open. This is obviously
            // incorrect.
            var navHigh = Math.Max(navOpen, navClose);
            var navLow = Math.Min(navOpen, navClose);
            _navMax = Math.Max(_navMax, navClose);
            _mdd = Math.Max(_mdd, 1.0 - navClose / _navMax);

            return new OHLCV(navOpen, navHigh, navLow, navClose, 0);
        }

        /// <summary>
        /// Return net asset value in currency, starting with $1,000
        /// at the beginning of the simulation. Note that currency
        /// has no relevance throughout the v2 engine. We use this
        /// value to make the NAV more tangible during analysis and
        /// debugging.
        /// </summary>
        public double NetAssetValue { get => CalcNetAssetValue(); }

        /// <summary>
        /// Calculate annualized return over the full simulation range.
        /// </summary>
        public double AnnualizedReturn { get => Math.Pow(CalcNetAssetValue() / INITIAL_CAPITAL, 365.25 / (_lastDate - _firstDate).TotalDays) - 1.0; }

        /// <summary>
        /// Return maximum drawdown over the full simulation range.
        /// </summary>
        public double MaxDrawdown { get => _mdd; }

        /// <summary>
        /// Return positions, as fraction of NAV.
        /// </summary>
        public Dictionary<string, double> Positions
        {
            get
            {
                var nav = CalcNetAssetValue();
                var result = new Dictionary<string, double>();
                foreach (var kv in _positions)
                {
                    result[kv.Key] = kv.Value * _algorithm.Asset(kv.Key).Close[0] / nav;
                }
                return result;
            }
        }

        /// <summary>
        /// Return of cash available, as fraction of NAV.
        /// </summary>
        public double Cash { get => _cash / CalcNetAssetValue(); }

        /// <summary>
        /// Container collecting all order information at time of order submittal.
        /// </summary>
        public class OrderTicket
        {
            /// <summary>
            /// Asset name.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Asset target allocation, as fraction of NAV.
            /// </summary>
            public readonly double TargetAllocation;

            /// <summary>
            /// Order type.
            /// </summary>
            public readonly OrderType OrderType;

            /// <summary>
            /// Order submit date.
            /// </summary>
            public readonly DateTime SubmitDate;

            /// <summary>
            /// Create new order ticket.
            /// </summary>
            /// <param name="symbol"></param>
            /// <param name="targetAllocation"></param>
            /// <param name="orderType"></param>
            /// <param name="submitDate"></param>
            public OrderTicket(string symbol, double targetAllocation, OrderType orderType, DateTime submitDate)
            {
                Name = symbol;
                TargetAllocation = targetAllocation;
                OrderType = orderType;
                SubmitDate = submitDate;
            }
        }

        /// <summary>
        /// Container collecting all order information at time of order execution.
        /// </summary>
        public class OrderReceipt
        {
            /// <summary>
            /// Order ticket.
            /// </summary>
            public readonly OrderTicket OrderTicket;

            /// <summary>
            /// Order execution date.
            /// </summary>
            public readonly DateTime ExecDate;

            /// <summary>
            /// Order size, as a fraction of NAV.
            /// </summary>
            public readonly double OrderSize;

            /// <summary>
            /// Order fill price.
            /// </summary>
            public readonly double FillPrice;

            /// <summary>
            /// Currency spent/received for assets traded. Note that throughout
            /// the v2 engine, currency has no significance. This is only to
            /// make trades more tangible while analyzing and debugging.
            /// </summary>
            public readonly double OrderAmount;

            /// <summary>
            /// Currency lost for trade friction. Note that throughout the v2
            /// engine, currency has no significance. This is only to make trades
            /// more tangible while analyzing and debugging.
            /// </summary>
            public readonly double FrictionAmount;

            /// <summary>
            /// Create order receipt.
            /// </summary>
            /// <param name="orderTicket"></param>
            /// <param name="orderSize"></param>
            /// <param name="fillPrice"></param>
            /// <param name="orderAmount"></param>
            /// <param name="frictionAmount"></param>
            public OrderReceipt(OrderTicket orderTicket,
                DateTime execDate,
                double orderSize,
                double fillPrice,
                double orderAmount,
                double frictionAmount)
            {
                OrderTicket = orderTicket;
                ExecDate = execDate;
                OrderSize = orderSize;
                FillPrice = fillPrice;
                OrderAmount = orderAmount;
                FrictionAmount = frictionAmount;
            }
        }

        /// <summary>
        /// Retrieve trade log. Note that this log only contains trades executed
        /// and not orders that were not executed.
        /// </summary>
        public List<OrderReceipt> TradeLog { get { return new List<OrderReceipt>(_tradeLog); } }

        /// <summary>
        /// Friction to model commissions, fees, and slippage.
        /// Expressed as percentage of traded value.
        /// </summary>
        public double Friction { get => _friction; set { _friction = value >= 0.0 ? value : DEFAULT_FRICTION; } }

        /// <summary>
        /// Minimum position size, as percentage of total.
        /// </summary>
        public double MinPosition { get; set; } = 0.001; // 0.1%  minimum position
    }
}

//==============================================================================
// end of file
