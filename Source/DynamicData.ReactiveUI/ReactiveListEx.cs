using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
		/// <typeparam name="T"></typeparam>
		/// <param name="source">The source.</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentNullException">source</exception>
		public static IObservable<IChangeSet<T>> ToObservableChangeSet<T>(this  ReactiveList<T> source)
        {
			return Observable.Create<IChangeSet<T>>
				(
					observer =>
					{
						Func<ChangeSet<T>> initialChangeSet = () =>
						{
							var items = source.Select((t, index) => new Change<T>(ListChangeReason.Add, t, index));
							return new ChangeSet<T>(items);
						};

						//populate local cache, otherwise there is no way to deal with a reset
						var cloneOfList = new SourceList<T>();

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
											return changes.NewItems.OfType<T>()
												.Select((t, index) => new Change<T>(ListChangeReason.Add, t, index + changes.NewStartingIndex));

										case NotifyCollectionChangedAction.Remove:
											return changes.OldItems.OfType<T>()
												.Select((t, index) => new Change<T>(ListChangeReason.Remove, t, index + changes.OldStartingIndex));

										case NotifyCollectionChangedAction.Replace:
											{
												return changes.NewItems.OfType<T>()
													.Select((t, idx) =>
													{
														var old = changes.OldItems[idx];
														return new Change<T>(ListChangeReason.Replace, t, (T)old, idx, idx);
													});
											}
										case NotifyCollectionChangedAction.Reset:
											{
												//Clear all from the cache and reload
												var removes = source.Select((t, index) => new Change<T>(ListChangeReason.Remove, t, index)).Reverse();
												return removes.Concat(initialChangeSet());
											}
                                        case NotifyCollectionChangedAction.Move:
                                            {
                                                var item = changes.NewItems.OfType<T>().First();
                                                var change = new Change<T>(item, changes.NewStartingIndex, changes.OldStartingIndex);
                                                return new[] { change };
                                            }
                                        default:
											return null;
									}
								})
							.Where(updates => updates != null)
							.Select(updates => (IChangeSet<T>)new ChangeSet<T>(updates));

						var initialChanges = initialChangeSet();
						var cacheLoader = Observable.Return(initialChanges).Concat(sourceUpdates).PopulateInto(cloneOfList);
						var subscriber = cloneOfList.Connect().SubscribeSafe(observer);
						return new CompositeDisposable(cacheLoader, subscriber, cloneOfList);
					});
		}

        /// <summary>
        /// Clones the ReactiveList from all changes
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">source
        /// or
        /// keySelector</exception>
        public static IObservable<IChangeSet<TObject, TKey>> ToObservableChangeSet<TObject, TKey>(this  ReactiveList<TObject> source, Func<TObject, TKey> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

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


        internal static void CloneReactiveList<T>(this ReactiveList<T> source, IChangeSet<T> changes)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (changes == null) throw new ArgumentNullException(nameof(changes));

            changes.ForEach(item =>
            {

                switch (item.Reason)
                {
                    case ListChangeReason.Add:
                        {
                            var change = item.Item;
                            var hasIndex = change.CurrentIndex >= 0;
                            if (hasIndex)
                            {
                                source.Insert(change.CurrentIndex, change.Current);
                            }
                            else
                            {
                                source.Add(change.Current);
                            }
                            break;
                        }

                    case ListChangeReason.AddRange:
                        {
                            var startingIndex = item.Range.Index;

                            if (RxApp.SupportsRangeNotifications)
                            {
                                if (startingIndex >= 0)
                                {
                                    source.InsertRange(startingIndex,item.Range);
                                }
                                else
                                {
                                    source.AddRange(item.Range);
                                }
                            }
                            else
                            {
                                if (startingIndex >= 0)
                                {
                                    item.Range.Reverse().ForEach(t => source.Insert(startingIndex, t));
                                }
                                else
                                {
                                    item.Range.ForEach(source.Add);
                                }
                            }

                            break;
                        }

                    case ListChangeReason.Clear:
                        {
                            source.Clear();
                            break;
                        }

                    case ListChangeReason.Replace:
                        {

                            var change = item.Item;
                            bool hasIndex = change.CurrentIndex >= 0;
                            if (hasIndex && change.CurrentIndex == change.PreviousIndex)
                            {
                                source[change.CurrentIndex] = change.Current;
                            }
                            else
                            {
                                source.RemoveAt(change.PreviousIndex);
                                source.Insert(change.CurrentIndex, change.Current);
                            }
                        }
                        break;
                    case ListChangeReason.Remove:
                        {
                            var change = item.Item;
                            bool hasIndex = change.CurrentIndex >= 0;
                            if (hasIndex)
                            {
                                source.RemoveAt(change.CurrentIndex);
                            }
                            else
                            {
                                source.Remove(change.Current);
                            }
                            break;
                        }

                    case ListChangeReason.RemoveRange:
                        {
                            if (RxApp.SupportsRangeNotifications && item.Range.Index>=0)
                            {
                                source.RemoveRange(item.Range.Index, item.Range.Count);
                            }
                            else
                            {
                                source.RemoveMany(item.Range);
                            }
                        }
                        break;

                    case ListChangeReason.Moved:
                        {
                            var change = item.Item;
                            bool hasIndex = change.CurrentIndex >= 0;
                            if (!hasIndex)
                                throw new UnspecifiedIndexException("Cannot move as an index was not specified");

                            source.Move(change.PreviousIndex, change.CurrentIndex);
                            break;
                        }
                }
            });


        }

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
       
        }

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var i = -1;
            foreach (var item in source)
                action(item,i++);
          
        }
    }
}