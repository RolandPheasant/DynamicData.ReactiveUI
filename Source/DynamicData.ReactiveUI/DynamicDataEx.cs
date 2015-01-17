using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

namespace DynamicData.ReactiveUI
{
    public static class DynamicDataEx
    {

        /// <summary>
        /// Populate and maintain the specified reactive list from the source observable changeset
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="resetThreshold">The reset threshold before a reset event  on the target list is invoked</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// destination
        /// or
        /// target
        /// </exception>
        public static IObservable<IChangeSet<TObject, TKey>> Bind<TObject, TKey>(
            this IObservable<IChangeSet<TObject, TKey>> source, IReactiveList<TObject> target,
            int resetThreshold = 25)
        {
            if (target == null) throw new ArgumentNullException("target");

            var adaptor = new ReactiveListAdaptor<TObject, TKey>(target, resetThreshold);
            return source.Bind(adaptor);

        }

        /// <summary>
        /// Binds the results using the specified changeset adaptor
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="updater">The updater.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">source</exception>
        public static IObservable<IChangeSet<TObject, TKey>> Bind<TObject, TKey>(
            this IObservable<IChangeSet<TObject, TKey>> source,
            IChangeSetAdaptor<TObject, TKey> updater)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (updater == null) throw new ArgumentNullException("updater");

            return Observable.Create<IChangeSet<TObject, TKey>>
                (observer =>
                {
                    var locker = new object();
                    var published = source.Synchronize(locker).Publish();

                    var adaptor = published.Subscribe(updates =>
                    {
                        try
                        {
                            updater.Adapt(updates);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                        observer.OnError, observer.OnCompleted);

                    var connected = published.Connect();

                    var subscriber = published.SubscribeSafe(observer);

                    return Disposable.Create(() =>
                    {
                        adaptor.Dispose();
                        subscriber.Dispose();
                        connected.Dispose();
                    });
                }
                );
        }

        /// <summary>
        /// Populate and maintain the specified reactive list from the source observable changeset
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="resetThreshold">The reset threshold before a reset event  on the target list is invoked</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// destination
        /// or
        /// target
        /// </exception>
        public static IObservable<ISortedChangeSet<TObject, TKey>> Bind<TObject, TKey>(
            this IObservable<ISortedChangeSet<TObject, TKey>> source, ReactiveList<TObject> target,
            int resetThreshold = 25)
        {
            if (target == null) throw new ArgumentNullException("target");

            var adaptor = new SortedReactiveListAdaptor<TObject, TKey>(target, resetThreshold);
            return source.Bind(adaptor);

        }

        /// <summary>
        /// Binds the results using the specified sorted changeset adaptor
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="updater">The updater.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">source</exception>
        public static IObservable<ISortedChangeSet<TObject, TKey>> Bind<TObject, TKey>(
            this IObservable<ISortedChangeSet<TObject, TKey>> source,
            ISortedChangeSetAdaptor<TObject, TKey> updater)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (updater == null) throw new ArgumentNullException("updater");

            return Observable.Create<ISortedChangeSet<TObject, TKey>>
                (observer =>
                {
                    var locker = new object();
                    var published = source.Synchronize(locker).Publish();

                    var adaptor = published.Subscribe(updates =>
                    {
                        try
                        {
                            updater.Adapt(updates);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                        }
                    },
                        observer.OnError, observer.OnCompleted);

                    var connected = published.Connect();

                    var subscriber = published.SubscribeSafe(observer);

                    return Disposable.Create(() =>
                    {
                        adaptor.Dispose();
                        subscriber.Dispose();
                        connected.Dispose();
                    });
                }
                );
        }
    }
}
