# Building Custom Applications

Custom applications allow you to integrate TuringTrader with other functionality, e.g., to implement automated trading. But, unfortunately, we didn't write this article yet.

However, the dev-branch contains a working sample: [https://github.com/fbertram/TuringTrader/tree/develop/More/TuringTrader.CustomApp](https://github.com/fbertram/TuringTrader/tree/develop/More/TuringTrader.CustomApp)

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

