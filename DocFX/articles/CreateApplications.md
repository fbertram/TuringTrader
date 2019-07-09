# Creating Applications

unfortunately, we didn't write this article yet. 

In a nutshell, this is what you need to do:

```c#
using TuringTrader.Simulator;

class MyAlgorithm : Algorithm
{
	// your code here
}

class TuringTraderApp
{
    static void Main()
    {
        Algorithm algo = new MyAlgorithm();
        algo->Run();
        algo->Report();
    }
}
```

