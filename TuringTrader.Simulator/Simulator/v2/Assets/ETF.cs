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
    public class ETF
    {
        public const string SPY = "splice:SPY,SPY++";
        public const string VONE = "splice:VONE,IWB";// Vanguard Russell 1000 ETF (US large-cap stocks)
        public const string VIOO = "splice:VIOO,IJR"; // Vanguard Small-Cap 600 ETF (US small-cap stocks)
        public const string VEA = "splice:VEA,EFA";  // Vanguard FTSE Developed Markets ETF (developed-market large-cap stocks)
        public const string VWO = "VWO";             // Vanguard FTSE Emerging Markets ETF (emerging-market stocks)
        public const string VTV = "VTV";                 // Vanguard Value Index ETF
        public const string VUG = "VUG";                 // Vanguard Growth Index ETF
        public const string VIOV = "splice:VIOV,IJS";     // Vanguard S&P Small-Cap 600 Value Index ETF
        public const string VIOG = "splice:VIOG,IJT";     // Vanguard S&P Small-Cap 600 Growth Index ETF
        public const string VT = "VT"; // Vanguard Total World Stock Index ETF


        public const string AGG = "splice:AGG,AGG++";
        public const string BIL = "splice:BIL,BIL++";
        public const string IEF = "splice:IEF,IEF++";
        public const string TLT = "splice:TLT,TLT++";
        public const string SHY = "splice:SHY,SHY++";
        public const string VGLT = "splice:VGLT,TLT"; // Vanguard Long-Term Govt. Bond ETF (US Treasury bonds, long-term)
        public const string SHV = "splice:SHV,SHY";  // iShares Short-Term Treasury ETF (US Treasury bills, 1 to 12 months)
        public const string EDV = "splice:EDV,TLT";      // Vanguard Extended Duration ETF
        public const string VGIT = "splice:VGIT,IEF";     // Vanguard Intermediate-Term Treasury Index ETF
        public const string VCLT = "splice:VCLT,LQD";     // Vanguard Long-Term Corporate Bond Index ETF
        public const string BNDX = "splice:BNDX,BWX,SHY"; // Vanguard Total International Bond Index ETF

        public const string GLD = "splice:GLD,GLD++";
        public const string DBC = "splice:DBC,DBC++";
        public const string VNQ = "VNQ";             // Vanguard Real Estate ETF (REITs)
        public const string PDBC = "splice:PDBC,DBC"; // Invesco Optimum Yield Diversified Commodity Strategy ETF (Commodities)
        public const string IAU = "IAU";             // iShares Gold Trust (Gold)
    }
}

//==============================================================================
// end of file
