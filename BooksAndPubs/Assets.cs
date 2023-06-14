﻿//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Assets
// Description: Definition of assets and backfills.
// History:     2022ii01, FUB, created
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

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
using TuringTrader.Support;
using TuringTrader.Indicators;
using TuringTrader.BooksAndPubs;
using System.Globalization;
#endregion

namespace TuringTrader.Algorithms.Glue
{
    public partial class Assets
    {
        #region stocks
        #region U.S. Stock Markets
        /// <summary>
        /// Vanguard Total Stock Market Index ETF
        /// (since May 2001)
        /// </summary>
        public static readonly string VTI = "VTI";
        /// <summary>
        /// SPDR S&amp;P 500 Trust ETF
        /// (since Jan 1993, backfilled to Jan 1950)
        /// </summary>
        public static readonly string SPY = "splice:SPY,csv:backfills/SPY.csv";
        /// <summary>
        /// SPDR Portfolio S&P 500 Growth ETF (since Sep 2000)
        /// </summary>
        public static readonly string SPYG = "SPYG";
        /// <summary>
        /// SPDR Portfolio S&P 500 Value ETF (since Sep 2000)
        /// </summary>
        public static readonly string SPYV = "SPYV";
        /// <summary>
        /// Invesco S&P 500 Equal Weight ETF
        /// (since Apr 2003)
        /// </summary>
        public static readonly string RSP = "RSP";
        /// <summary>
        /// Invesco QQQ Trust Series 1 ETF
        /// (since Mar 1999, backfilled to Mar 1999)
        /// </summary>
        public static readonly string QQQ = "splice:QQQ,csv:backfills/QQQ.csv";
        /// <summary>
        /// Dow Jones Industrial Average Trust ETF
        /// (since Jan 1998, backfilled to Oct 1987)
        /// </summary>
        public static readonly string DIA = "splice:DIA,csv:backfills/DIA.csv";
        /// <summary>
        /// iShares Russell 1000 Value ETF
        /// </summary>
        public static readonly string IWD = "IWD";
        /// <summary>
        /// iShares Russell 2000 ETF
        /// (since May 2000, backfilled to Dec 1978)
        /// </summary>
        public static readonly string IWM = "splice:IWM,csv:backfills/IWM.csv";
        /// <summary>
        /// SPDR S&P MidCap 400 ETF
        /// (since May 1995, backfilled to July 1991)
        /// </summary>
        public static readonly string MDY = "splice:MDY,csv:backfills/MDY.csv";
        /// <summary>
        /// SPDR S&P 400 Mid Cap Growth ETF (since Nov 2005)
        /// </summary>
        public static readonly string MDYG = "MDYG";
        /// <summary>
        /// SPDR S&P 400 Mid Cap Value ETF (since Nov 2005)
        /// </summary>
        public static readonly string MDYV = "MDYV";
        /// <summary>
        /// SPDR S&P 600 SmallCap ETF
        /// (since Nov 2005, backfilled to Jan 1995)
        /// </summary>
        public static readonly string SLY = "splice:SLY,csv:backfills/SLY.csv";
        /// <summary>
        /// SPDR S&P 600 Small Cap Growth ETF (since Sep 2000)
        /// </summary>
        public static readonly string SLYG = "SLYG";
        /// <summary>
        /// SPDR S&P 600 Small Cap Value ETF (since Sep 2000)
        /// </summary>
        public static readonly string SLYV = "SLYV";
        /// <summary>
        /// Vanguard S&P Small-Cap 600 Index ETF
        /// (since Sep 2010, backfilled to Jan 1995)
        /// </summary>
        public static readonly string VIOO = "splice:VIOO,csv:backfills/VIOO.csv";
        #endregion
        #region S&P 500 Sectors
        /// <summary>
        /// Materials Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLB = "splice:XLB,csv:backfills/XLB.csv";
        /// <summary>
        /// Communication Services Select Sector SPDR ETF
        /// (since Jun 2018, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLC = "splice:XLC,csv:backfills/XLC.csv";
        /// <summary>
        /// Engergy Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLE = "splice:XLE,csv:backfills/XLE.csv";
        /// <summary>
        /// Financial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLF = "splice:XLF,csv:backfills/XLF.csv";
        /// <summary>
        /// Industrial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLI = "splice:XLI,csv:backfills/XLI.csv";
        /// <summary>
        /// Technology Select Sector SPDR ETF
        /// </summary>
        /// (since Dec 1998, backfilled to Jan 1990)
        public static readonly string XLK = "splice:XLK,csv:backfills/XLK.csv";
        /// <summary>
        /// Consumer Staples Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLP = "splice:XLP,csv:backfills/XLP.csv";
        /// <summary>
        /// Real Estate Select Sector SPDR ETF
        /// (since Oct 2015, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLRE = "splice:XLRE,csv:backfills/XLRE.csv";
        /// <summary>
        /// Utilities Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLU = "splice:XLU,csv:backfills/XLU.csv";
        /// <summary>
        /// Health Care Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLV = "splice:XLV,csv:backfills/XLV.csv";
        /// <summary>
        /// Consumer Discretionary
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public static readonly string XLY = "splice:XLY,csv:backfills/XLY.csv";
        #endregion
        #region others
        /// <summary>
        /// iShares MSCI ACWI ex US ETF
        /// (since Mar 2008, backfilled to Jan 1980)
        /// </summary>
        public static readonly string ACWX = "splice:ACWX,csv:backfills/ACWX.csv";
        /// <summary>
        /// Vanguard FTSE All-World ex-US Small-Cap Index ETF
        /// (since Apr 2009, backfilled to Nov 1996)
        /// </summary>
        public static readonly string VSS = "splice:VSS,csv:backfills/VSS.csv";
        /// <summary>
        /// Vanguard FTSE All-World ex-US ETF
        /// (since Mar 2007)
        /// </summary>
        public static readonly string VEU = "splice:VEU,csv:backfills/VEU.csv";
        /// <summary>
        /// Vanguard FTSE Europe ETF
        /// </summary>
        public static readonly string VGK = "VGK";
        /// <summary>
        /// iShares MSCI Japan ETF
        /// </summary>
        public static readonly string EWJ = "EWJ";
        /// <summary>
        /// Vanguard MSCI Emerging Markets ETF
        /// </summary>
        public static readonly string VWO = "VWO";
        /// <summary>
        /// iShares MSCI Emerging Markets ETF
        /// </summary>
        public static readonly string EEM = "EEM";
        /// <summary>
        /// iShares MSCI EAFE ETF
        /// </summary>
        public static readonly string EFA = "EFA";
        #endregion
        #endregion
        #region bonds
        #region bond markets
        /// <summary>
        /// iShares Core US Aggregate Bond ETF
        /// (since Sep 2003, backfilled to Feb 1977)
        /// </summary>
        public static readonly string AGG = "splice:AGG,csv:backfills/AGG.csv";
        /// <summary>
        /// Vanguard Total Bond Market Index ETF
        /// </summary>
        public static readonly string BND = "splice:BND,csv:backfills/AGG.csv";
        #endregion
        #region U.S. Treasuries
        /// <summary>
        /// SPDR Bloomberg International Treasury Bond ETF
        /// (since Oct 2007)
        /// </summary>
        public static readonly string BWX = "splice:BWX,csv:backfills/BWX.csv";
        /// <summary>
        /// SPDR Bloomberg Barclays 1-3 Month T-Bill ETF
        /// (since May 2007, backfilled to Jan 1968)
        /// </summary>
        public static readonly string BIL = "splice:BIL,csv:backfills/BIL.csv";
        /// <summary>
        ///  iShares 1-3 Year Treasury Bond ETF
        ///  (since Jul 2002, backfilled to Jan 1968)
        /// </summary>
        public static readonly string SHY = "splice:SHY,csv:backfills/SHY.csv";
        /// <summary>
        /// iShares 3-7 Year Treasury Bond ETF
        /// (since Jan 2007, backfilled to Jan 1968)
        /// </summary>
        public static readonly string IEI = "splice:IEI,csv:backfills/IEI.csv";
        /// <summary>
        /// iShares 7-10 Year Treasury Bond ETF
        /// (since Jul 2002, backfilled to Jan 1968)
        /// </summary>
        public static readonly string IEF = "splice:IEF,csv:backfills/IEF.csv";
        /// <summary>
        /// iShares 10-20 Year Tresury Bond ETF
        /// (since Jan 2007, backfilled to Jan 1968)
        /// </summary>
        public static readonly string TLH = "splice:TLH,csv:backfills/TLH.csv";
        /// <summary>
        /// iShares 20+ Year Treasury Bond ETF
        /// (since Jul 2002, backfilled to Jan 1968)
        /// </summary>
        public static readonly string TLT = "splice:TLT,csv:backfills/TLT.csv";
        /// <summary>
        /// Vanguard Long-Term Treasury Index ETF
        /// (since Nov 2009, backfilled to Jan 1968)
        /// </summary>
        public static readonly string VGLT = "splice:VGLT,csv:backfills/VGLT.csv";
        /// <summary>
        /// iShares TIPS bond ETF
        /// (since Dec 2003, backfilled to Jun 2000)
        /// </summary>
        public static readonly string TIP = "splice:TIP,csv:backfills/TIP.csv";
        /// <summary>
        /// Vanguard Sht-Term Inflation-Protected Sec Idx ETF
        /// (since Oct 2012, backfilled to Jun 2000)
        /// </summary>
        public static readonly string VTIP = "splice:VTIP,csv:backfills/TIP.csv";
        #endregion
        #region U.S. Corporate
        /// <summary>
        /// iShares iBoxx $ Investment Grade Corporate Bond ETF
        /// (since Jul 2002, backfilled to Jan 1968)
        /// </summary>
        public static readonly string LQD = "splice:LQD,csv:backfills/LQD.csv";
        /// <summary>
        /// iShares 5-10 Yr Investment Grade Corporate Bond ETF
        /// (since Jan 2007, backfilled to Jan 1968)
        /// </summary>
        public static readonly string IGIB = "splice:IGIB,csv:backfills/IGIB.csv";
        /// <summary>
        /// iShares iBoxx $ High Yield Corporate Bond ETF
        /// (since Apr 2007, backfilled to Jan 1980)
        /// </summary>
        public static readonly string HYG = "splice:HYG,csv:backfills/HYG.csv";
        /// <summary>
        /// SPDR Bloomberg Barclays High Yield Bond ETF
        /// (since Dec 2007, backfilled to Jan 1980)
        /// </summary>
        public static readonly string JNK = "splice:JNK,csv:backfills/JNK.csv";
        /// <summary>
        /// SPDR Boomberg Barclay's Convertible Securities ETF
        /// (since April 2009, backfilled to April 2007)
        /// </summary>
        public static readonly string CWB = "splice:CWB,csv:backfills/CWB.csv";
        #endregion
        #endregion
        #region commodities
        /// <summary>
        /// SPDR Gold Shares ETF (since Nov 2004, backfilled to Jul 1982)
        /// </summary>
        public static readonly string GLD = "splice:GLD,csv:backfills/GLD.csv";
        /// <summary>
        /// iShares Silver Trust ETF (since Apr 2006)
        /// </summary>
        public static readonly string SLV = "SLV";
        /// <summary>
        /// Invesco DB Commodity Index Tracking ETF
        /// (since Feb 2006, backfilled to Jun 2002)
        /// </summary>
        public static readonly string DBC = "splice:DBC,csv:backfills/DBC.csv";
        /// <summary>
        /// iShares S&P GSCI Commodity-Indexed Trust
        /// </summary>
        public static readonly string GSG = "GSG";
        #endregion
        #region real estate
        /// <summary>
        /// Vanguard Real Estate Index ETF
        /// (since Sep 2004)
        /// </summary>
        public static readonly string VNQ = "VNQ";
        /// <summary>
        /// iShares Mortgage Real Estate ETF
        /// (since May 2007)
        /// </summary>
        public static readonly string REM = "REM";
        #endregion
        #region leveraged
        #region stocks
        #region S&P
        /// <summary>
        /// ProShares Ultra S&amp;P 500 ETF
        /// (since Jun 2006, backfilled to Jan 1988)
        /// </summary>
        public static readonly string SSO = "splice:SSO,csv:backfills/SSO.csv";
        /// <summary>
        /// Direxion Daily S&amp;P 500 Bull 2x Shares ETF
        /// (since May 2014, backfilled to Jan 1988)
        /// </summary>
        public static readonly string SPUU = "splice:SPUU,csv:backfills/SPUU.csv";
        /// <summary>
        /// ProShares UltraPro S&amp;P 500 ETF
        /// (since Jun 2009, backfilled to Jan 1970)
        /// </summary>
        public static readonly string UPRO = "splice:UPRO,csv:backfills/UPRO.csv";
        /// <summary>
        /// Direxion Daily S&amp;P 500 Bull 3x Shares ETF
        /// (since Nov 2008, backfilled to Jan 1970)
        /// </summary>
        public static readonly string SPXL = "splice:SPXL,csv:backfills/SPXL.csv";
        /// <summary>
        /// Direxion Daily S&amp;P 500 Bear 3x Shares ETF
        /// (since November 2008, backfilled to 1970)
        /// </summary>
        public static readonly string SPXU = "splice:SPXU,csv:backfills/SPXU.csv";
        /// <summary>
        /// ProShares Ultra MidCap400 ETF
        /// </summary>
        public static readonly string MVV = "MVV";
        /// <summary>
        /// ProShares UltraPro MidCap400 ETF
        /// (since Feb 2010, backfilled to May 1995)
        /// </summary>
        public static readonly string UMDD = "splice:UMDD,csv:backfills/UMDD.csv";
        /// <summary>
        /// Direxion Daily Mid Cap Bull 3X Shares ETF
        /// (since Jan 2009)
        /// </summary>
        public static readonly string MIDU = "MIDU";

