//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Support/Statistics
// Description: StatisticsSupport support functionality
// History:     2023iv20, FUB, created
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

namespace TuringTrader.SimulatorV2.Support
{
    public static class StatisticsSupport
    {
        #region Student's T-Test
        public static class StudentsT
        {
            // see https://www.youtube.com/watch?v=ijeEYFnS2v4
            #region T-Value
            public static double TTestTValue(
                double mean1, double stdev1, int n1,
                double mean2, double stdev2 = 0.0, int n2 = 1,
                bool twoTailed = true, bool independent = true)
            {
                if (!twoTailed || !independent) throw new NotImplementedException();

                return Math.Abs(mean1 - mean2)
                    / Math.Sqrt(stdev1 * stdev1 / n1 + stdev2 * stdev2 / n2);
            }
            public static double TValue(
                List<double> samples1, List<double> samples2,
                bool twoTailed = true, bool independent = true)
            {
                var n1 = samples1.Count;
                var n2 = samples2.Count;
                var avg1 = samples1.Sum(x => x) / n1;
                var avg2 = samples2.Sum(x => x) / n2;
                var std1 = Math.Sqrt(samples1.Sum(x => Math.Pow(x - avg1, 2.0) / (n1 - 1)));
                var std2 = n2 > 1 ? Math.Sqrt(samples2.Sum(x => Math.Pow(x - avg2, 2.0) / (n2 - 1))) : 0.0;

                return TTestTValue(
                    avg1, std1, n1,
                    avg2, std2, n2,
                    twoTailed, independent);
            }

            public static double TTestTValue(
                List<double> samples1, double mean2,
                bool twoTailed = true, bool independent = true)
                => TValue(samples1, new List<double> { mean2 }, twoTailed, independent);
            #endregion
            #region Critical Value
            #region t distribution critical values
#if false
        // simple, inaccurate, and incomplete table
        private static Dictionary<double, Dictionary<int, double>> _tTable = new Dictionary<double, Dictionary<int, double>>
            {
                {0.05, new Dictionary<int, double>
                    {
                        {1, 12.71},
                        {2, 4.30 },
                        {3, 3.18 },
                        {4, 2.78 },
                        {5, 2.57 },
                        {6, 2.45 },
                        {7, 2.36 },
                        {8, 2.31 },
                        {9, 2.26 },
                        {10, 2.23 },
                        {11, 2.20 },
                        {12, 2.18 },
                        {13, 2.16 },
                        {14, 2.14 },
                        {15, 2.13 },
                        {16, 2.12 },
                        {17, 2.11 },
                        {18, 2.10 },
                        {19, 2.09 },
                        {20, 2.09 },
                        {21, 2.08 },
                        {22, 2.07 },
                        {23, 2.07 },
                        {24, 2.06 },
                        {25, 2.06 },
                        {26, 2.06 },
                        {27, 2.05 },
                        {28, 2.05 },
                        {29, 2.04 },
                        {30, 2.04 },
                        {40, 2.02 },
                        {60, 2.00 },
                        {120, 1.98 },
                        {500, 1.96 }, // infinity
                    }
                },
                {0.025, new Dictionary<int, double>
                    {
                    }
                },
                {0.001, new Dictionary<int, double>
                    {
                    }
                },
            };
        private static double _TTestCriticalValue(double alpha, int df)
        {
            var bestTable = (Dictionary<int, double>)null;
            var bestDelta = 1.0;

            foreach (var table in _tTable)
            {
                var delta = Math.Abs(table.Key - alpha);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    bestTable = table.Value;
                }
            }
            var criticalValue = bestTable[df];

