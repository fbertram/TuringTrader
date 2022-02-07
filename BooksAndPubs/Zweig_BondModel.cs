
// see https://www.cmgwealth.com/how-to-track-the-zweig-bond-model/
// There are five steps to the scoring process. Here is how the tactical trend following model works:
// (1) Score a +1 when the Dow Jones 20 Bond Price Index (index symbol $DJCBP)
//     rises from a bottom price low by 0.6%.  Score a -1 when the index falls
//     from a peak price by 0.6%.
// (2) Score a +1 when the Dow Jones 20 Bond Price Index rises from a bottom
//     price by 1.8%.  Score a -1 when the index falls from a peak price by 1.8%.
// (3) Score a +1 when the Dow Jones 20 Bond Price Index crosses above its
//     50-day moving average by 1%.  Score a -1 when the index crosses
//     below its 50-day MA by 1%.
// (4) Score a +1 when the Fed Funds Target Rate drops by at least ½ point.
//     Score a -1 when the rate rises by at least ½ point. Score +1 if a buy and -1 if a sell.
// (5) Score a +1 when the yield difference of the Moody’s AAA Corporate Bond
//     Yield minus the yield on 90-day Commercial Paper Yield crosses above 0.6.
//     Score a -1 when the yield difference falls below -0.2.  Score it 0 for
//     a neutral score between -0.2 and 0.6.
//
// Next, sum the scores of steps 1 through 5 once a week (the chart below reflects Friday’s close calculations).
// 
// If the total is +1 or higher, we would suggest an investment in a total
// bond market ETF such as TLT (iShares 20+ Year Treasury Bond ETF), EDV
// (Vanguard Extended Duration Treasury ETF), BND (Vanguard Total Bond Market
// ETF) or AGG (iShares Barclays Aggregate Bond Index ETF). The longer duration
// ETFs like TLT and BND will have more gains or losses on interest rate moves.
// If the aggregate score is -1 or lower, we would suggest buying BIL (SPDR
// Lehman 1-3 Month T Bill ETF) or MINT(PIMCO’s Enhanced Short Maturity ETF).
// 
// The chart reflects the annual gain per annum versus a buy-and-hold gain
// (See this weeks Trade Signals for the most recent chart).
// 
// The process was developed in the mid-1980s and remains the same since
// its inception.  The data goes back to 1967 with the Barclays Aggregate
// Bond Index to 1976 and the Ibbotson Long-Term U.S. Bond Index from 1967
// to 1976.  I intend to post this chart each Wednesday in Trade Signals.
// Over that stretch of time, the model has done a good job at enhancing
// return and reducing risk in rising rate environments (refer to the
// chart – highlighted orange rectangle). 
//
// The bottom section of the chart shows the combined score of the model’s five measurements.  A buy is generated on scores > 0 and a sell on scores < 0.  The model remains in a buy signal.  ETFs such as BND can be used to express the view. 
// 
// charts: https://www.cmgwealth.com/ri/trade-signals-zweig-bond-model-buy-signal/
//
// - - - - - - - - - -
// see https://extradash.com/en/strategies/models/19/zweig-bond-timing-model/
// 
// - - - - - - - - - -
// see https://www.forbes.com/sites/greatspeculations/2014/07/30/zweig-bond-model-remains-bullish/?sh=54edd0da3393
//
//

// USE_FULL_RANGE: if defined, start in 1967
#define USE_FULL_RANGE

// NED_DAVIS_MOD: if defined, implement modification according to Ned Davis 2014
#define NED_DAVIS_MOD

#region libraries
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TuringTrader.Algorithms.Glue;
using TuringTrader.BooksAndPubs;
using TuringTrader.Indicators;
using TuringTrader.Simulator;
using TuringTrader.Support;
#endregion

namespace Algorithms.TTorg
{
    public class Zweig_BondModel : AlgorithmPlusGlue
    {
        public override string Name => "Zweig's Bond Trading Model";

