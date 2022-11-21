//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Assets/ETFs
// Description: Definitions for common ETFs.
// History:     2022x29, FUB, created
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

namespace TuringTrader.SimulatorV2.Assets
{
    /// <summary>
    /// Collection of ETFs.
    /// </summary>
    public class ETF
    {
        #region stock market
        /// <summary>
        /// SPDR S&P 500 Trust ETF. Available since February 1993.
        /// </summary>
        public const string SPY = "splice:SPY,SPY++";
        /// <summary>
        /// Invesco QQQ Trust Series 1 ETF. Available since March 1999.
        /// </summary>
        public const string QQQ = "QQQ";
        /// <summary>
        /// iShares Russell 2000 ETF. Available since June 2000.
        /// </summary>
        public const string IWM = "IWM";
        /// <summary>
        /// Vanguard European Stock Index ETF. Available since March 2005.
        /// </summary>
        public const string VGK = "VGK";
        /// <summary>
        /// iShares MSCI Japan ETF. Available since March 1996.
        /// </summary>
        public const string EWJ = "EWJ";
        /// <summary>
        /// // Vanguard Russell 1000 ETF. Available since October 2010.
        /// </summary>
        public const string VONE = "splice:VONE,IWB";
        /// <summary>
        /// Vanguard Small-Cap 600 ETF. Available since September 2010.
        /// </summary>
        public const string VIOO = "splice:VIOO,IJR";
        /// <summary>
        /// Vanguard FTSE Developed Markets ETF. Availale since August 2007.
        /// </summary>
        public const string VEA = "splice:VEA,EFA";
        /// <summary>
        /// Vanguard FTSE Emerging Markets ETF. Available since March 2005.
        /// </summary>
        public const string VWO = "VWO";
        /// <summary>
        /// Vanguard Value Index ETF. Available since February 2004.
        /// </summary>
        public const string VTV = "VTV";
        /// <summary>
        /// Vanguard Growth Index ETF. Available since February 2004.
        /// </summary>
        public const string VUG = "VUG";
        /// <summary>
        /// Vanguard S&P Small-Cap 600 Index ETF. Available since September 2010.
        /// </summary>
        public const string VIOV = "splice:VIOV,IJS";
        /// <summary>
        /// Vanguard S&P Small-Cap 600 Growth Index ETF. Available September 2010.
        /// </summary>
        public const string VIOG = "splice:VIOG,IJT";
        /// <summary>
        /// Vanguard Total World Stock Index ETF. Available since July 2008.
        /// </summary>
        public const string VT = "VT";
        #endregion
        #region bonds
        #region broad markets
        /// <summary>
        /// iShares Core US Aggregate Bond ETF. Available since October 2003.
        /// </summary>
        public const string AGG = "splice:AGG,AGG++";
        /// <summary>
        /// Vanguard Total Bond Market Index ETF. Available since April 2007.
        /// </summary>
        public const string BND = "BND";
        /// <summary>
        /// Vanguard Total International Bond Index ETF. Available since June 2013.
        /// </summary>
        public const string BNDX = "splice:BNDX,BWX,SHY";
        #endregion
        #region short-term (0-3yr) treasuries
        /// <summary>
        /// SPDR Bloomberg 1-3 Month T-Bill ETF. Available since June 2007.
        /// </summary>
        public const string BIL = "splice:BIL,BIL++";
        /// <summary>
        /// iShares 1-3 Year Treasury Bond ETF. Available since August 2002.
        /// </summary>
        public const string SHY = "splice:SHY,SHY++";
        #endregion
        #region intermediate-term (3-10yr) treasuries
        /// <summary>
        /// iShares 3-7 Year Treasury Bond ETF. Available since January 2007.
        /// </summary>
        public const string IEI = "IEI";
        /// <summary>
        /// iShares 7-10 Year Treasury Bond ETF. Available since August 2002.
        /// </summary>
        public const string IEF = "splice:IEF,IEF++";
        /// <summary>
        /// Vanguard Intermediate-Term Treasury Index ETF. Available since December 2009.
        /// </summary>
        public const string VGIT = "splice:VGIT,IEF";
        #endregion
        #region long-term (10+yr) treasuries
        /// <summary>
        /// iShares 10-20 Year Treasury Bond ETF. Available since January 2007.
        /// </summary>
        public const string TLH = "TLH";
        /// <summary>
        /// iShares 20 Plus Year Treasury Bond ETF. Available since August 2002.
        /// </summary>
        public const string TLT = "splice:TLT,TLT++";
        /// <summary>
        /// Vanguard Long-Term Treasury Index ETF. Available since December 2009.
        /// </summary>
        public const string VGLT = "splice:VGLT,TLT";
        /// <summary>
        /// Vanguard Extended Duration ETF. Available since December 2007.
        /// </summary>
        public const string EDV = "splice:EDV,TLT";
        #endregion
        #region other government bonds
        /// <summary>
        /// iShares TIPS Bond ETF. Available since December 2003.
        /// </summary>
        public const string TIP = "TIP";
        #endregion
        #region corporate bonds
        /// <summary>
        /// iShares iBoxx $ Investment Grade Corporate Bond ETF. Available since August 2002.
        /// </summary>
        public const string LQD = "LQD";
        /// <summary>
        /// iShares iBoxx $ High Yield Corporate Bond ETF. Available since April 2007.
        /// </summary>
        public const string HYG = "HYG";
        /// <summary>
        /// Vanguard Long-Term Corporate Bond Index ETF. Available since December 2009.
        /// </summary>
        public const string VCLT = "splice:VCLT,LQD";
        /// <summary>
        /// SPDR Bloomberg High Yield Bond ETF. Available since December 2007.
        /// </summary>
        public const string JNK = "JNK";
        #endregion
        #endregion
        #region commodities & hard assets
        /// <summary>
        /// SPDR Gold Shares ETF. Available since November 2004.
        /// </summary>
        public const string GLD = "splice:GLD,GLD++";
        /// <summary>
        /// Invesco DB Commodity Index Tracking ETF. Available since February 2006.
        /// </summary>
        public const string DBC = "splice:DBC,DBC++";
        /// <summary>
        /// VNQ	Vanguard Real Estate Index ETF. Available since September 2004.
        /// </summary>
        public const string VNQ = "VNQ";
        /// <summary>
        /// Invesco Optimum Yield Diversified Commodity Strategy ETF. Available since Novemer 2014.
        /// </summary>
        public const string PDBC = "splice:PDBC,DBC";
        /// <summary>
        /// iShares Gold Trust. Available since January 2005.
        /// </summary>
        public const string IAU = "IAU";
        #endregion
    }
}

//==============================================================================
// end of file
