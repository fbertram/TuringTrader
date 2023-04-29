//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Indicators/Volume
// Description: Volume indicators.
// History:     2022xi02, FUB, created
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

namespace TuringTrader.SimulatorV2.Indicators
{
    /// <summary>
    /// Collection of volume indicators.
    /// </summary>
    public static class Volume
    {
        #region AccumulationDistributionIndex
        /// <summary>
        /// Accumulation/ distribution index as described here:
        /// <see href="https://en.wikipedia.org/wiki/Accumulation/distribution_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>accumulation/ distribution index as time series</returns>
        public static TimeSeriesFloat AccumulationDistributionIndex(this TimeSeriesAsset series)
        {
            var name = string.Format("{0}.AccumulationDistributionIndex", series.Name);

            return series.Owner.Lambda(
                name,
                (prev) => prev + series.Volume[0] * series.CLV()[0],
                0.0);
        }
        #endregion
        #region ChaikinOscillator
        /// <summary>
        /// Chaikin oscillator as described here:
        /// <see href="https://en.wikipedia.org/wiki/Accumulation/distribution_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>accumulation/ distribution index as time series</returns>
        public static TimeSeriesFloat ChaikinOscillator(this TimeSeriesAsset series)
        {
            var adl = series.AccumulationDistributionIndex();

            return adl.EMA(3)
                .Sub(adl.EMA(10));
        }
        #endregion
        #region OnBalanceVolume
        /// <summary>
        /// Calculate On-Balance Volume indicator.
        /// <see href="https://en.wikipedia.org/wiki/On-balance_volume"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <returns>OBV time series</returns>
        public static TimeSeriesFloat OnBalanceVolume(this TimeSeriesAsset series)
        {
            var name = string.Format("{0}.OnBalanceVolume", series.Name);

            return series.Owner.Lambda(
                name,
                (prev) => series.Close[0] > series.Close[1]
                    ? prev + series.Volume[0]
                    : prev - series.Volume[0],
                0.0);
        }
        #endregion
        #region MoneyFlowIndex
        /// <summary>
        /// Calculate Money Flow Index indicator
        /// <see href="https://en.wikipedia.org/wiki/Money_flow_index"/>
        /// </summary>
        /// <param name="series">input time series</param>
        /// <param name="n">calculation period</param>
        /// <returns>MFI time series</returns>
        public static TimeSeriesFloat MoneyFlowIndex(this TimeSeriesAsset series, int n)
        {
            var name = string.Format("{0}.MoneyFlowIndex({1})", series.Name, n);

            var typicalPrice = series.TypicalPrice();

            var postiveMoneyFlow = series.Owner.Lambda(
                name + ".+MF",
                prev => typicalPrice[0] > typicalPrice[1] ? typicalPrice[0] * series.Volume[0] : 0.0,
                0.0);

            var negativeMoneyFlow = series.Owner.Lambda(
                name + ".-MF",
                prev => typicalPrice[0] < typicalPrice[1] ? typicalPrice[0] * series.Volume[0] : 0.0,
                0.0);

            return postiveMoneyFlow.SMA(n)
                .Div(postiveMoneyFlow.SMA(n).Add(negativeMoneyFlow.SMA(n)).Max(1e-99))
                .Mul(100.0);
        }
        #endregion

        // - Volume Rate of Change
    }
}

//==============================================================================
// end of file
