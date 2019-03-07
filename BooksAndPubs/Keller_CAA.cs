//==============================================================================
// Project:     TuringTrader, algorithms from books & publications
// Name:        Keller_CAA
// Description: Classical Asset Allocation (CAA) strategy, as published in 
//              Wouter J. Keller, Adam Butler, and Ilya Kipnis' paper 
//              'Momentum and Markowitz: a Golden Combination'.
//              https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2606884
// History:     2019iii07, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2019, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     This code is licensed under the term of the
//              GNU Affero General Public License as published by 
//              the Free Software Foundation, either version 3 of 
//              the License, or (at your option) any later version.
//              see: https://www.gnu.org/licenses/agpl-3.0.en.html
//==============================================================================

//--- target volatility selection - enable only one of these
#define TVOL_AGGRESSIVE   // from paper
//#define TVOL_DEFENSIVE    // from paper
//#define TVOL_MAX_SHARPE   // FUB addition
//#define TVOL_MIN_VARIANCE // FUB addition

//--- universe selection - enable only one of these
#define UNIVERSE_N8    // from paper
//#define UNIVERSE_N16   // from paper
//#define UNIVERSE_N39   // from paper
//#define UNIVERSE_G4    // FUB addition (see Keller's DAA)
//#define UNIVERSE_G12   // FUB addition (see Keller's DAA)
//#define UNIVERSE_FUB3  // FUB addition
//#define UNIVERSE_FUB4  // FUB addition
//#define UNIVERSE_FUB12 // FUB addition

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuringTrader.Simulator;
#endregion