        #region inputs
        //public virtual object ASSET { get; set; } = "$SPUSAGGT"; // Zweig uses Dow Jones 20 Bond Average
        //public virtual object ASSET { get; set; } = Assets.IEF; // Zweig uses Dow Jones 20 Bond Average
        public virtual object ASSET { get; set; } = Assets.IEF; // Zweig uses Dow Jones 20 Bond Average
        public virtual object SAFE { get; set; } = Assets.SHY; // Zweig assumes a money-market fund here
        public virtual object DISCOUNT_RATE { get; set; } = "%FFYE"; // Zweig uses Discount Rate
        public virtual object ST_RATE { get; set; } = "%FFYE"; // Zweig uses 90-day Commercial Paper Rate
        //public virtual object LT_RATE { get; set; } = "fred:DGS20"; // Zweig uses Moody's AAA Corporate Bond Rate
        public virtual object LT_RATE { get; set; } = "fred:AAA"; // Zweig uses Moody's AAA Corporate Bond Rate

        //public virtual int TAPE_IND_PER { get; set; } = 252;
        [OptimizerParam(10, 100, 10)]
        public virtual int TAPE_IND_A_BPTS { get; set; } = 60; // 0.6%
        [OptimizerParam(100, 1000, 25)]
        public virtual int TAPE_IND_B_BPTS { get; set; } = 180; // 1.8%
        [OptimizerParam(20, 100, 10)]
        public virtual int RATE_IND_BPTS { get; set; } = 50; // 0.5%

        [OptimizerParam(20, 100, 10)]
        public virtual int CURVE_IND_BULL_BPTS { get; set; } = 60; // 0.6%
        [OptimizerParam(-50, -10, 10)]
        public virtual int CURVE_IND_BEAR_BPTS { get; set; } = -20; // -0.2%
        [OptimizerParam(1, 3, 1)]
        public virtual int BUY_SELL_THRS { get; set; } = 3;
        protected virtual bool IsTradingDay
            => SimTime[0].DayOfWeek <= DayOfWeek.Wednesday && NextSimTime.DayOfWeek > DayOfWeek.Wednesday;
        #endregion