            return criticalValue;
        }
#else
            // see AP Statistics Formula Sheet, table B
            // https://apcentral.collegeboard.org/media/pdf/statistics-formula-sheet-and-tables-2020.pdf
            private static List<double> _tailProbabilityTable = new List<double>
        {
            .25, .20, .15, .10, .05, .025, .02, .01, .005, .0025, .001, .0005
        };
            private static Dictionary<int, List<double>> _criticalValueTable = new Dictionary<int, List<double>>
        {
            {1, new List<double>{ 1.000, 1.376, 1.963, 3.078, 6.314, 12.71, 15.89, 31.82, 63.66, 127.3, 318.3, 636.6}},
            {2, new List<double>{ .816, 1.061, 1.386, 1.886, 2.920, 4.303, 4.849, 6.965, 9.925, 14.09, 22.33, 31.60}},
            {3, new List<double>{ .765, .978, 1.250, 1.638, 2.353, 3.182, 3.482, 4.541, 5.841, 7.453, 10.21, 12.92}},
            {4, new List<double>{ .741, .941, 1.190, 1.533, 2.132, 2.776, 2.999, 3.747, 4.604, 5.598, 7.173, 8.610}},
            {5, new List<double>{ .727, .920, 1.156, 1.476, 2.015, 2.571, 2.757, 3.365, 4.032, 4.773, 5.893, 6.869}},
            {6, new List<double>{ .718, .906, 1.134, 1.440, 1.943, 2.447, 2.612, 3.143, 3.707, 4.317, 5.208, 5.959 }},
            {7, new List<double>{ .711, .896, 1.119, 1.415, 1.895, 2.365, 2.517, 2.998, 3.499, 4.029, 4.785, 5.408 }},
            {8, new List<double>{ .706, .889, 1.108, 1.397, 1.860, 2.306, 2.449, 2.896, 3.355, 3.833, 4.501, 5.041 }},
            {9, new List<double>{ .703, .883, 1.100, 1.383, 1.833, 2.262, 2.398, 2.821, 3.250, 3.690, 4.297, 4.781 }},
            {10, new List<double>{ .700, .879, 1.093, 1.372, 1.812, 2.228, 2.359, 2.764, 3.169, 3.581, 4.144, 4.587 }},
            {11, new List<double>{ .697, .876, 1.088, 1.363, 1.796, 2.201, 2.328, 2.718, 3.106, 3.497, 4.025, 4.437}},
            {12, new List<double>{ .695, .873, 1.083, 1.356, 1.782, 2.179, 2.303, 2.681, 3.055, 3.428, 3.930, 4.318}},
            {13, new List<double>{ .694, .870, 1.079, 1.350, 1.771, 2.160, 2.282, 2.650, 3.012, 3.372, 3.852, 4.221}},
            {14, new List<double>{ .692, .868, 1.076, 1.345, 1.761, 2.145, 2.264, 2.624, 2.977, 3.326, 3.787, 4.140}},
            {15, new List<double>{ .691, .866, 1.074, 1.341, 1.753, 2.131, 2.249, 2.602, 2.947, 3.286, 3.733, 4.073}},
            {16, new List<double>{ .690, .865, 1.071, 1.337, 1.746, 2.120, 2.235, 2.583, 2.921, 3.252, 3.686, 4.015 }},
            {17, new List<double>{ .689, .863, 1.069, 1.333, 1.740, 2.110, 2.224, 2.567, 2.898, 3.222, 3.646, 3.965 }},
            {18, new List<double>{ .688, .862, 1.067, 1.330, 1.734, 2.101, 2.214, 2.552, 2.878, 3.197, 3.611, 3.922 }},
            {19, new List<double>{ .688, .861, 1.066, 1.328, 1.729, 2.093, 2.205, 2.539, 2.861, 3.174, 3.579, 3.883 }},
            {20, new List<double>{ .687, .860, 1.064, 1.325, 1.725, 2.086, 2.197, 2.528, 2.845, 3.153, 3.552, 3.850 }},
            {21, new List<double>{ .686, .859, 1.063, 1.323, 1.721, 2.080, 2.189, 2.518, 2.831, 3.135, 3.527, 3.819}},
            {22, new List<double>{ .686, .858, 1.061, 1.321, 1.717, 2.074, 2.183, 2.508, 2.819, 3.119, 3.505, 3.792}},
            {23, new List<double>{ .685, .858, 1.060, 1.319, 1.714, 2.069, 2.177, 2.500, 2.807, 3.104, 3.485, 3.768}},
            {24, new List<double>{ .685, .857, 1.059, 1.318, 1.711, 2.064, 2.172, 2.492, 2.797, 3.091, 3.467, 3.745}},
            {25, new List<double>{ .684, .856, 1.058, 1.316, 1.708, 2.060, 2.167, 2.485, 2.787, 3.078, 3.450, 3.725}},
            {26, new List<double>{ .684, .856, 1.058, 1.315, 1.706, 2.056, 2.162, 2.479, 2.779, 3.067, 3.435, 3.707 }},
            {27, new List<double>{ .684, .855, 1.057, 1.314, 1.703, 2.052, 2.158, 2.473, 2.771, 3.057, 3.421, 3.690 }},
            {28, new List<double>{ .683, .855, 1.056, 1.313, 1.701, 2.048, 2.154, 2.467, 2.763, 3.047, 3.408, 3.674 }},
            {29, new List<double>{ .683, .854, 1.055, 1.311, 1.699, 2.045, 2.150, 2.462, 2.756, 3.038, 3.396, 3.659 }},
            {30, new List<double>{ .683, .854, 1.055, 1.310, 1.697, 2.042, 2.147, 2.457, 2.750, 3.030, 3.385, 3.646 }},
            {40, new List<double>{ .681, .851, 1.050, 1.303, 1.684, 2.021, 2.123, 2.423, 2.704, 2.971, 3.307, 3.551}},
            {50, new List<double>{ .679, .849, 1.047, 1.299, 1.676, 2.009, 2.109, 2.403, 2.678, 2.937, 3.261, 3.496}},
            {60, new List<double>{ .679, .848, 1.045, 1.296, 1.671, 2.000, 2.099, 2.390, 2.660, 2.915, 3.232, 3.460}},
            {80, new List<double>{ .678, .846, 1.043, 1.292, 1.664, 1.990, 2.088, 2.374, 2.639, 2.887, 3.195, 3.416}},
            {100, new List<double>{ .677, .845, 1.042, 1.290, 1.660, 1.984, 2.081, 2.364, 2.626, 2.871, 3.174, 3.390}},
            {1000, new List<double>{ .675, .842, 1.037, 1.282, 1.646, 1.962, 2.056, 2.330, 2.581, 2.813, 3.098, 3.300}},
            {10000, new List<double>{ .674, .841, 1.036, 1.282, 1.645, 1.960, 2.054, 2.326, 2.576, 2.807, 3.091, 3.291}},
        };

