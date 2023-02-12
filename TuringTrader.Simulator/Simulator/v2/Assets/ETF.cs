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
    #region Legacy backfills from V1's glue logic
    internal class AssetsV1
    {
        #region stocks
        #region U.S. Stock Markets
        /// <summary>
        /// Vanguard Total Stock Market Index ETF
        /// (since May 2001)
        /// </summary>
        public const string VTI = "VTI";
        /// <summary>
        /// SPDR S&amp;P 500 Trust ETF
        /// (since Jan 1993, backfilled to Jan 1950)
        /// </summary>
        public const string SPY = "splice:SPY,SPY++";
        /// <summary>
        /// SPDR Portfolio S&P 500 Growth ETF (since Sep 2000)
        /// </summary>
        public const string SPYG = "SPYG";
        /// <summary>
        /// SPDR Portfolio S&P 500 Value ETF (since Sep 2000)
        /// </summary>
        public const string SPYV = "SPYV";
        /// <summary>
        /// Invesco S&P 500 Equal Weight ETF
        /// (since Apr 2003)
        /// </summary>
        public const string RSP = "RSP";
        /// <summary>
        /// Invesco QQQ Trust Series 1 ETF
        /// (since Mar 1999, backfilled to Mar 1999)
        /// </summary>
        public const string QQQ = "splice:QQQ,QQQ++";
        /// <summary>
        /// Dow Jones Industrial Average Trust ETF
        /// (since Jan 1998, backfilled to Oct 1987)
        /// </summary>
        public const string DIA = "splice:DIA,DIA++";
        /// <summary>
        /// iShares Russell 1000 Value ETF
        /// </summary>
        public const string IWD = "IWD";
        /// <summary>
        /// iShares Russell 2000 ETF
        /// (since May 2000, backfilled to Dec 1978)
        /// </summary>
        public const string IWM = "splice:IWM,IWM++";
        /// <summary>
        /// SPDR S&P MidCap 400 ETF
        /// (since May 1995, backfilled to July 1991)
        /// </summary>
        public const string MDY = "splice:MDY,MDY++";
        /// <summary>
        /// SPDR S&P 400 Mid Cap Growth ETF (since Nov 2005)
        /// </summary>
        public const string MDYG = "MDYG";
        /// <summary>
        /// SPDR S&P 400 Mid Cap Value ETF (since Nov 2005)
        /// </summary>
        public const string MDYV = "MDYV";
        /// <summary>
        /// SPDR S&P 600 SmallCap ETF
        /// (since Nov 2005, backfilled to Jan 1995)
        /// </summary>
        public const string SLY = "splice:SLY,SLY++";
        /// <summary>
        /// SPDR S&P 600 Small Cap Growth ETF (since Sep 2000)
        /// </summary>
        public const string SLYG = "SLYG";
        /// <summary>
        /// SPDR S&P 600 Small Cap Value ETF (since Sep 2000)
        /// </summary>
        public const string SLYV = "SLYV";
        #endregion
        #region S&P 500 Sectors
        /// <summary>
        /// Materials Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLB = "splice:XLB,XLB++";
        /// <summary>
        /// Communication Services Select Sector SPDR ETF
        /// (since Jun 2018, backfilled to Jan 1990)
        /// </summary>
        public const string XLC = "splice:XLC,XLC++";
        /// <summary>
        /// Engergy Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLE = "splice:XLE,XLE++";
        /// <summary>
        /// Financial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLF = "splice:XLF,XLF++";
        /// <summary>
        /// Industrial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLI = "splice:XLI,XLI++";
        /// <summary>
        /// Technology Select Sector SPDR ETF
        /// </summary>
        /// (since Dec 1998, backfilled to Jan 1990)
        public const string XLK = "splice:XLK,XLK++";
        /// <summary>
        /// Consumer Staples Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLP = "splice:XLP,XLP++";
        /// <summary>
        /// Real Estate Select Sector SPDR ETF
        /// (since Oct 2015, backfilled to Jan 1990)
        /// </summary>
        public const string XLRE = "splice:XLRE,XLRE++";
        /// <summary>
        /// Utilities Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLU = "splice:XLU,XLU++";
        /// <summary>
        /// Health Care Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLV = "splice:XLV,XLV++";
        /// <summary>
        /// Consumer Discretionary
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLY = "splice:XLY,XLY++";
        #endregion
        #region others
        /// <summary>
        /// iShares MSCI ACWI ex US ETF
        /// (since Mar 2008, backfilled to Jan 1980)
        /// </summary>
        public const string ACWX = "splice:ACWX,ACWX++";
        /// <summary>
        /// Vanguard FTSE All-World ex-US Small-Cap Index ETF
        /// (since Apr 2009, backfilled to Nov 1996)
        /// </summary>
        public const string VSS = "splice:VSS,VSS++";
        /// <summary>
        /// Vanguard FTSE All-World ex-US ETF
        /// (since Mar 2007)
        /// </summary>
        public const string VEU = "splice:VEU,VEU++";
        /// <summary>
        /// Vanguard FTSE Europe ETF
        /// </summary>
        public const string VGK = "VGK";
        /// <summary>
        /// iShares MSCI Japan ETF
        /// </summary>
        public const string EWJ = "EWJ";
        /// <summary>
        /// Vanguard MSCI Emerging Markets ETF
        /// </summary>
        public const string VWO = "VWO";
        /// <summary>
        /// iShares MSCI Emerging Markets ETF
        /// </summary>
        public const string EEM = "EEM";
        /// <summary>
        /// iShares MSCI EAFE ETF
        /// </summary>
        public const string EFA = "EFA";
        #endregion
        #endregion
        #region bonds
        #region bond markets
        /// <summary>
        /// iShares Core US Aggregate Bond ETF
        /// (since Sep 2003, backfilled to Feb 1977)
        /// </summary>
        public const string AGG = "splice:AGG,AGG++";
        /// <summary>
        /// Vanguard Total Bond Market Index ETF
        /// </summary>
        public const string BND = "splice:BND,AGG++";
        #endregion
        #region U.S. Treasuries
        /// <summary>
        /// SPDR Bloomberg International Treasury Bond ETF
        /// (since Oct 2007)
        /// </summary>
        public const string BWX = "splice:BWX,BWX++";
        /// <summary>
        /// SPDR Bloomberg Barclays 1-3 Month T-Bill ETF
        /// (since May 2007, backfilled to Jan 1968)
        /// </summary>
        public const string BIL = "splice:BIL,BIL++";
        /// <summary>
        ///  iShares 1-3 Year Treasury Bond ETF
        ///  (since Jul 2002, backfilled to Jan 1968)
        /// </summary>
        public const string SHY = "splice:SHY,SHY++";
        /// <summary>
        /// iShares 3-7 Year Treasury Bond ETF
        /// (since Jan 2007, backfilled to Jan 1968)
        /// </summary>
        public const string IEI = "splice:IEI,IEI++";
        /// <summary>
        /// iShares 7-10 Year Treasury Bond ETF
        /// (since Jul 2002, backfilled to Jan 1968)
        /// </summary>
        public const string IEF = "splice:IEF,IEF++";
        /// <summary>
        /// iShares 10-20 Year Tresury Bond ETF
        /// (since Jan 2007, backfilled to Jan 1968)
        /// </summary>
        public const string TLH = "splice:TLH,TLH++";
        /// <summary>
        /// iShares 20+ Year Treasury Bond ETF
        /// (since Jul 2002, backfilled to Jan 1968)
        /// </summary>
        public const string TLT = "splice:TLT,TLT++";
        /// <summary>
        /// Vanguard Long-Term Treasury Index ETF
        /// (since Nov 2009, backfilled to Jan 1968)
        /// </summary>
        public const string VGLT = "splice:VGLT,VGLT++";
        /// <summary>
        /// iShares TIPS bond ETF
        /// (since Dec 2003, backfilled to Jun 2000)
        /// </summary>
        public const string TIP = "splice:TIP,TIP++";
        /// <summary>
        /// Vanguard Sht-Term Inflation-Protected Sec Idx ETF
        /// (since Oct 2012, backfilled to Jun 2000)
        /// </summary>
        public const string VTIP = "splice:VTIP,TIP++";
        #endregion
        #region U.S. Corporate
        /// <summary>
        /// iShares iBoxx $ Investment Grade Corporate Bond ETF
        /// (since Jul 2002, backfilled to Jan 1968)
        /// </summary>
        public const string LQD = "splice:LQD,LQD++";
        /// <summary>
        /// iShares 5-10 Yr Investment Grade Corporate Bond ETF
        /// (since Jan 2007, backfilled to Jan 1968)
        /// </summary>
        public const string IGIB = "splice:IGIB,IGIB++";
        /// <summary>
        /// iShares iBoxx $ High Yield Corporate Bond ETF
        /// (since Apr 2007, backfilled to Jan 1980)
        /// </summary>
        public const string HYG = "splice:HYG,HYG++";
        /// <summary>
        /// SPDR Bloomberg Barclays High Yield Bond ETF
        /// (since Dec 2007, backfilled to Jan 1980)
        /// </summary>
        public const string JNK = "splice:JNK,JNK++";
        /// <summary>
        /// SPDR Boomberg Barclay's Convertible Securities ETF
        /// (since April 2009, backfilled to April 2007)
        /// </summary>
        public const string CWB = "splice:CWB,CWB++";
        #endregion
        #endregion
        #region commodities
        /// <summary>
        /// SPDR Gold Shares ETF (since Nov 2004, backfilled to Jul 1982)
        /// </summary>
        public const string GLD = "splice:GLD,GLD++";
        /// <summary>
        /// iShares Silver Trust ETF (since Apr 2006)
        /// </summary>
        public const string SLV = "SLV";
        /// <summary>
        /// Invesco DB Commodity Index Tracking ETF
        /// (since Feb 2006, backfilled to Jun 2002)
        /// </summary>
        public const string DBC = "splice:DBC,DBC++";
        /// <summary>
        /// iShares S&P GSCI Commodity-Indexed Trust
        /// </summary>
        public const string GSG = "GSG";
        #endregion
        #region real estate
        /// <summary>
        /// Vanguard Real Estate Index ETF
        /// (since Sep 2004)
        /// </summary>
        public const string VNQ = "VNQ";
        /// <summary>
        /// iShares Mortgage Real Estate ETF
        /// (since May 2007)
        /// </summary>
        public const string REM = "REM";
        #endregion
        #region leveraged
        #region stocks
        #region S&P
        /// <summary>
        /// ProShares Ultra S&amp;P 500 ETF
        /// (since Jun 2006, backfilled to Jan 1988)
        /// </summary>
        public const string SSO = "splice:SSO,SSO++";
        /// <summary>
        /// Direxion Daily S&amp;P 500 Bull 2x Shares ETF
        /// (since May 2014, backfilled to Jan 1988)
        /// </summary>
        public const string SPUU = "splice:SPUU,SPUU++";
        /// <summary>
        /// ProShares UltraPro S&amp;P 500 ETF
        /// (since Jun 2009, backfilled to Jan 1970)
        /// </summary>
        public const string UPRO = "splice:UPRO,UPRO++";
        /// <summary>
        /// Direxion Daily S&amp;P 500 Bull 3x Shares ETF
        /// (since Nov 2008, backfilled to Jan 1970)
        /// </summary>
        public const string SPXL = "splice:SPXL,SPXL++";
        /// <summary>
        /// Direxion Daily S&amp;P 500 Bear 3x Shares ETF
        /// (since November 2008, backfilled to 1970)
        /// </summary>
        public const string SPXU = "splice:SPXU,SPXU++";
        /// <summary>
        /// ProShares Ultra MidCap400 ETF
        /// </summary>
        public const string MVV = "MVV";
        /// <summary>
        /// ProShares UltraPro MidCap400 ETF
        /// (since Feb 2010, backfilled to May 1995)
        /// </summary>
        public const string UMDD = "splice:UMDD,UMDD++";
        /// <summary>
        /// Direxion Daily Mid Cap Bull 3X Shares ETF
        /// (since Jan 2009)
        /// </summary>
        public const string MIDU = "MIDU";
        /// <summary>
        /// ProShares Ultra SmallCap600 ETF
        /// (since Jan 2007, backfilled to Oct 1994)
        /// </summary>
        public const string SAA = "splice:SAA,SAA++";
        /// <summary>
        /// Direxion Daily Small Cap Bull 3X ETF
        /// (since Nov 2008)
        /// </summary>
        public const string TNA = "TNA"; // Small Cap bull 3x (since Nov 2008)
        #endregion
        #region Nasdaq
        /// <summary>
        /// ProShares Ultra QQQ ETF
        /// (since Jun 2006, backfilled to Mar 1999)
        /// </summary>
        public const string QLD = "splice:QLD,QLD++";
        /// <summary>
        /// ProShares UltraPro QQQ ETF
        /// (since Jan 2010, backfilled to Mar 1999)
        /// </summary>
        public const string TQQQ = "splice:TQQQ,TQQQ++";
        #endregion
        #region others
        /// <summary>
        /// ProShares Ultra Dow30 ETF
        /// </summary>
        public const string DDM = "DDM";
        /// <summary>
        /// ProShares UltraPro Dow30 ETF
        /// (since Feb 2010, backfilled to Feb 1998)
        /// </summary>
        public const string UDOW = "splice:UDOW,UDOW++";
        /// <summary>
        /// ProShares Ultra Russell2000 ETF
        /// </summary>
        public const string UWM = "UWM";
        /// <summary>
        /// ProShares UltraPro Russell2000 ETF
        /// (since Feb 2010, backfilled to May 2000)
        /// </summary>
        public const string URTY = "splice:URTY,URTY++";
        /// <summary>
        /// Direxion Daily MSCI Emerging Markets Bull 3x Shs ETF
        /// (since Dec 2008)
        /// </summary>
        public const string EDC = "EDC";
        #endregion
        #region sectors
        /// <summary>
        /// Direxion Daily Energy Bull 2x Shares ETF
        /// (since Nov 2008)
        /// </summary>
        public const string ERX = "ERX";
        /// <summary>
        /// Direxion Daily Financial Bull 3x Shares ETF
        /// (since Nov 2008)
        /// </summary>
        public const string FAS = "FAS";
        /// <summary>
        /// Direxion Daily Technology Bull 3X Shares ETF
        /// (since Dec 2008)
        /// </summary>
        public const string TECL = "TECL";
        /// <summary>
        /// Direxion Daily Healthcare Bull 3X Shares ETF
        /// (since June 2011)
        /// </summary>
        public const string CURE = "CURE";
        /// <summary>
        /// Direxion Daily Retail Bull 3X Shares ETF
        /// (since July 2010)
        /// </summary>
        public const string RETL = "RETL";
        #endregion
        #endregion
        #region bonds
        /// <summary>
        /// UJB	ProShares Ultra High Yield ETF
        /// (since April 2011)
        /// </summary>
        public const string UJB = "UJB";
        /// <summary>
        /// ProShares Ultra 7-10 Year Treasury ETF
        /// (since January 2010, backfilled)
        /// </summary>
        public const string UST = "splice:UST,UST++";
        /// <summary>
        /// Direxion Daily 7-10 Year Treasury Bull 3x Shares ETF
        /// (inception April 2009, backfilled to July 2002)
        /// </summary>
        public const string TYD = "splice:TYD,TYD++";
        /// <summary>
        ///  ProShares Ultra 20+ Year Treasury ETF
        ///  (since Januaryh 2010, backfilled)
        /// </summary>
        public const string UBT = "splice:UBT,UBT++";
        /// <summary>
        /// Direxion Daily 20+ Year Treasury Bull 3X Shares
        /// (since April 2009, backfilled to July 2002)
        /// </summary>
        public const string TMF = "splice:TMF,TMF++";
        #endregion
        #region commodities
        /// <summary>
        /// ProShares Ultra Gold ETF
        /// (since Dec 2008, backfilled to Jul 1982)
        /// </summary>
        public const string UGL = "splice:UGL,UGL++";
        /// <summary>
        /// DB Gold Double Long ETN
        /// (since Feb 2008, backfilled to Jul 1982)
        /// </summary>
        public const string DGP = "splice:DGP,UGL++";
        /// <summary>
        /// Credit Suisse VelocityShares 3x Long Gold SP GSCI Gold Index ER ETN
        /// (since Oct 2011, backfilled to Jul 1982)
        /// </summary>
        public const string UGLDF = "splice:UGLDF,UGLDF++";
        #endregion
        #endregion
        #region volatility
        /// <summary>
        /// Barclays iPath Series B S&P 500 VIX Short-Term Futures ETN
        /// (since Jan 2018, backfilled to Jan 2008)
        /// </summary>
        public const string VXX = "splice:VXX,VXX++";
        /// <summary>
        /// ProShares VIX Short-Term Futures ETF
        /// (since Jan 2011, backfilled to Jan 2008)
        /// </summary>
        public const string VIXY = "splice:VIXY,VXX++";
        /// <summary>
        /// ProShares Ultra VIX Short Term Futures ETF
        /// </summary>
        public const string UVXY = "UVXY";
        #endregion
        #region currencies
        public const string UDN = "UDN"; // Invesco DB US Dollar Index Bearish ETF
        public const string USDU = "USDU"; //WisdomTree Bloomberg US Dollar Bullish ETF

        #endregion
    }
    #endregion

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
        public const string SPY = AssetsV1.SPY;
        /// <summary>
        /// SPDR Portfolio S&amp;P 500 Growth ETF (since Sep 2000)
        /// </summary>
        public const string SPYG = AssetsV1.SPYG;
        /// <summary>
        /// SPDR Portfolio S&ampP 500 Value ETF (since Sep 2000)
        /// </summary>
        public const string SPYV = AssetsV1.SPYV;
        /// <summary>
        /// SPDR S&amp;P MidCap 400 ETF
        /// (since May 1995, backfilled to July 1991)
        /// </summary>
        public const string MDY = AssetsV1.MDY;
        /// <summary>
        /// SPDR S&amp;P 400 Mid Cap Growth ETF (since Nov 2005)
        /// </summary>
        public const string MDYG = AssetsV1.MDYG;
        /// <summary>
        /// SPDR S&amp;P 400 Mid Cap Value ETF (since Nov 2005)
        /// </summary>
        public const string MDYV = AssetsV1.MDYV;
        /// <summary>
        /// SPDR S&amp;P 600 SmallCap ETF
        /// (since Nov 2005, backfilled to Jan 1995)
        /// </summary>
        public const string SLY = AssetsV1.SLY;
        /// <summary>
        /// SPDR S&amp;P 600 Small Cap Growth ETF (since Sep 2000)
        /// </summary>
        public const string SLYG = AssetsV1.SLYG;
        /// <summary>
        /// SPDR S&amp;P 600 Small Cap Value ETF (since Sep 2000)
        /// </summary>
        public const string SLYV = AssetsV1.SLYV;
        /// <summary>
        /// Invesco QQQ Trust Series 1 ETF. Available since March 1999.
        /// </summary>
        public const string QQQ = AssetsV1.QQQ;
        /// <summary>
        /// iShares Russell 2000 ETF. Available since June 2000.
        /// </summary>
        public const string IWM = AssetsV1.IWM;
        /// <summary>
        /// Vanguard European Stock Index ETF. Available since March 2005.
        /// </summary>
        public const string VGK = AssetsV1.VGK;
        /// <summary>
        /// iShares MSCI Japan ETF. Available since March 1996.
        /// </summary>
        public const string EWJ = AssetsV1.EWJ;
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
        public const string VWO = AssetsV1.VWO;
        /// <summary>
        /// Vanguard Value Index ETF. Available since February 2004.
        /// </summary>
        public const string VTV = "VTV";
        /// <summary>
        /// Vanguard Growth Index ETF. Available since February 2004.
        /// </summary>
        public const string VUG = "VUG";
        /// <summary>
        /// Vanguard S&amp;P Small-Cap 600 Index ETF. Available since September 2010.
        /// </summary>
        public const string VIOV = "splice:VIOV,IJS";
        /// <summary>
        /// Vanguard S&amp;P Small-Cap 600 Growth Index ETF. Available September 2010.
        /// </summary>
        public const string VIOG = "splice:VIOG,IJT";
        /// <summary>
        /// Vanguard Total World Stock Index ETF. Available since July 2008.
        /// </summary>
        public const string VT = "VT";
        #endregion
        #region S&P sectors
        /// <summary>
        /// Materials Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLB = AssetsV1.XLB;
        /// <summary>
        /// Communication Services Select Sector SPDR ETF
        /// (since Jun 2018, backfilled to Jan 1990)
        /// </summary>
        public const string XLC = AssetsV1.XLC;
        /// <summary>
        /// Engergy Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLE = AssetsV1.XLE;
        /// <summary>
        /// Financial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLF = AssetsV1.XLF;
        /// <summary>
        /// Industrial Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLI = AssetsV1.XLI;
        /// <summary>
        /// Technology Select Sector SPDR ETF
        /// </summary>
        /// (since Dec 1998, backfilled to Jan 1990)
        public const string XLK = AssetsV1.XLK;
        /// <summary>
        /// Consumer Staples Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLP = AssetsV1.XLP;
        /// <summary>
        /// Real Estate Select Sector SPDR ETF
        /// (since Oct 2015, backfilled to Jan 1990)
        /// </summary>
        public const string XLRE = AssetsV1.XLRE;
        /// <summary>
        /// Utilities Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLU = AssetsV1.XLU;
        /// <summary>
        /// Health Care Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLV = AssetsV1.XLV;
        /// <summary>
        /// Consumer Discretionary Select Sector SPDR ETF
        /// (since Dec 1998, backfilled to Jan 1990)
        /// </summary>
        public const string XLY = AssetsV1.XLY;
        #endregion
        #region non-US and others
        #endregion
        #endregion
        #region bonds
        #region broad markets
        /// <summary>
        /// iShares Core US Aggregate Bond ETF. Available since October 2003.
        /// </summary>
        public const string AGG = AssetsV1.AGG;
        /// <summary>
        /// Vanguard Total Bond Market Index ETF. Available since April 2007.
        /// </summary>
        public const string BND = AssetsV1.BND;
        /// <summary>
        /// Vanguard Total International Bond Index ETF. Available since June 2013.
        /// </summary>
        public const string BNDX = "splice:BNDX,BWX,SHY";
        #endregion
        #region short-term (0-3yr) treasuries
        /// <summary>
        /// SPDR Bloomberg 1-3 Month T-Bill ETF. Available since June 2007.
        /// </summary>
        public const string BIL = AssetsV1.BIL;
        /// <summary>
        /// iShares 1-3 Year Treasury Bond ETF. Available since August 2002.
        /// </summary>
        public const string SHY = AssetsV1.SHY;
        /// <summary>
        /// iShares Short Treasury Bond ETF. Available since January 2007.
        /// </summary>
        public const string SHV = "SHV";
        #endregion
        #region intermediate-term (3-10yr) treasuries
        /// <summary>
        /// iShares 3-7 Year Treasury Bond ETF. Available since January 2007.
        /// </summary>
        public const string IEI = AssetsV1.IEI;
        /// <summary>
        /// iShares 7-10 Year Treasury Bond ETF. Available since August 2002.
        /// </summary>
        public const string IEF = AssetsV1.IEF;
        /// <summary>
        /// Vanguard Intermediate-Term Treasury Index ETF. Available since December 2009.
        /// </summary>
        public const string VGIT = "splice:VGIT,IEF";
        #endregion
        #region long-term (10+yr) treasuries
        /// <summary>
        /// iShares 10-20 Year Treasury Bond ETF. Available since January 2007.
        /// </summary>
        public const string TLH = AssetsV1.TLH;
        /// <summary>
        /// iShares 20 Plus Year Treasury Bond ETF. Available since August 2002.
        /// </summary>
        public const string TLT = AssetsV1.TLT;
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
        public const string TIP = AssetsV1.TIP;
        #endregion
        #region corporate bonds
        /// <summary>
        /// iShares iBoxx $ Investment Grade Corporate Bond ETF. Available since August 2002.
        /// </summary>
        public const string LQD = AssetsV1.LQD;
        /// <summary>
        /// iShares iBoxx $ High Yield Corporate Bond ETF. Available since April 2007.
        /// </summary>
        public const string HYG = AssetsV1.HYG;
        /// <summary>
        /// Vanguard Long-Term Corporate Bond Index ETF. Available since December 2009.
        /// </summary>
        public const string VCLT = "splice:VCLT,LQD";
        /// <summary>
        /// SPDR Bloomberg High Yield Bond ETF. Available since December 2007.
        /// </summary>
        public const string JNK = AssetsV1.JNK;
        #endregion
        #endregion
        #region commodities & hard assets
        /// <summary>
        /// SPDR Gold Shares ETF. Available since November 2004.
        /// </summary>
        public const string GLD = AssetsV1.GLD;
        /// <summary>
        /// Invesco DB Commodity Index Tracking ETF. Available since February 2006.
        /// </summary>
        public const string DBC = AssetsV1.DBC;
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