        public override IEnumerable<Bar> Run(DateTime? startTime, DateTime? endTime)
        {
#if USE_FULL_RANGE
            StartTime = startTime ?? DateTime.Parse("01/01/1965", CultureInfo.InvariantCulture);
            WarmupStartTime = StartTime - TimeSpan.FromDays(180);
            //EndTime = DateTime.Parse("12/31/1986", CultureInfo.InvariantCulture);
            EndTime = endTime ?? DateTime.Now.Date - TimeSpan.FromDays(5);
#else
            WarmupStartTime = Globals.WARMUP_START_TIME;
            StartTime = Globals.START_TIME;
            EndTime = Globals.END_TIME;
#endif

            Deposit(Globals.INITIAL_CAPITAL);
            CommissionPerShare = Globals.COMMISSION; // Zweig is not considering commissions

            // tape indicators
            var asset = AddDataSource(ASSET);
            var tapeIndicatorABuy = false;
            var tapeIndicatorARef = (double?)null;
            var tapeIndicatorBBuy = false;
            var tapeIndicatorBRef = (double?)null;

            // additional trend indicator according to Ned Davis 2014
            var trendIndicatorBuy = false;

            // discount rate indicator
            var fedRate = AddDataSource(DISCOUNT_RATE); // Zweig uses Fed's discount rate
            var rateIndicatorBuy = false;
            var rateIndicatorRef = (double?)null;

            // yield curve indicator
            var stRate = AddDataSource(ST_RATE);
            var ltRate = AddDataSource(LT_RATE);
            var curveIndicatorBuy = false;
            var curveIndicatorSell = false;


            // aggregate buy/ sell signal
            var safe = SAFE != null ? AddDataSource(SAFE) : null;
            var aggregateBuy = false;

            foreach (var simTime in SimTimes)
            {
                if (!HasInstruments(new List<DataSource> { asset, fedRate, stRate, ltRate }))
                    continue;

                if (safe != null && !HasInstrument(safe))
                    continue;

                var assetSma50 = asset.Instrument.Close.EMA(50);
                var filteredRate = fedRate.Instrument.Close.EMA(10).EMA(10); // Zweig does not mention filter
                var yieldCurve = ltRate.Instrument.Close.Subtract(stRate.Instrument.Close).EMA(10).EMA(10); // Zweig does not mention filter

                if (IsTradingDay)
                {
                    #region Tape Indicator A
                    if (tapeIndicatorARef == null)
                        tapeIndicatorARef = asset.Instrument.Close[0];

                    if (tapeIndicatorABuy)
                    {
                        // sell after 0.6% decline
                        if (asset.Instrument.Close[0] <= tapeIndicatorARef * (1.0 - TAPE_IND_A_BPTS / 10000.0))
                        {
                            tapeIndicatorABuy = false;
                            tapeIndicatorARef = asset.Instrument.Close[0];
                        }
                        else
                        {
                            tapeIndicatorARef = Math.Max((double)tapeIndicatorARef, asset.Instrument.Close[0]);
                        }
                    }
                    else
                    {
                        // buy after 0.6% rise
                        if (asset.Instrument.Close[0] >= tapeIndicatorARef * (1.0 + TAPE_IND_A_BPTS / 10000.0))
                        {
                            tapeIndicatorABuy = true;
                            tapeIndicatorARef = asset.Instrument.Close[0];
                        }
                        else
                        {
                            tapeIndicatorARef = Math.Min((double)tapeIndicatorARef, asset.Instrument.Close[0]);
                        }
                    }
                    #endregion
                    #region Tape Indicator B
                    if (tapeIndicatorBRef == null)
                        tapeIndicatorBRef = asset.Instrument.Close[0];

                    if (tapeIndicatorBBuy)
                    {
                        // sell after 1.8% decline
                        if (asset.Instrument.Close[0] <= tapeIndicatorBRef * (1.0 - TAPE_IND_B_BPTS / 10000.0))
                        {
                            tapeIndicatorBBuy = false;
                            tapeIndicatorBRef = asset.Instrument.Close[0];
                        }
                        else
                        {
                            tapeIndicatorBRef = Math.Max((double)tapeIndicatorBRef, asset.Instrument.Close[0]);
                        }
                    }
                    else
                    {
                        // buy after 1.8% rise
                        if (asset.Instrument.Close[0] >= tapeIndicatorBRef * (1.0 + TAPE_IND_B_BPTS / 10000.0))
                        {
                            tapeIndicatorBBuy = true;
                            tapeIndicatorBRef = asset.Instrument.Close[0];
                        }
                        else
                        {
                            tapeIndicatorBRef = Math.Min((double)tapeIndicatorBRef, asset.Instrument.Close[0]);
                        }
                    }
                    #endregion
                    #region Trend Indicator (Ned Davis 2014)
                    if (trendIndicatorBuy)
                    {
                        if (asset.Instrument.Close[0] < 0.99 * assetSma50[0])
                            trendIndicatorBuy = false;
                    }
                    else
                    {
                        if (asset.Instrument.Close[0] > 1.01 * assetSma50[0])
                            trendIndicatorBuy = true;
                    }
                    #endregion
                    #region Discount Rate Indicator
                    if (rateIndicatorRef == null)
                        rateIndicatorRef = filteredRate[0];

                    if (rateIndicatorBuy)
                    {
                        // sell after 0.5% rate increase
                        if (filteredRate[0] >= rateIndicatorRef + RATE_IND_BPTS / 100.0)
                        {
                            rateIndicatorBuy = false;
                            tapeIndicatorBRef = filteredRate[0];
                        }
                        else
                        {
                            rateIndicatorRef = Math.Min((double)rateIndicatorRef, filteredRate[0]);
                        }
                    }
                    else
                    {
                        // buy after 0.5% rate decrease
                        if (filteredRate[0] <= rateIndicatorRef - RATE_IND_BPTS / 100.0)
                        {
                            rateIndicatorBuy = true;
                            rateIndicatorRef = filteredRate[0];
                        }
                        else
                        {
                            rateIndicatorRef = Math.Max((double)rateIndicatorRef, filteredRate[0]);
                        }
                    }
                    #endregion
                    #region Yield Curve Indicator
                    curveIndicatorBuy = yieldCurve[0] >= CURVE_IND_BULL_BPTS / 100.0;
                    curveIndicatorSell = yieldCurve[0] <= CURVE_IND_BEAR_BPTS / 100.0; 
                    #endregion
                }

                //----- putting things together
                var combinedScore = (tapeIndicatorABuy ? 1 : -1)
                    + (tapeIndicatorBBuy ? 1 : -1)
#if NED_DAVIS_MOD
                    + (trendIndicatorBuy ? 1 : -1)
#endif
                    + (rateIndicatorBuy ? 1 : -1)
                    + (curveIndicatorBuy ? 1 : 0) + (curveIndicatorSell ? 0 : -1);

#if true
                if (aggregateBuy)
                {
                    // sell, if score reaches -3
                    if (combinedScore <= -BUY_SELL_THRS)
                        aggregateBuy = false;
                }
                else
                {
                    // buy, if score reaches +3
                    if (combinedScore >= BUY_SELL_THRS)
                        aggregateBuy = true;
                }
#endif

                var assetWeight = aggregateBuy ? 1.0 : 0.0;
                var assetShares = (int)Math.Floor(assetWeight * NetAssetValue[0] / asset.Instrument.Close[0]);
                asset.Instrument.Trade(assetShares - asset.Instrument.Position);

                if (safe != null)
                {
                    var safeWeight = 1.0 - assetWeight;
                    var safeShares = (int)Math.Floor(safeWeight * NetAssetValue[0] / safe.Instrument.Close[0]);
                    safe.Instrument.Trade(safeShares - safe.Instrument.Position);
                }

                var p = 10.0 * NetAssetValue[0] / Globals.INITIAL_CAPITAL;
                yield return Bar.NewOHLC(
                    this.GetType().Name, SimTime[0],
                    p, p, p, p, 0);

                if (TradingDays > 0.0 && !IsOptimizing)
                {
                    _plotter.AddNavAndBenchmark(this, asset.Instrument);
                    _plotter.AddStrategyHoldings(this, safe != null ? new List<Instrument> { asset.Instrument, safe.Instrument } : new List<Instrument> { asset.Instrument });

                    _plotter.SelectChart("Tape Indicator A", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Log Price", Math.Log(asset.Instrument.Close[0]));
                    _plotter.Plot("Buy Signal", tapeIndicatorABuy ? 1.0 : 0.0);

                    _plotter.SelectChart("Tape Indicator B", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Log Price", Math.Log(asset.Instrument.Close[0]));
                    _plotter.Plot("Buy Signal", tapeIndicatorBBuy ? 1.0 : 0.0);

                    _plotter.SelectChart("Trend Indicator", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Log Price", Math.Log(asset.Instrument.Close[0]));
                    _plotter.Plot("Log Price - SMA", Math.Log(assetSma50[0]));
                    _plotter.Plot("Buy Signal", trendIndicatorBuy ? 1.0 : 0.0);

                    _plotter.SelectChart("Discount Rate Indicator", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Interest Rate", filteredRate[0]);
                    //_plotter.Plot("Interest Rate Ref", (double)rateIndicatorRef);
                    _plotter.Plot("Buy Signal", rateIndicatorBuy ? 3.0 : 0.0);

                    _plotter.SelectChart("Yield Curve Indicator", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Yield Spread", yieldCurve[0]);
                    _plotter.Plot("Buy Signal", (curveIndicatorBuy ? 1.0 : 0.0) + (curveIndicatorSell ? -1.0 : 0.0));

                    _plotter.SelectChart("Combine Indicators", "Date");
                    _plotter.SetX(SimTime[0]);
                    _plotter.Plot("Tape A", (tapeIndicatorABuy ? 1.0 : 0.0) + 14.0);
                    _plotter.Plot("Tape B", (tapeIndicatorBBuy ? 1.0 : 0.0) + 12.0);
                    _plotter.Plot("Trend Indicator", (trendIndicatorBuy ? 1.0 : 0.0) + 10.0);
                    _plotter.Plot("Discount Rate", (rateIndicatorBuy ? 1.0 : 0.0) + 8.0);
                    _plotter.Plot("Yield Curve", (curveIndicatorBuy ? 0.5 : 0.0) + (curveIndicatorSell ? 0.0 : 0.5) + 6.0);
                    _plotter.Plot("Total Score", combinedScore);
                    _plotter.Plot("Total Buy", aggregateBuy ? 1.0 : 0.0);
                }
            }

            //========== post processing ==========

            if (!IsOptimizing)
            {
                _plotter.AddAverageHoldings(this);
                _plotter.AddTargetAllocation(Alloc);
                _plotter.AddOrderLog(this);
                _plotter.AddPositionLog(this);
                _plotter.AddPnLHoldTime(this);
                _plotter.AddMfeMae(this);
                _plotter.AddParameters(this);
            }

            FitnessValue = this.CalcFitness();

        }
    }

    public class Zweig_Bond_Model_LQD : Zweig_BondModel
    {
        public override string Name => base.Name + " (LQD)";
        public override object ASSET { get; set; } = Assets.LQD;
        public override object SAFE { get; set; } = Assets.IEI;
        public override int TAPE_IND_A_BPTS { get; set; } = 100;
        public override int TAPE_IND_B_BPTS { get; set; } = 425;
        public override int RATE_IND_BPTS { get; set; } = 30;
        public override int CURVE_IND_BULL_BPTS { get; set; } = 60;
        public override int CURVE_IND_BEAR_BPTS { get; set; } = -30;
        public override int BUY_SELL_THRS { get; set; } = 2;
    }

    public class Zweig_Bond_Model_JNK : Zweig_BondModel
    {
        public override string Name => base.Name + " (JNK)";
        public override object ASSET { get; set; } = Assets.JNK;
        public override object SAFE { get; set; } = Assets.IEI;
        public override int TAPE_IND_A_BPTS { get; set; } = 50;
        public override int TAPE_IND_B_BPTS { get; set; } = 100;
        public override int RATE_IND_BPTS { get; set; } = 100;
        public override int CURVE_IND_BULL_BPTS { get; set; } = 30;
        public override int CURVE_IND_BEAR_BPTS { get; set; } = -50;
        public override int BUY_SELL_THRS { get; set; } = 2;
    }

    public class Zweig_Bond_Model_TLT : Zweig_BondModel
    {
        public override string Name => base.Name + " (TLT)";
        public override object ASSET { get; set; } = Assets.TLT;
        public override object SAFE { get; set; } = Assets.BIL;
        public override int TAPE_IND_A_BPTS { get; set; } = 100;
        public override int TAPE_IND_B_BPTS { get; set; } = 925;
        public override int RATE_IND_BPTS { get; set; } = 70;
        public override int CURVE_IND_BULL_BPTS { get; set; } = 40;
        public override int CURVE_IND_BEAR_BPTS { get; set; } = -10;
        public override int BUY_SELL_THRS { get; set; } = 2;
    }

    public class Zweig_Bond_Model_IEF : Zweig_BondModel
    {
        public override string Name => base.Name + " (IEF)";
        public override object ASSET { get; set; } = Assets.IEF;
        public override object SAFE { get; set; } = Assets.SHY;
        public override int TAPE_IND_A_BPTS { get; set; } = 100;
        public override int TAPE_IND_B_BPTS { get; set; } = 975;
        public override int RATE_IND_BPTS { get; set; } = 70;
        public override int CURVE_IND_BULL_BPTS { get; set; } = 20;
        public override int CURVE_IND_BEAR_BPTS { get; set; } = -10;
        public override int BUY_SELL_THRS { get; set; } = 2;
    }

    public class Zweig_Meta : LazyPortfolio
    {
        public override string Name => "Zweig Meta-Portfolio";
        public override HashSet<Tuple<object, double>> ALLOCATION => new HashSet<Tuple<object, double>>
        {
            new Tuple<object, double>(new Zweig_Bond_Model_TLT(), 0.25),
            new Tuple<object, double>(new Zweig_Bond_Model_IEF(), 0.25),
            new Tuple<object, double>(new Zweig_Bond_Model_LQD(), 0.25),
            new Tuple<object, double>(new Zweig_Bond_Model_JNK(), 0.25),
        };
        //public override string BENCH => (string)Assets.AGG;

        public override string BENCH => "algo:TTcom_BondsNot";
        public override DateTime START_TIME => DateTime.Parse("01/01/1950");
    }
}
