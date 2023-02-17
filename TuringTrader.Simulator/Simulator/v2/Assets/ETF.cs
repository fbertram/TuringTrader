//==============================================================================
// Project:     TuringTrader, simulator core v2
// Name:        Assets/ETF
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
        /// (since Jan 1993, backfilled to Jan 1990)
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
        /// (since Nov 2005, backfilled to Dec 1994)
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
        /// Invesco QQQ Trust Series 1 ETF
        /// (since Mar 1999)
        /// </summary>
        public const string QQQ = "splice:QQQ,csv:backfills/QQQ.csv";
        /// <summary>
        /// iShares Russell 2000 ETF
        /// (since May 2000, backfilled to Jan 1990)
        /// </summary>
        public const string IWM = "splice:IWM,csv:backfills/IWM.csv";
        /// <summary>
        /// // Vanguard Russell 1000 ETF
        /// (since Sep 2010, backfilled to Jan 1990)
        /// </summary>
        public const string VONE = "splice:VONE,csv:backfills/VONE.csv";
        /// <summary>
        /// Vanguard Small-Cap 600 ETF
        /// (since Sep 2010, backfilled to Dec 1994)
        /// </summary>
        public const string VIOO = "splice:VIOO,csv:backfills/VIOO.csv";
        /// <summary>
        /// Vanguard Value Index ETF
        /// (since Jan 2004)
        /// </summary>
        public const string VTV = "splice:VTV,csv:backfills/VTV.csv";
        /// <summary>
        /// Vanguard Growth Index ETF
        /// (since Jan 2004)
        /// </summary>
        public const string VUG = "splice:VUG,csv:backfills/VUG.csv";
        /// <summary>
        /// Vanguard S&amp;P Small-Cap 600 Index ETF
        /// (since September 2010, backfilled to Jul 2000)
        /// </summary>
        public const string VIOV = "splice:VIOV,csv:backfills/VIOV.csv";
        /// <summary>
        /// Vanguard S&amp;P Small-Cap 600 Growth Index ETF
        /// (since Sep 2010, backfilled to Jul 2000)
        /// </summary>
        public const string VIOG = "splice:VIOG,csv:backfills/VIOG.csv";
        /// <summary>
        /// Vanguard Total World Stock Index ETF
        /// (since Jun 2008)
        /// </summary>
        public const string VT = "splice:VT,csv:backfills/VT.csv";
        /// <summary>
        /// Dow Jones Industrial Average Trust ETF
        /// (since Jan 1998, backfilled to Jan 1990)
        /// </summary>
        public const string DIA = "splice:DIA,csv:backfills/DIA.csv";
        #endregion
        #region S&P sectors
        /// <summary>
        /// Materials Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Sep 1989)
        /// </summary>
        public const string XLB = "splice:XLB,csv:backfills/XLB.csv";
        /// <summary>
        /// Communication Services Select Sector SPDR ETF
        /// (since Jun 2018, backfilled to Sep 1989)
        /// </summary>
        public const string XLC = "splice:XLC,csv:backfills/XLC.csv";
        /// <summary>
        /// Engergy Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Sep 1989)
        /// </summary>
        public const string XLE = "splice:XLE,csv:backfills/XLE.csv";
        /// <summary>
        /// Financial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Sep 1989)
        /// </summary>
        public const string XLF = "splice:XLF,csv:backfills/XLF.csv";
        /// <summary>
        /// Industrial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Sep 1989)
        /// </summary>
        public const string XLI = "splice:XLI,csv:backfills/XLI.csv";
        /// <summary>
        /// Technology Select Sector SPDR ETF
        /// </summary>
        /// (since Dec 1998, backfilled to Sep 1989)
        public const string XLK = "splice:XLK,csv:backfills/XLK.csv";
        /// <summary>
        /// Consumer Staples Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Sep 1989)
        /// </summary>
        public const string XLP = "splice:XLP,csv:backfills/XLP.csv";
        /// <summary>
        /// Real Estate Select Sector SPDR ETF
        /// (since Oct 2015, backfilled to Oct 2001)
        /// </summary>
        public const string XLRE = "splice:XLRE,csv:backfills/XLRE.csv";
        /// <summary>
        /// Utilities Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Sep 1989)
        /// </summary>
        public const string XLU = "splice:XLU,csv:backfills/XLU.csv";
        /// <summary>
        /// Health Care Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Sep 1989)
        /// </summary>
        public const string XLV = "splice:XLV,csv:backfills/XLV.csv";
        /// <summary>
        /// Consumer Discretionary Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan Sep 1989)
        /// </summary>
        public const string XLY = "splice:XLY,csv:backfills/XLY.csv";
        #endregion
        #region non-US and others
        /// <summary>
        /// Vanguard European Stock Index ETF
        /// (since Mar 2005)
        /// </summary>
        public const string VGK = "splice:VGK,csv:backfills/VGK.csv";
        /// <summary>
        /// iShares MSCI Japan ETF
        /// (since Mar 1996)
        /// </summary>
        public const string EWJ = "splice:EWJ,csv:backfills/EWJ.csv";
        /// <summary>
        /// Vanguard FTSE Developed Markets ETF
        /// (since August 2007, backfilled to Aug 2001)
        /// </summary>
        public const string VEA = "splice:VEA,csv:backfills/VEA.csv";
        /// <summary>
        /// Vanguard FTSE Emerging Markets ETF
        /// (since Mar 2005)
        /// </summary>
        public const string VWO = "splice:VWO,csv:backfills/VWO.csv";
        /// <summary>
        /// iShares MSCI ACWI ex US ETF
        /// (since Mar 2008, backfilled to Jan 1980)
        /// </summary>
        public const string ACWX = "splice:ACWX,csv:backfills/ACWX.csv";
        /// <summary>
        /// Vanguard FTSE All World ex US ETF
        /// (since Mar 2007, backfilled to Jan 1980)
        /// </summary>
        public const string VEU = "splice:VEU,csv:backfills/VEU.csv";
        /// <summary>
        /// Vanguard FTSE All-World ex-US Small-Cap Index ETF
        /// (since Apr 2009, backfilled to Nov 1996)
        /// </summary>
        public const string VSS = "splice:VSS,csv:backfills/VSS.csv";
        #endregion
        #endregion
        #region bonds
        #region broad markets
        /// <summary>
        /// iShares Core US Aggregate Bond ETF
        /// (since Oct 2003, backfilled to Jan 1970)
        /// </summary>
        public const string AGG = "splice:AGG,csv:backfills/AGG.csv";
        /// <summary>
        /// Vanguard Total Bond Market Index ETF
        /// (since Apr 2007)
        /// </summary>
        public const string BND = "splice:BND,csv:backfills/BND.csv";
        #endregion
        #region short-term (0-3yr) treasuries
        /// <summary>
        /// SPDR Bloomberg 1-3 Month T-Bill ETF
        /// (since Jun 2007, backfilled to Jan 1970)
        /// </summary>
        public const string BIL = "splice:BIL,csv:backfills/BIL.csv";
        /// <summary>
        /// iShares 1-3 Year Treasury Bond ETF
        /// (since Aug 2002, backfilled to Jan 1970)
        /// </summary>
        public const string SHY = "splice:SHY,csv:backfills/SHY.csv";
        /// <summary>
        /// iShares Short Treasury Bond ETF
        /// (since Jan 2007, backfilled to Jul 2002)
        /// </summary>
        public const string SHV = "splice:SHV,csv:backfills/SHV.csv";
        #endregion
        #region intermediate-term (3-10yr) treasuries
        /// <summary>
        /// iShares 3-7 Year Treasury Bond ETF
        /// (since Jan 2007, backfilled to Jan 1970)
        /// </summary>
        public const string IEI = "splice:IEI,csv:backfills/IEI.csv";
        /// <summary>
        /// iShares 7-10 Year Treasury Bond ETF
        /// (since Aug 2002, backfilled to Jan 1970)
        /// </summary>
        public const string IEF = "splice:IEF,csv:backfills/IEF.csv";
        /// <summary>
        /// Vanguard Intermediate-Term Treasury Index ETF
        /// (since December 2009, backfilled to Jul 2002)
        /// </summary>
        public const string VGIT = "splice:VGIT,csv:backfills/VGIT.csv";
        #endregion
        #region long-term (10+yr) treasuries
        /// <summary>
        /// iShares 10-20 Year Treasury Bond ETF
        /// (since Jan 2007, backfilled to Jan 1970)
        /// </summary>
        public const string TLH = "splice:TLH,csv:backfills/TLH.csv";
        /// <summary>
        /// iShares 20 Plus Year Treasury Bond ETF
        /// (since Aug 2002, backfilled to Jan 1970)
        /// </summary>
        public const string TLT = "splice:TLT,csv:backfills/TLT.csv";
        /// <summary>
        /// Vanguard Long-Term Treasury Index ETF
        /// (since Dec 2009, backfilled to Jan 1970)
        /// </summary>
        public const string VGLT = "splice:VGLT,csv:backfills/VGLT.csv";
        /// <summary>
        /// Vanguard Extended Duration ETF
        /// (since Dec 2007, backfilled to Jan 1970)
        /// </summary>
        public const string EDV = "splice:EDV,csv:backfills/EDV.csv";
        #endregion
        #region other government bonds
        /// <summary>
        /// iShares TIPS Bond ETF
        /// (since Dec 2003, backfilled to Jun 2000)
        /// </summary>
        public const string TIP = "splice:TIP,csv:backfills/TIP.csv";
        #endregion
        #region corporate bonds
        /// <summary>
        /// iShares iBoxx $ Investment Grade Corporate Bond ETF
        /// (since Aug 2002, backfilled to Jan 1970)
        /// </summary>
        public const string LQD = "splice:LQD,csv:backfills/LQD.csv";
        /// <summary>
        /// iShares iBoxx $ High Yield Corporate Bond ETF
        /// (since Apr 2007, backfilled to Jan 1980)
        /// </summary>
        public const string HYG = "splice:HYG,csv:backfills/HYG.csv";
        /// <summary>
        /// Vanguard Long-Term Corporate Bond Index ETF
        /// (since Dec 2009, backfilled to Jan 1970)
        /// </summary>
        public const string VCLT = "splice:VCLT,csv:backfills/VCLT.csv";
        /// <summary>
        /// SPDR Bloomberg High Yield Bond ETF
        /// (since Dec 2007, backfilled to Jan 1980)
        /// </summary>
        public const string JNK = "splice:JNK,csv:backfills/JNK.csv";
        /// <summary>
        /// SPDR Boomberg Barclay's Convertible Securities ETF
        /// (since Apr 2009, backfilled to Jun 1986)
        /// </summary>
        public const string CWB = "splice:CWB,csv:backfills/CWB.csv";
        /// <summary>
        /// iShares 5-10 Yr Investment Grade Corporate Bond ETF
        /// (since Jan 2007, backfilled to Jan 1970)
        /// </summary>
        public const string IGIB = "splice:IGIB,csv:backfills/IGIB.csv";
        #endregion
        #region non-US
        /// <summary>
        /// SPDR Bloomberg International Treasury Bond ETF
        /// (since Oct 2007)
        /// </summary>
        public const string BWX = "splice:BWX,csv:backfills/BWX.csv";
        /// <summary>
        /// Vanguard Total International Bond Index ETF
        /// (since Jun 2013, backfilled to Jan 1993)
        /// </summary>
        public const string BNDX = "splice:BNDX,csv:backfills/BNDX.csv";
        #endregion
        #endregion
        #region commodities & hard assets
        /// <summary>
        /// SPDR Gold Shares ETF. 
        /// (since Nov 2004, backfilled to Jul 1982)
        /// </summary>
        public const string GLD = "splice:GLD,csv:backfills/GLD.csv";
        /// <summary>
        /// Invesco DB Commodity Index Tracking ETF
        /// (since Feb 2006, backfilled to Jul 2002).
        /// </summary>
        public const string DBC = "splice:DBC,csv:backfills/DBC.csv";
        /// <summary>
        /// VNQ	Vanguard Real Estate Index ETF
        /// (since Sep 2004)
        /// </summary>
        public const string VNQ = "splice:VNQ,csv:backfills/VNQ.csv";
        /// <summary>
        /// Invesco Optimum Yield Diversified Commodity Strategy ETF
        /// (since Nov 2014, backfilled to Jul 2002)
        /// </summary>
        public const string PDBC = "splice:PDBC,csv:backfills/PDBC.csv";
        /// <summary>
        /// iShares Gold Trust
        /// (since Jan 2005)
        /// </summary>
        public const string IAU = "splice:IAU,csv:backfills/IAU.csv";
        #endregion
        #region leveraged
        /// <summary>
        /// Direxion Daily S&P 500 Bull 2x Shares ETF
        /// (since May 2014, backfilled to Jun 1976)
        /// </summary>
        public const string SPUU = "splice:SPUU,csv:backfills/SPUU.csv";
        /// <summary>
        /// ProShares Ultra S&P 500 ETF
        /// (since Jun 2006, backfilled to Jun 1976)
        /// </summary>
        public const string SSO = "splice:SSO,csv:backfills/SSO.csv";
        /// <summary>
        /// Direxion Daily S&P 500 Bull 3x Shares ETF
        /// (since Nov 2008, backfilled to Jun 1976)
        /// </summary>
        public const string SPXL = "splice:SPXL,csv:backfills/SPXL.csv";
        /// <summary>
        /// ProShares UltraPro S&P500 ETF
        /// (since Jun 2009, backfilled to Jun 1976)
        /// </summary>
        public const string UPRO = "splice:UPRO,csv:backfills/UPRO.csv";
        /// <summary>
        /// ProShares Short S&P 500 ETF
        /// (since Jun 2006, backfilled to Jun 1976)
        /// </summary>
        public const string SH = "splice:SH,csv:backfills/SH.csv";
        /// <summary>
        /// ProShares UltraShort S&P500 ETF
        /// (since Jul 2006, backfilled to Jun 1976)
        /// </summary>
        public const string SDS = "splice:SDS,csv:backfills/SDS.csv";
        /// <summary>
        /// Direxion Daily S&P 500 Bear 3x Shares ETF
        /// (since Nov 2009, backfilled to Jun 1976)
        /// </summary>
        public const string SPXS = "splice:SPXS,csv:backfills/SPXS.csv";
        /// <summary>
        /// ProShares UltraPro S&P500 Bear 3x ETF
        /// (since Jun 2009, backfilled to Jun 1976)
        /// </summary>
        public const string SPXU = "splice:SPXU,csv:backfills/SPXU.csv";
        /// <summary>
        /// UMDD - ProShares UltraPro MidCap400 ETF
        /// (since Feb 2010, backfilled to Oct 1994)
        /// </summary>
        public const string UMDD = "splice:UMDD,csv:backfills/UMDD.csv";
        /// <summary>
        /// ProShares Ultra SmallCAp600 ETF
        /// (since Nov 2008, backfilled to Oct 1994)
        /// </summary>
        public const string SAA = "splice:SAA,csv:backfills/SAA.csv";
        /// <summary>
        /// ProShares UltraQQQ
        /// (since Jun 2006, backfilled to Mar 1999)
        /// </summary>
        public const string QLD = "splice:QLD,csv:backfills/QLD.csv";
        /// <summary>
        /// ProShares UltraPro QQQ
        /// (since Feb 2010, backfilled to Mar 1999)
        /// </summary>
        public const string TQQQ = "splice:TQQQ,csv:backfills/TQQQ.csv";
        /// <summary>
        /// ProShares UltraPro Dow30 ETF
        /// (since Feb 2010, backfilled to Jun 1976)
        /// </summary>
        public const string UDOW = "splice:UDOW,csv:backfills/UDOW.csv";
        /// <summary>
        /// ProShares UltraPro Russell2000 ETF
        /// (since Feb 2010, backfilled to Dec 1978)
        /// </summary>
        public const string URTY = "splice:URTY,csv:backfills/URTY.csv";
        /// <summary>
        /// ProShares Ultra 20+ Year Treasury ETF
        /// (since Jan 2010, backfilled to Jan 1970)
        /// </summary>
        public const string UBT = "splice:UBT,csv:backfills/UBT.csv";
        /// <summary>
        /// Direxion Daily 20+ Year Treasury Bull 3X Shares ETF
        /// (since Apr 2009, backfilled to Jan 1970)
        /// </summary>
        public const string TMF = "splice:TMF,csv:backfills/TMF.csv";
        /// <summary>
        /// ProShares Ultra 7-10 Year Treasury ETF
        /// (since Jan 2010, backfilled to Jan 1970)
        /// </summary>
        public const string UST = "splice:UST,csv:backfills/UST.csv";
        /// <summary>
        /// Direxion Daily 7-10 Year Treasury Bull 3X Shares ETF
        /// (since Apr 2009, backfilled to Jan 1970)
        /// </summary>
        public const string TYD = "splice:TYD,csv:backfills/TYD.csv";
        /// <summary>
        /// ProShares Ultra Gold
        /// (since Feb 2010, backfilled to Jul 1982)
        /// </summary>
        public const string UGL = "splice:UGL,csv:backfills/UGL.csv";
        /// <summary>
        /// Credit Suisse VelocityShares 3x Long Gold SP GSCI Gold Index ER ETN
        /// (since Oct 2011, backfilled to Jul 1982)
        /// </summary>
        public const string UGLDF = "splice:UGLDF,csv:backfills/UGLDF.csv";
        #endregion
        #region volatility
        /// <summary>
        /// Barclays iPath Series B S&P 500 VIX Short-Term Futures ETN
        /// (since Jan 2018, backfilled to Jan 2008)
        /// </summary>
        public const string VXX = "splice:VXX,csv:backfills/VXX.csv";
        #endregion
    }
}

//==============================================================================
// end of file
