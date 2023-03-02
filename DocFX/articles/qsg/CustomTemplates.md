# Creating Custom Templates

TuringTrader can create beautiful and fully-customizable reports, based on user-defined templates. Depending on your application, you can render reports in the following formats:
* C#
* Microsoft Excel
* R Markdown
* R

In this topic, we describe how these templates work.

## C#

Unfortunately, we didn't write this section yet. Basically, TuringTrader provides a window with an OxyPlot.PlotView for charts, and a DataGrid for tables. Check out the sample template [here](https://github.com/fbertram/TuringTrader/blob/develop/Templates/SimpleChart.cs)

## Microsoft Excel

Unfortunately, we didn't write this section yet. Basically an Excel template exposes a macro with the following signature:

```vb
Sub UPDATE_LOGGER(ByVal PathToCsv As String, Optional ByVal numPlots = 1, Optional ByVal plotIndex = 0, Optional ByVal plotTitle = "Simple Chart")
    ' your code here
EndSub
```

TuringTrader will pass all charts from a plot object sequentially to this macro.

## R Markdown

Unfortunately, we didn't write this section yet.

## R

Unfortunately, we didn't write this section yet. TuringTrader will launch `rscript.exe` and pass one CSV per plotter chart.