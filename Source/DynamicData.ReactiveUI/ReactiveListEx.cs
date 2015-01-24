using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Kernel;
using ReactiveUI;

namespace DynamicData.ReactiveUI
{
    /// <summary>
    /// Extensions to convert a reactive list collection into a dynamic stream
    /// </summary>
    public static class ReactiveListEx
    {
        /// <summary>
        /// Convert an observable collection into a dynamic stream of change sets, using the hash code as the object's key
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">source</exception>
        public static IObservable<IChangeSet<TObject, int>> ToObservableChangeSet<TObject>(this  IReactiveCollection<TObject> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            return source.ToObservableChangeSet(t => t.GetHashCode());
        }

        /// <summary>
        /// Convert an observable collection into a dynamic stream of change sets
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">source
        /// or
        /// keySelector</exception>
        public static IObservable<IChangeSet<TObject, TKey>> ToObservableChangeSet<TObject, TKey>(this  IReactiveCollection<TObject> source, Func<TObject, TKey> keySelector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (keySelector == null) throw new ArgumentNullException("keySelector");

            return Observable.Create<IChangeSet<TObject, TKey>>
                (
                    observer =>
                    {
                        //populate local cache, otherwise there is no way to deal with a reset
                        var resultCache = new SourceCache<TObject, TKey>(keySelector);

                        Func<ChangeSet<TObject, TKey>> initialChangeSet = () =>
                        {
                            var items = source.Select(t => new Change<TObject, TKey>(ChangeReason.Add, keySelector(t), t));
                            return new ChangeSet<TObject, TKey>(items);
                        };

                        var sourceUpdates = Observable
                            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                                h => source.CollectionChanged += h,
                                h => source.CollectionChanged -= h)
                            .Select
                            (
                                args =>
                                {
                                    var changes = args.EventArgs;

                                    switch (changes.Action)
                                    {
                                        case NotifyCollectionChangedAction.Add:
                                            return changes.NewItems.OfType<TObject>()
                                                .Select(t => new Change<TObject, TKey>(ChangeReason.Add, keySelector(t), t));

                                        case NotifyCollectionChangedAction.Remove:
                                            return changes.OldItems.OfType<TObject>()
                                                .Select(t => new Change<TObject, TKey>(ChangeReason.Remove, keySelector(t), t));

                                        case NotifyCollectionChangedAction.Replace:
                                        {
                                            return changes.NewItems.OfType<TObject>()
                                                .Select((t, idx) =>
                                                {
                                                    var old = changes.OldItems[idx];
                                                    return new Change<TObject, TKey>(ChangeReason.Update, keySelector(t), t, (TObject)old);
                                                });
                                        }
                                        case NotifyCollectionChangedAction.Reset:
                                        {
                                            //Clear all from the cache and reload
                                            var removes = resultCache.KeyValues.Select(t => new Change<TObject, TKey>(ChangeReason.Remove, t.Key, t.Value)).ToArray();
                                            return removes.Concat(initialChangeSet());
                                        }
                                        default:
                                            return null;
                                    }
                                })
                            .Where(updates => updates != null)
                            .Select(updates => (IChangeSet<TObject, TKey>)new ChangeSet<TObject, TKey>(updates));


                        var initialChanges = initialChangeSet();
                        var cacheLoader = Observable.Return(initialChanges).Concat(sourceUpdates).PopulateInto(resultCache);
                        var subscriber = resultCache.Connect().SubscribeSafe(observer);

                        return new CompositeDisposable(cacheLoader, subscriber, resultCache);
                    }

                );
        }
    }
}