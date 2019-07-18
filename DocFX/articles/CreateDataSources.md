# Creating Custom Data Sources

unfortunately, we didn't write this article yet. 

In a nutshell, this is what you need to do:

```c#
using TuringTrader.Simulator;

class MyDataSource : DataSource
{
	// code for your data source here
}

class MayAlgorithm : Algorithm
{
    override public void Run()
    {
    	AddDataSource(new MyDataSource());
    	
    	// code for your algorithm here
    }
}
```