            private static double _TTestCriticalValue(double alpha, int df)
            {
                var alpha2 = 0.5 * alpha; // table is single-tailed

                var tailProbability = _tailProbabilityTable
                    .OrderBy(x => Math.Abs(x - alpha2))
                    .First();
                var tailProbabilityIndex = _tailProbabilityTable.IndexOf(tailProbability);

                var criticalValueRow = _criticalValueTable
                    .OrderBy(x => Math.Abs(x.Key - df))
                    .First().Value;

                return criticalValueRow[tailProbabilityIndex];
            }
#endif
            #endregion

            public static double TTestCriticalValue(
                int n1, int n2 = 1, double alpha = 0.05,
                bool twoTailed = true, bool independent = true)
            {
                if (!twoTailed || !independent) throw new NotImplementedException();

                var df = n1 + n2 - 2;

                return _TTestCriticalValue(alpha, df);
            }

            public static double CriticalValue(
                List<double> samples1, List<double> samples2, double alpha = 0.05,
                bool twoTailed = true, bool independent = true)
                => TTestCriticalValue(samples1.Count, samples2.Count, alpha, twoTailed, independent);

            public static double TTestCriticalValue(
                List<double> samples1, double alpha = 0.05,
                bool twoTailed = true, bool independent = true)
                => TTestCriticalValue(samples1.Count, 1, alpha, twoTailed, independent);
            #endregion
            #region Test H0
            public static bool TTestNullHypothesis(
                double mean1, double stdev1, int n1,
                double mean2, double stdev2 = 0.0, int n2 = 1,
                double alpha = 0.05,
                bool twoTailed = true, bool independent = true)
            {
                var tValue = TTestTValue(
                    mean1, stdev1, n1,
                    mean2, stdev2, n2,
                    twoTailed, independent);

                var criticalValue = TTestCriticalValue(
                    n1, n2, alpha,
                    twoTailed, independent);

                return Math.Abs(tValue) < Math.Abs(criticalValue);
            }

            public static bool TestH0(
                List<double> samples1, List<double> samples2,
                double alpha = 0.05,
                bool twoTailed = true, bool independent = true)
            {
                var n1 = samples1.Count;
                var n2 = samples2.Count;
                var avg1 = samples1.Sum(x => x) / n1;
                var avg2 = samples2.Sum(x => x) / n2;
                var std1 = Math.Sqrt(samples1.Sum(x => Math.Pow(x - avg1, 2.0) / (n1 - 1)));
                var std2 = Math.Sqrt(samples2.Sum(x => Math.Pow(x - avg2, 2.0) / (n2 - 1)));

                return TTestNullHypothesis(
                    avg1, std1, n1,
                    avg2, std2, n2,
                    alpha,
                    twoTailed, independent);
            }

            public static bool TTestH0(
                List<double> samples1, double mean2,
                double alpha = 0.05,
                bool twoTailed = true, bool independent = true)
                => TestH0(samples1, new List<double> { mean2 }, alpha, twoTailed, independent);
            #endregion
        }
        #endregion
    }
}

//==============================================================================
// end of file
