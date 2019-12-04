''==============================================================================
'' Project:     TuringTrader, Visual Basic demonstration
'' Name:        Program
'' Description: Demo, showing how to use TuringTrader with VisualBasic
'' History:     2019xii03, FUB, created
''------------------------------------------------------------------------------
'' Copyright:   (c) 2011-2019, Bertram Solutions LLC
''              https://www.bertram.solutions
'' License:     This file Is part Of TuringTrader, an open-source backtesting
''              engine/ market simulator.
''              TuringTrader Is free software: you can redistribute it And/Or 
''              modify it under the terms of the GNU Affero General Public 
''              License as published by the Free Software Foundation, either 
''              version 3 of the License, Or (at your option) any later version.
''              TuringTrader Is distributed in the hope that it will be useful,
''              but WITHOUT ANY WARRANTY; without even the implied warranty of
''              MERCHANTABILITY Or FITNESS FOR A PARTICULAR PURPOSE.See the
''              GNU Affero General Public License for more details.
''              You should have received a copy of the GNU Affero General Public
''              License along with TuringTrader. If Not, see 
''              https://www.gnu.org/licenses/agpl-3.0.
''==============================================================================

Imports System
Imports TuringTrader.Simulator
Imports TuringTrader.Indicators
Imports TuringTrader.BooksAndPubs
Imports System.Globalization

Module Program
    Public Class MyVisualBasicAlgo
        Inherits Algorithm
        Private _plotter As Plotter

        Public Overrides ReadOnly Property Name As String
            Get
                Return "TuringTrader Moving Average Crossover Algorithm in VB"
            End Get
        End Property


        Public Overrides Sub Run()

            '===== algorithm initialization

            ' the plotter object is basically a thin wrapper around
            ' creating CSV files. It is optional, and you might want 
            ' a different mechanism to capture the strategy output

            _plotter = New Plotter

            ' set the simulation time frame
            ' this would look much cleaner, if you used your local date format
            StartTime = DateTime.Parse("01/01/2007", CultureInfo.InvariantCulture)
            EndTime = DateTime.Parse("12/01/2019", CultureInfo.InvariantCulture)

            ' set starting capital and commissions
            Deposit(1000000.0)
            CommissionPerShare = 0.015

            ' add data source for "SPY"
            ' it would be simple to add additional data sources for your market
            Dim ds As DataSource
            ds = AddDataSource("SPY")

            '===== event loop

            For Each s In SimTimes
                ' calculate indicators
                Dim sma50 As Double
                sma50 = ds.Instrument.Close.SMA(50).Item(0)
                Dim sma100 As Double
                sma100 = ds.Instrument.Close.SMA(100).Item(0)

                ' trade logic
                If sma50 > sma100 Then
                    Dim ts As Integer
                    ts = Math.Floor(NetAssetValue.Item(0) / ds.Instrument.Close.Item(0))
                    ds.Instrument.Trade(ts - ds.Instrument.Position)
                End If

                ' add data to plotter object, so that we can visualize them later
                _plotter.SelectChart(Name, "Date")
                _plotter.SetX(SimTime.Item(0))
                _plotter.Plot("NAV", NetAssetValue.Item(0))
                _plotter.Plot(ds.Instrument.Name, ds.Instrument.Close.Item(0))
            Next

            '===== post-processing
            ' here, we could do additional things, e.g. create order and 
            ' position logs and either add them to the plotter object, or 
            ' output them in a different manner
        End Sub

        Public Overrides Sub Report()
            ' TuringTrader assumes that a strategy does not visualize its
            ' results immediately, but when this function is called
            ' this makes it simple to not have output during optimization
            Console.WriteLine(Name)
            Console.WriteLine(NetAssetValue.Item(0))
        End Sub
    End Class

    Sub OutputEventHandler(message As String)
        Console.WriteLine(message)
    End Sub
    Sub RegisterOutput()
        ' TODO: any messages printed by the simulator core
        ' are current lost. need to register the event handler
        ' for the Output object here
    End Sub

    Sub RunVisualBasicAlgorithm()
        Dim algo As Algorithm
        algo = New MyVisualBasicAlgo

        algo.Run()
        algo.Report()
    End Sub

    Sub Main(args As String())
        RegisterOutput()
        RunVisualBasicAlgorithm()
    End Sub
End Module

''==============================================================================
'' end of file