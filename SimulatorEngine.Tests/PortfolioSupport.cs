//==============================================================================
// Project:     TuringTrader: SimulatorEngine.Tests
// Name:        PortfolioSupport
// Description: unit test for portfolio support class
// History:     2019ii03, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TuringTrader.Simulator;
#endregion

/*
X1,X2,X3,X4,X5,X6,X7,X8,X9,X10
1.175,1.19,0.396,1.12,0.346,0.679,0.089,0.73,0.481,1.08
0,0,0,0,0,0,0,0,0,0
1,1,1,1,1,1,1,1,1,1
0.40755159,0.03175842,0.05183923,0.05663904,0.0330226,0.00827775,0.02165938,0.01332419,0.0343476,0.02249903
0.03175842,0.9063047,0.03136385,0.02687256,0.01917172,0.00934384,0.02495043,0.00761036,0.02874874,0.01336866
0.05183923,0.03136385,0.19490901,0.04408485,0.03006772,0.01322738,0.03525971,0.0115493,0.0427563,0.02057303
0.05663904,0.02687256,0.04408485,0.19528471,0.02777345,0.00526665,0.01375808,0.00780878,0.02914176,0.01640377
0.0330226,0.01917172,0.03006772,0.02777345,0.34059105,0.00777055,0.02067844,0.00736409,0.02542657,0.01284075
0.00827775,0.00934384,0.01322738,0.00526665,0.00777055,0.15983874,0.02105575,0.00518686,0.01723737,0.00723779
0.02165938,0.02495043,0.03525971,0.01375808,0.02067844,0.02105575,0.68056711,0.01377882,0.04627027,0.01926088
0.01332419,0.00761036,0.0115493,0.00780878,0.00736409,0.00518686,0.01377882,0.95526918,0.0106553,0.00760955
0.0343476,0.02874874,0.0427563,0.02914176,0.02542657,0.01723737,0.04627027,0.0106553,0.31681584,0.01854318
0.02249903,0.01336866,0.02057303,0.01640377,0.01284075,0.00723779,0.01926088,0.00760955,0.01854318,0.11079287
*/
/*
#---------------------------------------------------------------
def plot2D(x,y,xLabel='',yLabel='',title='',pathChart=None):
    import matplotlib.pyplot as mpl
    fig=mpl.figure()
    ax=fig.add_subplot(1,1,1) #one row, one column, first plot
    ax.plot(x,y,color='blue')
    ax.set_xlabel(xLabel)
    ax.set_ylabel(yLabel,rotation=90)
    mpl.xticks(rotation='vertical')
    mpl.title(title)
    if pathChart==None:
        mpl.show()
    else:
        mpl.savefig(pathChart)
    mpl.clf() # reset pylab
    return
#---------------------------------------------------------------
def main():
    import numpy as np
    import CLA
    #1) Path
    path='H:/PROJECTS/Data/CLA_Data.csv'
    #2) Load data, set seed
    headers=open(path,'r').readline()[:-1].split(',')
    data=np.genfromtxt(path,delimiter=',',skip_header=1) # load as numpy array
    mean=np.array(data[:1]).T
    lB=np.array(data[1:2]).T
    uB=np.array(data[2:3]).T
    covar=np.array(data[3:])
    #3) Invoke object
    cla=CLA.CLA(mean,covar,lB,uB)
    cla.solve()
    print cla.w # print all turning points
    #4) Plot frontier
    mu,sigma,weights=cla.efFrontier(100)
    plot2D(sigma,mu,'Risk','Expected Excess Return','CLA-derived Efficient Frontier')
    #5) Get Maximum Sharpe ratio portfolio
    sr,w_sr=cla.getMaxSR()
    print np.dot(np.dot(w_sr.T,cla.covar),w_sr)[0,0]**.5,sr
    print w_sr
    #6) Get Minimum Variance portfolio
    mv,w_mv=cla.getMinVar()
    print mv
    print w_mv
    return
#---------------------------------------------------------------
# Boilerplate
if __name__=='__main__':main()
*/

