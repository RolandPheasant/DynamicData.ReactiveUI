//using DynamicData;
//using ReactiveUI;
//using System;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Reactive;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using System.Threading.Tasks;
//using DynamicData.ReactiveUI;

//namespace WPFAsyncQueue
//{
//    internal class ViewModel
//    {


//        private readonly ReadOnlyObservableCollection<int> _result;

//        public ViewModel()
//        {

//            var backEnd = new Subject<int>();

//            //IObservable<IChangeSet<int, int>> loadedResult = backEnd
//            //    .ToObservableChangeSet(limitSizeTo: 5);


//            //    .AsObservableList<int>();

//            //Ticks = new ReactiveList<int> { ChangeTrackingEnabled = true };
//            //Ticks.AddRange(Enumerable.Repeat(0, 5));

//            StartSimulation = ReactiveCommand.CreateAsyncObservable(_ =>
//            {
//                var ticks = Observable.Interval(TimeSpan.FromSeconds(1.0)).Take(10);
//                ticks.ToObservableChangeSet(limitSizeTo: 5).AsObservableCache();
//                return Task.CompletedTask;
//            });


//        }

//        public ReactiveCommand<Unit> StartSimulation { get; set; }

//        public ReactiveList<int> Ticks
//        {
//            get; set;
//        }
//    }
//}