# Indicator Pitfalls

Unfortunately, we didn't write this article yet. 

Here are some hints:

* indicators must be called exactly once per bar. Therefore
  * do not call indicators within conditional statements
  * be extra careful when calling indicators within LINQ or logical expressions
* indicators are distinguished by their type, their input time series, their parameter values, and the line of code from which they are called. This might lead to situations, where indicators are called multiple times, even though you didn't intend to do so

