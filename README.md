### What is this library?

**ReactiveUI** is a powerful MVVM framework based on Rx. 
**Dynamic Data** is a portable class library based on Rx and provides an observable list and an observable cache. There are a very rich set of collection specific operators.

This library plugs dynamic data into reactive ui to give you the power the power of both.

- [ReactiveUI on GitHub](https://github.com/reactiveui/ReactiveUI)
- [Dynamic Data on GitHub](https://github.com/RolandPheasant/DynamicData) 

###  Create a dynamic data source

Create an observable list.
```
var list = new SourceList<T>();
var observableChanges = list.Connect();
```
or create an observable cache.
```
var cache = new SourceCache<TObject,TKey>(t=>t.Key);
var observableChanges = cache.Connect();
```
and that is that. You have a thread safe data source which either of which become observable by calling ```.Connect()```. The connect method has dozens of collection specified composable and fluent operators.

###  Create a dynamic data source from  ReactiveList

```
var list = new SourceList<T>();
```
Now create a cache source or a list source as follows
```
var observableChanges= list.ToObservableChangSet();
//or
var observableChanges= list.ToObservableChangSet(t=>t.Key);
```
Integration of dynamic data and reactive ui is as easy as that. 

### What are the benefits of the integration between Dynamic Data and ReactiveUI

The cache part of dynamic data has about 60 operators and the list side has about 35 operators and counting

```csharp
var myoperation = observableChanges
			.Filter(trade=>trade.Status == TradeStatus.Live) 
			.Transform(trade => new TradeProxy(trade))
			.Sort(SortExpressionComparer<TradeProxy>.Descending(t => t.Timestamp))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Bind(myReactiveList) //an instance of a reactive list
			.DisposeMany()
			.Subscribe()
```
As ```somedynamicdatasource``` changes the results are filtered by live trades, transformed into a proxy, put into a sort order and the reactive list will exactly reflect all this. When a tradeproxy is removed from the observable it is disposed and when  ```myoperation``` is disposed, all trade proxys are disposed.

That was easy, declarative and powerful.

###So what else can it do

In all the examples, for readabilty I have ommited the ```datasource.Connect()``` method. All the operators are fluent and composable.

#### Group
```csharp
var myoperation = somedynamicdatasource
            .GroupOn(person=>person.Status) 
			.Subscribe(changeSet=>//do something with the groups)
```
#### Transformation

```csharp
//transform many to flatten a child enumerable
var myoperation = somedynamicdatasource.TransformMany(person=>person.Children) 

//Map to a proxy object
var myoperation = somedynamicdatasource.Transform(person=>new PersonProxy(person)) 
```

#### Aggregation

if we have a a list of people we can aggregate as follows
```csharp
var people= new SourceList<Person>();
var observable = people.Connect();

var count= observable.Count();
var max= observable.Max(p=>p.Age);
var min= observable.Min(p=>p.Age);
var stdDev= observable.StdDev(p=>p.Age);
```

#### Join operators

There are And, Or and Except logical operators
```csharp
var peopleA= new SourceCache<Person,string>(p=>p.Name);
var peopleB= new SourceCache<Person,string>(p=>p.Name);
var observableA = peopleA.Connect();
var observableB = peopleB.Connect();

var inBoth = observableA.And(observableB);
var inEither= observableA.Or(observableB);
var inAandNotinB = observableA.Except(observableB);
```

#### Virtualisation

There are Virtualise, Page and Top operators

```csharp
//virtualise
var controller =  new VirtualisingController(new VirtualRequest(0,25));
var myoperation = somedynamicdatasource.Virtualise(controller)

//Top (overload of virtualise)
var myoperation = somedynamicdatasource.Top(10)

//page
var controller =  new PageController(new PageRequest(1,25));
var myoperation = somedynamicdatasource.Page(controller)
```
The parameters of the controllers above can be changes any time to force a re-evaluation.

Plus much much more...

### Some links to get going

- Contact me any time on [![Join the chat at https://gitter.im/RolandPheasant/DynamicData](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/RolandPheasant/DynamicData?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
- Master build [![Build status](https://ci.appveyor.com/api/projects/status/22ywek7rlteq28go/branch/develop?svg=true)](https://ci.appveyor.com/project/RolandPheasant/dynamicdata-reactiveui/branch/develop)
- Sample wpf project https://github.com/RolandPheasant/Dynamic.Trader
- Blog at  http://dynamic-data.org/
- Get it from https://www.nuget.org/packages/DynamicData.ReactiveUI