        /// <summary>
        /// ProShares Ultra SmallCap600 ETF
        /// (since Jan 2007, backfilled to Oct 1994)
        /// </summary>
        public static readonly string SAA = "splice:SAA,csv:backfills/SAA.csv";
        /// <summary>
        /// Direxion Daily Small Cap Bull 3X ETF
        /// (since Nov 2008)
        /// </summary>
        public static readonly string TNA = "TNA"; // Small Cap bull 3x (since Nov 2008)
        #endregion
        #region Nasdaq
        /// <summary>
        /// ProShares Ultra QQQ ETF
        /// (since Jun 2006, backfilled to Mar 1999)
        /// </summary>
        public static readonly string QLD = "splice:QLD,csv:backfills/QLD.csv";
        /// <summary>
        /// ProShares UltraPro QQQ ETF
        /// (since Jan 2010, backfilled to Mar 1999)
        /// </summary>
        public static readonly string TQQQ = "splice:TQQQ,csv:backfills/TQQQ.csv";
        #endregion
        #region others
        /// <summary>
        /// ProShares Ultra Dow30 ETF
        /// </summary>
        public static readonly string DDM = "DDM";
        /// <summary>
        /// ProShares UltraPro Dow30 ETF
        /// (since Feb 2010, backfilled to Feb 1998)
        /// </summary>
        public static readonly string UDOW = "splice:UDOW,csv:backfills/UDOW.csv";
        /// <summary>
        /// ProShares Ultra Russell2000 ETF
        /// </summary>
        public static readonly string UWM = "UWM";
        /// <summary>
        /// ProShares UltraPro Russell2000 ETF
        /// (since Feb 2010, backfilled to May 2000)
        /// </summary>
        public static readonly string URTY = "splice:URTY,csv:backfills/URTY.csv";
        /// <summary>
        /// Direxion Daily MSCI Emerging Markets Bull 3x Shs ETF
        /// (since Dec 2008)
        /// </summary>
        public static readonly string EDC = "EDC";
        #endregion
        #region sectors
        /// <summary>
        /// Direxion Daily Energy Bull 2x Shares ETF
        /// (since Nov 2008)
        /// </summary>
        public static readonly string ERX = "ERX";
        /// <summary>
        /// Direxion Daily Financial Bull 3x Shares ETF
        /// (since Nov 2008)
        /// </summary>
        public static readonly string FAS = "FAS";
        /// <summary>
        /// Direxion Daily Technology Bull 3X Shares ETF
        /// (since Dec 2008)
        /// </summary>
        public static readonly string TECL = "TECL";
        /// <summary>
        /// Direxion Daily Healthcare Bull 3X Shares ETF
        /// (since June 2011)
        /// </summary>
        public static readonly string CURE = "CURE";
        /// <summary>
        /// Direxion Daily Retail Bull 3X Shares ETF
        /// (since July 2010)
        /// </summary>
        public static readonly string RETL = "RETL";
        #endregion
        #endregion
        #region bonds
        /// <summary>
        /// UJB	ProShares Ultra High Yield ETF
        /// (since April 2011)
        /// </summary>
        public static readonly string UJB = "UJB";
        /// <summary>
        /// ProShares Ultra 7-10 Year Treasury ETF
        /// (since January 2010, backfilled)
        /// </summary>
        public static readonly string UST = "splice:UST,csv:backfills/UST.csv";
        /// <summary>
        /// Direxion Daily 7-10 Year Treasury Bull 3x Shares ETF
        /// (inception April 2009, backfilled to July 2002)
        /// </summary>
        public static readonly string TYD = "splice:TYD,csv:backfills/TYD.csv";
        /// <summary>
        ///  ProShares Ultra 20+ Year Treasury ETF
        ///  (since Januaryh 2010, backfilled)
        /// </summary>
        public static readonly string UBT = "splice:UBT,csv:backfills/UBT.csv";
        /// <summary>
        /// Direxion Daily 20+ Year Treasury Bull 3X Shares
        /// (since April 2009, backfilled to July 2002)
        /// </summary>
        public static readonly string TMF = "splice:TMF,csv:backfills/TMF.csv";
        #endregion
        #region commodities
        /// <summary>
        /// ProShares Ultra Gold ETF
        /// (since Dec 2008, backfilled to Jul 1982)
        /// </summary>
        public static readonly string UGL = "splice:UGL,csv:backfills/UGL.csv";
        /// <summary>
        /// DB Gold Double Long ETN
        /// (since Feb 2008, backfilled to Jul 1982)
        /// </summary>
        public static readonly string DGP = "splice:DGP,csv:backfills/UGL.csv";
        /// <summary>
        /// Credit Suisse VelocityShares 3x Long Gold SP GSCI Gold Index ER ETN
        /// (since Oct 2011, backfilled to Jul 1982)
        /// </summary>
        public static readonly string UGLDF = "splice:UGLDF,csv:backfills/UGLDF.csv";
        #endregion
        #endregion
        #region volatility
        /// <summary>
        /// Barclays iPath Series B S&P 500 VIX Short-Term Futures ETN
        /// (since Jan 2018, backfilled to Jan 2008)
        /// </summary>
        public static readonly string VXX = "splice:VXX,csv:backfills/VXX.csv";
        /// <summary>
        /// ProShares VIX Short-Term Futures ETF
        /// (since Jan 2011, backfilled to Jan 2008)
        /// </summary>
        public static readonly string VIXY = "splice:VIXY,csv:backfills/VXX.csv";
        /// <summary>
        /// ProShares Ultra VIX Short Term Futures ETF
        /// </summary>
        public static readonly string UVXY = "UVXY";
        #endregion
        #region currencies
        public static readonly string UDN = "UDN"; // Invesco DB US Dollar Index Bearish ETF
        public static readonly string USDU = "USDU"; //WisdomTree Bloomberg US Dollar Bullish ETF

        #endregion
    }
}

//==============================================================================
// end of file