namespace BooksAndPubs
{
    public class Keller_CAA : Algorithm
    {
        #region target volatility settings
#if TVOL_DEFENSIVE
        private readonly string TVOL_NICK = "TV=5%";
        private readonly double TVOL = 0.05;
#endif
#if TVOL_AGGRESSIVE
        private readonly string TVOL_NICK = "TV=10%";
        private readonly double TVOL = 0.10;
#endif
#if TVOL_MAX_SHARPE
        private readonly string TVOL_NICK = "Max Sharpe Ratio";
#endif
#if TVOL_MIN_VARIANCE
        private readonly string TVOL_NICK = "Min Variance";
#endif
        #endregion
        #region instrument settings
#if UNIVERSE_N8
        private readonly string UNIVERSE_NICK = "N=8";
        private readonly string[] RISKY_ASSETS =
        {
            "SPY.etf", // S&P 500
            "EFA.etf", // EAFE
            "EEM.etf", // Emerging Markets
            "QQQ.etf", // US Technology Sector
            "EWJ.etf", // Japanese Equities
            "HYG.etf", // High Yield Bonds
        };
        private readonly double MAX_RISKY_ALLOC = 0.25;
        private readonly string[] SAFE_ASSETS =
        {
            "IEF.etf", // 10-Year Treasuries
            "BIL.etf", // T-Bills
        };
#endif
#if UNIVERSE_N16
#error This universe is not defined, yet
        private readonly string UNIVERSE_NICK = "N=16";
        private readonly string[] RISKY_ASSETS =
        {
            "", // Non - Durables
            "", // Durables
            "", // Manufacturing
            "", // Energy
            "", // Technology
            "", // Telecom
            "", // Shops
            "", // Health
            "", // Utilities
            "", // Other
            "", // 10-Year Treasuries
            "", // 30-Year Treasuries
            "", // U.S. Municipal Bonds
            "", // U.S. Corporate Bonds
            "", // U.S High Yield Bonds
            "", // T-Bills
        };
        private readonly double MAX_RISKY_ALLOC = 0.25;
        private readonly string[] SAFE_ASSETS =
        {
            "IEF.etf", // 10-Year Treasuries
            "BIL.etf", // T-Bills
        };
#endif
#if UNIVERSE_N39
#error This universe is not defined, yet
        private readonly string UNIVERSE_NICK = "N=16";
        private readonly string[] RISKY_ASSETS =
        {
            "", // S&P 500
            "", // US Small Caps
            "", // EAFE
            "", // Emerging Markets
            "", // Japanese Equities
            "", // Non-Durables
            "", // Durables
            "", // Manufacturing
            "", // Energy
            "", // Technology
            "", // Telecom 
            "", // Shops
            "", // Health
            "", // Utilities
            "", // Other Sector
            "", // Dow Utilities
            "", // Dow Transports
            "", // Dow Industrials
            "", // FTSE US 1000
            "", // FTSE US 1500 
            "", // FTSE Global ex-US
            "", // FTSE Developed Equities
            "", // FTSE Emerging Markets
            "", // 10-Year Treasuries
            "", // 30-Year Treasuries
            "", // U.S. TIPs
            "", // U.S. Municipal Bonds
            "", // U.S. Corporate Bonds
            "", // U.S High Yield Bonds
            "", // T-Bills
            "", // Int’l Gov’t Bonds 
            "", // Japan 10-Year Gov’t Bond
            "", // Commodities (GSCI)
            "", // Gold
            "", // REITs
            "", // Mortgage REITs
            "", // FX (1x )
            "", // FX (2x)
            "", // Timber
        };
        private readonly double MAX_RISKY_ALLOC = 0.25;
        private readonly string[] SAFE_ASSETS =
        {
            "IEF.etf", // 10-Year Treasuries
            "BIL.etf", // T-Bills
        };
#endif
#if UNIVERSE_G4
        private readonly string UNIVERSE_NICK = "G4";
        private static string[] RISKY_ASSETS =
        {
            "SPY.etf", // SPDR S&P 500 ETF
            "VEA.etf", // Vanguard FTSE Developed Markets ETF
            "VWO.etf", // Vanguard FTSE Emerging Markets ETF
            "BND.etf", // Vanguard Total Bond Market ETF
        };
        private readonly double MAX_RISKY_ALLOC = 0.70; // target of 2 risky assets
        private static string[] SAFE_ASSETS =
        {
            "SHY.etf", // iShares 1-3 Year Treasury Bond ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
            "LQD.etf"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };
#endif
#if UNIVERSE_G12
        private static string UNIVERSE_NICK = "G12";
        private static string[] RISKY_ASSETS =
        {
            "SPY.etf", // SPDR S&P 500 ETF
            "IWM.etf", // iShares Russell 2000 ETF
            "QQQ.etf", // Invesco Nasdaq-100 ETF
            "VGK.etf", // Vanguard FTSE Europe ETF
            "EWJ.etf", // iShares MSCI Japan ETF
            "VWO.etf", // Vanguard MSCI Emerging Markets ETF
            "VNQ.etf", // Vanguard Real Estate ETF
            "GSG.etf", // iShares S&P GSCI Commodity-Indexed Trust
            "GLD.etf", // SPDR Gold Trust ETF
            "TLT.etf", // iShares 20+ Year Treasury Bond ETF
            "HYG.etf", // iShares iBoxx High Yield Corporate Bond ETF
            //"LQD.etf"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };
        private readonly double MAX_RISKY_ALLOC = 0.20; // target of 6 risky assets
        private static string[] SAFE_ASSETS =
        {
            "SHY.etf", // iShares 1-3 Year Treasury Bond ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
            "LQD.etf"  // iShares iBoxx Investment Grade Corporate Bond ETF
        };
#endif
#if UNIVERSE_FUB3
        private readonly string UNIVERSE_NICK = "FUB-3";
        private readonly string[] RISKY_ASSETS =
        {
            "VTI.etf",
            "TLT.etf", 
        };
        private readonly double MAX_RISKY_ALLOC = 0.70;
        private string[] SAFE_ASSETS =
        {
            "BIL.etf", // T-Bills
        };
#endif
#if UNIVERSE_FUB4
        private readonly string UNIVERSE_NICK = "FUB-4";
        private readonly string[] RISKY_ASSETS =
        {
            "VTI.etf", // US equities
            "VNQ.etf", // Real estate
        };
        private readonly double MAX_RISKY_ALLOC = 0.70;
        private string[] SAFE_ASSETS =
        {
            "LQD.etf", // US investment grade bonds
            "IEF.etf", // Intermediate-term treasuries
        };
#endif
#if UNIVERSE_FUB12
        private readonly string UNIVERSE_NICK = "FUB-12";
        private readonly string[] RISKY_ASSETS =
        {
            "SPY.etf", // SPDR S&P 500 Trust ETF
            "QQQ.etf", // Invesco QQQ Nasdaq-100 ETF
            "IWM.etf", // iShares Russell 200 ETF
            "VGK.etf", // Vanguard European Stock Index ETF
            "EEM.etf", // iShares MSCI Emerging Markets ETF
            "DXJ.etf", // Wisdom Tree Japan Hedged Equity ETF
            "VNQ.etf", // Vanguard Real Estate ETF
            "RWX.etf", // SPDR Dow Jones International Real Estate ETF
            "DBC.etf", // Invesco DB Commodity Index Tracking ETF
            "GLD.etf", // SPDR Gold Shares ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
            "TLT.etf", // iShares 20+ Year Treasury Bond ETF
        };
        private readonly double MAX_RISKY_ALLOC = 0.25;
        private string[] SAFE_ASSETS =
        {
            "LQD.etf", // iShares iBoxx $ Investment Grade Corporate Bond ETF
            "IEF.etf", // iShares 7-10 Year Treasury Bond ETF
        };
#endif
        #endregion
        #region internal data
        private readonly string _benchmark = "^SPX.index";
        private Plotter _plotter = new Plotter();
        #endregion

        #region public override void Run()
        public override void Run()
        {
            //---------- initialization

            StartTime = DateTime.Parse("01/01/1990");
            EndTime = DateTime.Now - TimeSpan.FromDays(5);

            // our universe consists of risky & safe assets
            var universe = RISKY_ASSETS
                .Concat(SAFE_ASSETS).ToList();

            // add all data sources
            AddDataSource(_benchmark);
            foreach (var nick in universe)
                AddDataSource(nick);

            Deposit(1e6);
            CommissionPerShare = 0.015;

            string strategyName = string.Format("Classical Asset Allocation: {0}, {1}",
                UNIVERSE_NICK, TVOL_NICK);

            //---------- simulation loop

            foreach (var simTime in SimTimes)
            {
                // calculate indicators on overy bar
                Dictionary<Instrument, double> momentum = Instruments
                    .ToDictionary(
                        i => i,
                        i => (1.0 * i.Close.Momentum(21)[0]
                            + 3.0 * i.Close.Momentum(63)[0]
                            + 6.0 * i.Close.Momentum(126)[0]
                            + 12.0 * i.Close.Momentum(252)[0]) / 22.0);

                // skip if there are any instruments missing from our universe
                if (universe.Where(n => Instruments.Where(i => i.Nickname == n).Count() == 0).Count() > 0)
                    continue;

                // trigger rebalancing
                if (SimTime[0].Month != SimTime[1].Month) // monthly
                //if (SimTime[0].DayOfWeek < SimTime[1].DayOfWeek) // weekly
                {
                    // calculate covariance
                    var covar = new PortfolioSupport.PortfolioCovariance(Instruments, 12, 21); // 12 monthly bars
                    //var covar = new PortfolioSupport.PortfolioCovariance(Instruments, 63); // 63 daily bars

                    // calculate efficient frontier for universe
                    // note how momentum and covariance are annualized here
                    var cla = new PortfolioSupport.MarkowitzCLA(
                        Instruments.Where(i => universe.Contains(i.Nickname)),
                        i => 252.0 * momentum[i],
                        (i, j) => Math.Sqrt(252.0 / covar.BarSize) * covar[i, j],
                        i => 0.0,
                        i => SAFE_ASSETS.Contains(i.Nickname) ? 1.0 : MAX_RISKY_ALLOC);

                    // find portfolio with specified risk
#if TVOL_AGGRESSIVE || TVOL_DEFENSIVE
                    // target volatility is annualized, pf is monthly
                    var pf = cla.DefinedRisk(TVOL);
#endif
#if TVOL_MAX_SHARPE
                    var pf = cla.MaximumSharpeRatio();
#endif
#if TVOL_MIN_VARIANCE
                    var pf = cla.MinimumVariance();
#endif

                    Output.WriteLine("{0:MM/dd/yyyy}: {1}", SimTime[0], pf.ToString());

                    // adjust all positions
                    foreach (var i in pf.Weights.Keys)
                    {
                        int targetShares = (int)Math.Floor(NetAssetValue[0] * pf.Weights[i] / i.Close[0]);
                        int currentShares = i.Position;

                        var ticket = i.Trade(targetShares - currentShares);

                        if (ticket != null)
                        {
                            if (i.Position == 0) ticket.Comment = "open";
                            else if (targetShares == 0) ticket.Comment = "close";
                            else ticket.Comment = "rebalance";
                        }
                    }
                }

                _plotter.SelectChart(strategyName, "date");
                _plotter.SetX(SimTime[0]);
                _plotter.Plot("NAV", NetAssetValue[0]);
                _plotter.Plot(FindInstrument(_benchmark).Symbol, FindInstrument(_benchmark).Close[0]);
            }

            //---------- post-processing

            _plotter.SelectChart(strategyName + " trades", "date");
            foreach (LogEntry entry in Log)
            {
                _plotter.SetX(entry.BarOfExecution.Time);
                _plotter.Plot("action", entry.Action);
                _plotter.Plot("type", entry.InstrumentType);
                _plotter.Plot("instr", entry.Symbol);
                _plotter.Plot("qty", entry.OrderTicket.Quantity);
                _plotter.Plot("fill", entry.FillPrice);
                _plotter.Plot("gross", -entry.OrderTicket.Quantity * entry.FillPrice);
                _plotter.Plot("commission", -entry.Commission);
                _plotter.Plot("net", -entry.OrderTicket.Quantity * entry.FillPrice - entry.Commission);
                _plotter.Plot("comment", entry.OrderTicket.Comment ?? "");
            }
        }
        #endregion
        #region public override void Report()
        public override void Report()
        {
            _plotter.OpenWith("SimpleReport");
        }
        #endregion
    }
}

//==============================================================================
// end of file