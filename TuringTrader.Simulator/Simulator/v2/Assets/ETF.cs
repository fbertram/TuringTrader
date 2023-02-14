//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Assets/ETFs
// Description: Definitions for common ETFs.
// History:     2022x29, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2023, Bertram Enterprises LLC dba TuringTrader.
//              https://www.turingtrader.org
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ trading simulator.
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
        #region stocks
        #region U.S. stock markets
        /// <summary>
        /// SPDR S&amp;P 500 Trust ETF
        /// (since Jan 1993, backfilled to Jan 1950)
        /// </summary>
        public const string SPY = "splice:SPY,csv:backfills/SPY.csv";
        /// <summary>
        /// SPDR Portfolio S&amp;P 500 Growth ETF (since Sep 2000)
        /// </summary>
        public const string SPYG = "splice:SPYG,csv:backfills/SPYG.csv";
        /// <summary>
        /// SPDR Portfolio S&amp;P 500 Value ETF (since Sep 2000)
        /// </summary>
        public const string SPYV = "splice:SPYV,csv:backfills/SPYV.csv";
        /// <summary>
        /// SPDR S&amp;P MidCap 400 ETF
        /// (since May 1995, backfilled to July 1991)
        /// </summary>
        public const string MDY = "splice:MDY,csv:backfills/MDY.csv";
        /// <summary>
        /// SPDR S&amp;P 400 Mid Cap Growth ETF (since Nov 2005)
        /// </summary>
        public const string MDYG = "splice:MDYG,csv:backfills/MDYG.csv";
        /// <summary>
        /// SPDR S&amp;P 400 Mid Cap Value ETF (since Nov 2005)
        /// </summary>
        public const string MDYV = "splice:MDYV,csv:backfills/MDYV.csv";
        /// <summary>
        /// SPDR S&amp;P 600 SmallCap ETF
        /// (since Nov 2005, backfilled to Jan 1995)
        /// </summary>
        public const string SLY = "splice:SLY,csv:backfills/SLY.csv";
        /// <summary>
        /// SPDR S&amp;P 600 Small Cap Growth ETF (since Sep 2000)
        /// </summary>
        public const string SLYG = "splice:SLYG,csv:backfills/SLYG.csv";
        /// <summary>
        /// SPDR S&amp;P 600 Small Cap Value ETF (since Sep 2000)
        /// </summary>
        public const string SLYV = "splice:SLYV,csv:backfills/SLYV.csv";
        /// <summary>
        /// Invesco QQQ Trust Series 1 ETF. Available since March 1999.
        /// </summary>
        public const string QQQ = "splice:QQQ,csv:backfills/QQQ.csv";
        /// <summary>
        /// iShares Russell 2000 ETF. Available since June 2000.
        /// </summary>
        public const string IWM = "splice:IWM,csv:backfills/IWM.csv";
        /// <summary>
        /// Vanguard European Stock Index ETF. Available since March 2005.
        /// </summary>
        public const string VGK = "splice:VGK,csv:backfills/VGK.csv";
        /// <summary>
        /// iShares MSCI Japan ETF. Available since March 1996.
        /// </summary>
        public const string EWJ = "splice:EWJ,csv:backfills/EWJ.csv";
        /// <summary>
        /// // Vanguard Russell 1000 ETF. Available since October 2010.
        /// </summary>
        public const string VONE = "splice:VONE,csv:backfills/VONE.csv";
        /// <summary>
        /// Vanguard Small-Cap 600 ETF. Available since September 2010.
        /// </summary>
        public const string VIOO = "splice:VIOO,csv:backfills/VIOO.csv";
        /// <summary>
        /// Vanguard FTSE Developed Markets ETF. Availale since August 2007.
        /// </summary>
        public const string VEA = "splice:VEA,csv:backfills/VEA.csv";
        /// <summary>
        /// Vanguard FTSE Emerging Markets ETF. Available since March 2005.
        /// </summary>
        public const string VWO = "splice:VWO,csv:backfills/VWO.csv";
        /// <summary>
        /// Vanguard Value Index ETF. Available since February 2004.
        /// </summary>
        public const string VTV = "splice:VTV,csv:backfills/VTV.csv";
        /// <summary>
        /// Vanguard Growth Index ETF. Available since February 2004.
        /// </summary>
        public const string VUG = "splice:VUG,csv:backfills/VUG.csv";
        /// <summary>
        /// Vanguard S&amp;P Small-Cap 600 Index ETF. Available since September 2010.
        /// </summary>
        public const string VIOV = "splice:VIOV,csv:backfills/VIOV.csv";
        /// <summary>
        /// Vanguard S&amp;P Small-Cap 600 Growth Index ETF. Available September 2010.
        /// </summary>
        public const string VIOG = "splice:VIOG,csv:backfills/VIOG.csv";
        /// <summary>
        /// Vanguard Total World Stock Index ETF. Available since July 2008.
        /// </summary>
        public const string VT = "splice:VT,csv:backfills/VT.csv";
        #endregion
        #region S&P sectors
        /// <summary>
        /// Materials Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLB = "splice:XLB,csv:backfills/XLB.csv";
        /// <summary>
        /// Communication Services Select Sector SPDR ETF
        /// (since Jun 2018, backfilled to Jan 1990)
        /// </summary>
        public const string XLC = "splice:XLC,csv:backfills/XLC.csv";
        /// <summary>
        /// Engergy Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLE = "splice:XLE,csv:backfills/XLE.csv";
        /// <summary>
        /// Financial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLF = "splice:XLF,csv:backfills/XLF.csv";
        /// <summary>
        /// Industrial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLI = "splice:XLI,csv:backfills/XLI.csv";
        /// <summary>
        /// Technology Select Sector SPDR ETF
        /// </summary>
        /// (since Dec 1998, backfilled to Jan 1990)
        public const string XLK = "splice:XLK,csv:backfills/XLK.csv";
        /// <summary>
        /// Consumer Staples Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLP = "splice:XLP,csv:backfills/XLP.csv";
        /// <summary>
        /// Real Estate Select Sector SPDR ETF
        /// (since Oct 2015, backfilled to Jan 1990)
        /// </summary>
        public const string XLRE = "splice:XLRE,csv:backfills/XLRE.csv";
        /// <summary>
        /// Utilities Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLU = "splice:XLU,csv:backfills/XLU.csv";
        /// <summary>
        /// Health Care Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLV = "splice:XLV,csv:backfills/XLV.csv";
        /// <summary>
        /// Consumer Discretionary Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLY = "splice:XLY,csv:backfills/XLY.csv";
        #endregion
        #region non-US and others
        #endregion
        #endregion
        #region bonds
        #region broad markets
        /// <summary>
        /// iShares Core US Aggregate Bond ETF. Available since October 2003.
        /// </summary>
        public const string AGG = "splice:AGG,csv:backfills/AGG.csv";
        /// <summary>
        /// Vanguard Total Bond Market Index ETF. Available since April 2007.
        /// </summary>
        public const string BND = "splice:BND,csv:backfills/BND.csv";
        /// <summary>
        /// Vanguard Total International Bond Index ETF. Available since June 2013.
        /// </summary>
        public const string BNDX = "splice:BNDX,csv:backfills/BNDX.csv";
        #endregion
        #region short-term (0-3yr) treasuries
        /// <summary>
        /// SPDR Bloomberg 1-3 Month T-Bill ETF. Available since June 2007.
        /// </summary>
        public const string BIL = "splice:BIL,csv:backfills/BIL.csv";
        /// <summary>
        /// iShares 1-3 Year Treasury Bond ETF. Available since August 2002.
        /// </summary>
        public const string SHY = "splice:SHY,csv:backfills/SHY.csv";
        /// <summary>
        /// iShares Short Treasury Bond ETF. Available since January 2007.
        /// </summary>
        public const string SHV = "splice:SHV,csv:backfills/SHV.csv";
        #endregion
        #region intermediate-term (3-10yr) treasuries
        /// <summary>
        /// iShares 3-7 Year Treasury Bond ETF. Available since January 2007.
        /// </summary>
        public const string IEI = "splice:IEI,csv:backfills/IEI.csv";
        /// <summary>
        /// iShares 7-10 Year Treasury Bond ETF. Available since August 2002.
        /// </summary>
        public const string IEF = "splice:IEF,csv:backfills/IEF.csv";
        /// <summary>
        /// Vanguard Intermediate-Term Treasury Index ETF. Available since December 2009.
        /// </summary>
        public const string VGIT = "splice:VGIT,csv:backfills/VGIT.csv";
        #endregion
        #region long-term (10+yr) treasuries
        /// <summary>
        /// iShares 10-20 Year Treasury Bond ETF. Available since January 2007.
        /// </summary>
        public const string TLH = "splice:TLH,csv:backfills/TLH.csv";
        /// <summary>
        /// iShares 20 Plus Year Treasury Bond ETF. Available since August 2002.
        /// </summary>
        public const string TLT = "splice:TLT,csv:backfills/TLT.csv";
        /// <summary>
        /// Vanguard Long-Term Treasury Index ETF. Available since December 2009.
        /// </summary>
        public const string VGLT = "splice:VGLT,csv:backfills/VGLT.csv";
        /// <summary>
        /// Vanguard Extended Duration ETF. Available since December 2007.
        /// </summary>
        public const string EDV = "splice:EDV,csv:backfills/EDV.csv";
        #endregion
        #region other government bonds
        /// <summary>
        /// iShares TIPS Bond ETF. Available since December 2003.
        /// </summary>
        public const string TIP = "splice:TIP,csv:backfills/TIP.csv";
        #endregion
        #region corporate bonds
        /// <summary>
        /// iShares iBoxx $ Investment Grade Corporate Bond ETF. Available since August 2002.
        /// </summary>
        public const string LQD = "splice:LQD,csv:backfills/LQD.csv";
        /// <summary>
        /// iShares iBoxx $ High Yield Corporate Bond ETF. Available since April 2007.
        /// </summary>
        public const string HYG = "splice:HYG,csv:backfills/HYG.csv";
        /// <summary>
        /// Vanguard Long-Term Corporate Bond Index ETF. Available since December 2009.
        /// </summary>
        public const string VCLT = "splice:VCLT,csv:backfills/VCLT.csv";
        /// <summary>
        /// SPDR Bloomberg High Yield Bond ETF. Available since December 2007.
        /// </summary>
        public const string JNK = "splice:JNK,csv:backfills/JNK.csv";
        #endregion
        #endregion
        #region commodities & hard assets
        /// <summary>
        /// SPDR Gold Shares ETF. Available since November 2004.
        /// </summary>
        public const string GLD = "splice:GLD,csv:backfills/GLD.csv";
        /// <summary>
        /// Invesco DB Commodity Index Tracking ETF
        /// (since February 2006, backfilled to July 2002).
        /// </summary>
        public const string DBC = "splice:DBC,csv:backfills/DBC.csv";
        /// <summary>
        /// VNQ	Vanguard Real Estate Index ETF. Available since September 2004.
        /// </summary>
        public const string VNQ = "splice:VNQ,csv:backfills/VNQ.csv";
        /// <summary>
        /// Invesco Optimum Yield Diversified Commodity Strategy ETF. Available since Novemer 2014.
        /// </summary>
        public const string PDBC = "splice:PDBC,csv:backfills/PDBC.csv";
        /// <summary>
        /// iShares Gold Trust. Available since January 2005.
        /// </summary>
        public const string IAU = "splice:IAU,csv:backfills/IAU.csv";
        #endregion
    }
}

//==============================================================================
// end of file
