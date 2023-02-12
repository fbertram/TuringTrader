//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Account_Default
// Description: Default account class.
// History:     2022x25, FUB, created
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace TuringTrader.SimulatorV2
{
    /// <summary>
    /// The Account class maintains the status of the trading account.
    /// </summary>
    public class Account_Default : IAccount
    {
        #region internal stuff
        private readonly Algorithm _algorithm;
        private List<IAccount.OrderTicket> _orderQueue = new List<IAccount.OrderTicket>();
        private List<IAccount.OrderReceipt> _tradeLog = new List<IAccount.OrderReceipt>();
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
        public Account_Default(Algorithm algorithm)
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
                new IAccount.OrderTicket(
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

            var navOpen = 0.0;
            var navClose = 0.0;

            foreach (var orderTypeFilter in new List<OrderType> {
                OrderType.closeThisBar,
                OrderType.openNextBar })
            {
                // execute pending next-day orders on close of last bar
                var orderType = orderTypeFilter switch
                {
                    OrderType.openNextBar => _algorithm.IsLastBar ? OrderType.closeThisBar : orderTypeFilter,
                    _ => orderTypeFilter,
                };

                var navType = orderType switch
                {
                    OrderType.closeThisBar => NavType.closeThisBar,
                    _ => NavType.openNextBar,
                };

                var execDate = orderType switch
                {
                    OrderType.closeThisBar => _algorithm.SimDate,
                    _ => _algorithm.NextSimDate,
                };

                //----- process orders
                foreach (var order in _orderQueue.Where(o => o.OrderType == orderTypeFilter))
                {
                    var orderAsset = _algorithm.Asset(order.Name);

                    // FIXME: this code has not been tested with short positions
                    // it is likely that the logic around isBuy and price2 needs to change
                    var price = orderType switch
                    {
                        OrderType.closeThisBar => _algorithm.Asset(order.Name).Close[0],
                        // for all other order types, we assume they are executed
                        // at the opening price. this will not be true for limit
                        // and stop orders, but we see no better alternative, as
                        // the asset's high and low prices are not aligned in time.
                        _ => _algorithm.Asset(order.Name).Open[-1],
                    };

                    // we need to calculate the nav every time we get
                    // here due to trading friction.
                    var nav = CalcNetAssetValue(navType);

                    var currentShares = _positions.ContainsKey(order.Name) ? _positions[order.Name] : 0;
                    var currentAlloc = currentShares * price / nav;
                    var targetAlloc = Math.Abs(order.TargetAllocation) >= MinPosition ? order.TargetAllocation : 0.0;

#if false
                    // TODO: determine if skipping small orders is helpful
                    //       it is unclear how much this really increases execution speed.
                    //       At the same time, it seems that for an equal-weighted index,
                    //       this optimization might result in about 0.2% deviation in CAGR
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
                            _tradeLog.Add(new IAccount.OrderReceipt(
                                order,
                                execDate,
                                targetAlloc - currentAlloc,
                                price,
                                orderAmount,
                                frictionAmount));
                    }
                }

                //----- save NAV
                switch (orderTypeFilter)
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
        /// Retrieve trade log. Note that this log only contains trades executed
        /// and not orders that were not executed.
        /// </summary>
        public List<IAccount.OrderReceipt> TradeLog { get { return new List<IAccount.OrderReceipt>(_tradeLog); } }

        /// <summary>
        /// Friction to model commissions, fees, and slippage
        /// expressed as a fraction of the traded value. A value of 0.01 is 
        /// equivalent to 1% of friction. Setting Friction to a negative 
        /// value will reset it to its default setting.
        /// </summary>
        public double Friction { get => _friction; set { _friction = value >= 0.0 ? value : DEFAULT_FRICTION; } }

        /// <summary>
        /// Minimum position size, as fraction of total account value. A
        /// value of 0.01 is equivalent to a minimum position of 1% of the
        /// account's liquidation value.
        /// </summary>
        public double MinPosition { get; set; } = 0.001; // 0.1%  minimum position
    }
}

//==============================================================================
// end of file
