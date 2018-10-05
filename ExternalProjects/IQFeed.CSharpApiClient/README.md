# IQFeed CSharp Api Client #
[![Build status](https://ci.appveyor.com/api/projects/status/6u1f245xxvkri7s2/branch/master?svg=true)](https://ci.appveyor.com/project/mathpaquette/iqfeed-csharpapiclient/branch/master) [![NuGet Version and Downloads count](https://buildstats.info/nuget/IQFeed.CSharpApiClient)](https://www.nuget.org/packages/IQFeed.CSharpApiClient) 

IQFeed.CSharpApiClient is fastest and the most well designed C# DTN IQFeed socket API connector available to the open source community! Currently supporting the latest stable IQFeed protocol version 6.0.

IQFeed is an affordable and reputable Internet market data provider. For more [info](http://www.iqfeed.net/index.cfm?displayaction=developer&section=main).<br>
*** SPECIAL OFFER (Save $50 - No Startup Fee) *** [Get Free Trial Now](https://www.iqfeed.net/trent/index.cfm?displayaction=start&promo=1996499)

If you appreciate this project, please star :star: it now!

## Table of Contents
- [Features](#features)
- [Usage](#usage)
  - [Installation](#installation)
  - [Configuration](#configuration)
  - [Examples](#examples)
- [IQFeed API support status](#iqfeed-api-support-status)
- [Contributing](#contributing)

## Features
- Designed completely non-blocking from bottom-up with nice async/await interfaces
- Streaming events are distributed in a consistent way using Action delegates
- Handle multiple socket connections for at least 40% performance increase when requesting lookup data
- Sockets are using [SocketAsyncEventArgs](https://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs(v=vs.110).aspx) for maximum performance and trying to reduce pressure on GC
- Support for .NET Core 2.0

## Usage
### Installation
`Install-Package IQFeed.CSharpApiClient`

### Configuration
Now, you need to set your API credentials and product id somewhere. You have 2 options, in your user environment variables or app.config.

##### User environment variables
- Run `rundll32 sysdm.cpl,EditEnvironmentVariables` to open the Environment Variables
- In your User variables, create 4 new ones:
  - IQCONNECT_LOGIN
  - IQCONNECT_PASSWORD
  - IQCONNECT_PRODUCT_ID
  - IQCONNECT_PRODUCT_VERSION (not mandatory, will fallback to 1.0.0.0)

##### app.config
In your appSettings section, assign values to these key:
```
<appSettings>
     <add key="IQConnect:login" value=""/>  
     <add key="IQConnect:password" value=""/>  
     <add key="IQConnect:product_id" value=""/>  
     <add key="IQConnect:product_version" value=""/>  
</appSettings>
```

### Examples
Check IQFeed.CSharpApiClient.Example for more examples.

```
IQFeedLauncher.Start();
var lookupClient = LookupClientFactory.CreateNew();
lookupClient.Connect();
var ticksMessages = await lookupClient.Historical.ReqHistoryTickDatapointsAsync("AAPL", 100);
```

### IQFeed API support status
##### Streaming data
- [x] Level 1 data
- [ ] Level 2 data
- [x] Derivative data
- [x] Admin data
##### Lookup data
- [x] Historical data
- [ ] News data
- [ ] Symbol Lookup data
- [x] Chains Lookup data
 
 # Contributing
 Pull requests are welcome! Don't hesitate to open an issue if something goes wrong.