namespace SimulatorEngine.Tests
{
    [TestClass]
    public class PortfolioSupport
    {
        [TestMethod]
        public void Test_MarkowitzCLA()
        {
            // the instruments serves no function, other than as a key
            List<Instrument> instruments = Enumerable.Range(0, 10)
                .Select(i =>
                {
                    Dictionary<DataSourceValue, string> info = new Dictionary<DataSourceValue, string>
                    {
                        { DataSourceValue.name, string.Format("X{0}", i) },
                        { DataSourceValue.nickName, string.Format("X{0}", i) },
                    };
                    var dataSource = new DataSourceFromBars(null, info);

                    return new Instrument(null, dataSource);
                })
                .ToList();

            Dictionary<string, double> mean = new Dictionary<string, double>
            {
                { "X0", 1.175 },
                { "X1", 1.19  },
                { "X2", 0.396 },
                { "X3", 1.12  },
                { "X4", 0.346 },
                { "X5", 0.679 },
                { "X6", 0.089 },
                { "X7", 0.73  },
                { "X8", 0.481 },
                { "X9", 1.08  },
            };

            Dictionary<string, Dictionary<string, double>> covariance = new Dictionary<string, Dictionary<string, double>>
            {
                { "X0", new Dictionary<string, double> { { "X0", 0.40755159 }, { "X1", 0.03175842 }, { "X2", 0.05183923 }, { "X3", 0.05663904 }, { "X4", 0.0330226  }, { "X5", 0.00827775 }, { "X6", 0.02165938 }, { "X7", 0.01332419 }, { "X8", 0.0343476  }, { "X9", 0.02249903 }, } },
                { "X1", new Dictionary<string, double> { { "X0", 0.03175842 }, { "X1", 0.9063047  }, { "X2", 0.03136385 }, { "X3", 0.02687256 }, { "X4", 0.01917172 }, { "X5", 0.00934384 }, { "X6", 0.02495043 }, { "X7", 0.00761036 }, { "X8", 0.02874874 }, { "X9", 0.01336866 }, } },
                { "X2", new Dictionary<string, double> { { "X0", 0.05183923 }, { "X1", 0.03136385 }, { "X2", 0.19490901 }, { "X3", 0.04408485 }, { "X4", 0.03006772 }, { "X5", 0.01322738 }, { "X6", 0.03525971 }, { "X7", 0.0115493  }, { "X8", 0.0427563  }, { "X9", 0.02057303 }, } },
                { "X3", new Dictionary<string, double> { { "X0", 0.05663904 }, { "X1", 0.02687256 }, { "X2", 0.04408485 }, { "X3", 0.19528471 }, { "X4", 0.02777345 }, { "X5", 0.00526665 }, { "X6", 0.01375808 }, { "X7", 0.00780878 }, { "X8", 0.02914176 }, { "X9", 0.01640377 }, } },
                { "X4", new Dictionary<string, double> { { "X0", 0.0330226  }, { "X1", 0.01917172 }, { "X2", 0.03006772 }, { "X3", 0.02777345 }, { "X4", 0.34059105 }, { "X5", 0.00777055 }, { "X6", 0.02067844 }, { "X7", 0.00736409 }, { "X8", 0.02542657 }, { "X9", 0.01284075 }, } },
                { "X5", new Dictionary<string, double> { { "X0", 0.00827775 }, { "X1", 0.00934384 }, { "X2", 0.01322738 }, { "X3", 0.00526665 }, { "X4", 0.00777055 }, { "X5", 0.15983874 }, { "X6", 0.02105575 }, { "X7", 0.00518686 }, { "X8", 0.01723737 }, { "X9", 0.00723779 }, } },
                { "X6", new Dictionary<string, double> { { "X0", 0.02165938 }, { "X1", 0.02495043 }, { "X2", 0.03525971 }, { "X3", 0.01375808 }, { "X4", 0.02067844 }, { "X5", 0.02105575 }, { "X6", 0.68056711 }, { "X7", 0.01377882 }, { "X8", 0.04627027 }, { "X9", 0.01926088 }, } },
                { "X7", new Dictionary<string, double> { { "X0", 0.01332419 }, { "X1", 0.00761036 }, { "X2", 0.0115493  }, { "X3", 0.00780878 }, { "X4", 0.00736409 }, { "X5", 0.00518686 }, { "X6", 0.01377882 }, { "X7", 0.95526918 }, { "X8", 0.0106553  }, { "X9", 0.00760955 }, } },
                { "X8", new Dictionary<string, double> { { "X0", 0.0343476  }, { "X1", 0.02874874 }, { "X2", 0.0427563  }, { "X3", 0.02914176 }, { "X4", 0.02542657 }, { "X5", 0.01723737 }, { "X6", 0.04627027 }, { "X7", 0.0106553  }, { "X8", 0.31681584 }, { "X9", 0.01854318 }, } },
                { "X9", new Dictionary<string, double> { { "X0", 0.02249903 }, { "X1", 0.01336866 }, { "X2", 0.02057303 }, { "X3", 0.01640377 }, { "X4", 0.01284075 }, { "X5", 0.00723779 }, { "X6", 0.01926088 }, { "X7", 0.00760955 }, { "X8", 0.01854318 }, { "X9", 0.11079287 }, } },
            };

            Dictionary<string, double> lowerBound = instruments.Select(i => i.Nickname)
                .ToDictionary(nick => nick, nick => 0.0);

            Dictionary<string, double> upperBound = instruments.Select(i => i.Nickname)
                .ToDictionary(nick => nick, nick => 1.0);

            var cla = TuringTrader.Simulator.PortfolioSupport.MarkowitzCLA(
                instruments,
                i => mean[i.Nickname],
                (i, j) => covariance[i.Nickname][j.Nickname],
                i => lowerBound[i.Nickname],
                i => upperBound[i.Nickname]);

            List<Dictionary<string, double>> expectedTurningPoints = new List<Dictionary<string, double>>
            {
                new Dictionary<string, double> { { "X0", 0.00000000e+00 }, { "X1", 1.00000000e+00 }, { "X2", 0.00000000e+00 }, { "X3", 0.00000000e+00  }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.00000000e+00  }, { "X8", 0.00000000e+00 }, { "X9", 0.00000000e+00  } },
                new Dictionary<string, double> { { "X0", 0.00000000e+00 }, { "X1", 1.00000000e+00 }, { "X2", 0.00000000e+00 }, { "X3", 0.00000000e+00  }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.00000000e+00  }, { "X8", 0.00000000e+00 }, { "X9", 0.00000000e+00  } },
                new Dictionary<string, double> { { "X0", 6.49369407e-01 }, { "X1", 3.50630593e-01 }, { "X2", 0.00000000e+00 }, { "X3", -3.55271368e-15 }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.00000000e+00  }, { "X8", 0.00000000e+00 }, { "X9", 0.00000000e+00  } },
                new Dictionary<string, double> { { "X0", 4.33984134e-01 }, { "X1", 2.31247501e-01 }, { "X2", 0.00000000e+00 }, { "X3", 3.34768365e-01  }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.00000000e+00  }, { "X8", 0.00000000e+00 }, { "X9", -3.55271368e-15 } },
                new Dictionary<string, double> { { "X0", 1.26887854e-01 }, { "X1", 7.23433472e-02 }, { "X2", 0.00000000e+00 }, { "X3", 2.81253749e-01  }, { "X4", 0.00000000e+00  }, { "X5", 0.00000000e+00 }, { "X6", 0.00000000e+00 }, { "X7", -1.38777878e-17 }, { "X8", 0.00000000e+00 }, { "X9", 5.19515050e-01  } },
                new Dictionary<string, double> { { "X0", 1.23201004e-01 }, { "X1", 7.04440713e-02 }, { "X2", 0.00000000e+00 }, { "X3", 2.78993567e-01  }, { "X4", 0.00000000e+00  }, { "X5", 1.11022302e-16 }, { "X6", 0.00000000e+00 }, { "X7", 6.43556436e-03  }, { "X8", 0.00000000e+00 }, { "X9", 5.20925793e-01  } },
                new Dictionary<string, double> { { "X0", 8.69215493e-02 }, { "X1", 5.04510423e-02 }, { "X2", 0.00000000e+00 }, { "X3", 2.23594017e-01  }, { "X4", 0.00000000e+00  }, { "X5", 1.73831615e-01 }, { "X6", 0.00000000e+00 }, { "X7", 3.01730156e-02  }, { "X8", 6.93889390e-18 }, { "X9", 4.35028760e-01  } },
                new Dictionary<string, double> { { "X0", 8.46709412e-02 }, { "X1", 4.92538587e-02 }, { "X2", 0.00000000e+00 }, { "X3", 2.19633903e-01  }, { "X4", -1.73472348e-18 }, { "X5", 1.80039235e-01 }, { "X6", 0.00000000e+00 }, { "X7", 3.10298019e-02  }, { "X8", 6.48570242e-03 }, { "X9", 4.28886558e-01  } },
                new Dictionary<string, double> { { "X0", 7.37892530e-02 }, { "X1", 4.38286608e-02 }, { "X2", 3.46944695e-18 }, { "X3", 1.98975608e-01  }, { "X4", 2.61581599e-02  }, { "X5", 1.98151872e-01 }, { "X6", 0.00000000e+00 }, { "X7", 3.34195864e-02  }, { "X8", 2.79029660e-02 }, { "X9", 3.97773894e-01  } },
                new Dictionary<string, double> { { "X0", 0.06834400e+00 }, { "X1", 0.04138703e+00 }, { "X2", 0.01521526e+00 }, { "X3", 0.18813443e+00  }, { "X4", 0.03416249e+00  }, { "X5", 0.20231943e+00 }, { "X6", 0.00000000e+00 }, { "X7", 0.03392932e+00  }, { "X8", 0.03363265e+00 }, { "X9", 0.38287539e+00  } },
                new Dictionary<string, double> { { "X0", 0.03696858e+00 }, { "X1", 0.02690084e+00 }, { "X2", 0.09494243e+00 }, { "X3", 0.12577595e+00  }, { "X4", 0.07674608e+00  }, { "X5", 0.21935567e+00 }, { "X6", 0.02998710e+00 }, { "X7", 0.03596328e+00  }, { "X8", 0.06134984e+00 }, { "X9", 0.29201023e+00  } }
            };
        }
    }
}

//==============================================================================
// end of file