using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData.Operators;
using ReactiveUI;

namespace DynamicData.ReactiveUI
{
    public class SortedReactiveListAdaptor<TObject, TKey> : ISortedChangeSetAdaptor<TObject, TKey>
    {
        private IDictionary<TKey, TObject> _data;
        private readonly ReactiveList<TObject> _target;
        private readonly int _resetThreshold;
        
        public SortedReactiveListAdaptor(ReactiveList<TObject> target, int resetThreshold = 50)
        {
            if (target == null) throw new ArgumentNullException("target");
            _target = target;
            _resetThreshold = resetThreshold;
        }

        public void Adapt(ISortedChangeSet<TObject, TKey> changes)
        {
            Clone(changes);

            switch (changes.SortedItems.SortReason)
            {
                case SortReason.InitialLoad:
                case SortReason.ComparerChanged:
                case SortReason.Reset:
                {
                    using (_target.SuppressChangeNotifications())
                    {
                        _target.Clear();
                        _target.AddRange(changes.SortedItems.Select(kv => kv.Value));
                    }
                }
                    break;

                case SortReason.DataChanged:
                {
                    if (changes.Count > _resetThreshold)
                    {
                        using (_target.SuppressChangeNotifications())
                        {
                            _target.Clear();
                            _target.AddRange(changes.SortedItems.Select(kv => kv.Value));
                        }
                    }
                    else
                    {
                        DoUpdate(changes);
                    }
                }
                    break;
                case SortReason.Reorder:

                    //Updates will only be moves, so appply logic
                    DoUpdate(changes);
                    break;


                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Clone(IChangeSet<TObject, TKey> changes)
        {
            //for efficiency resize dictionary to initial batch size
            if (_data == null || _data.Count == 0)
                _data = new Dictionary<TKey, TObject>(changes.Count);

            foreach (var item in changes)
            {
                switch (item.Reason)
                {
                    case ChangeReason.Update:
                    case ChangeReason.Add:
                    {
                        _data[item.Key] = item.Current;
                    }
                        break;
                    case ChangeReason.Remove:
                        _data.Remove(item.Key);
                        break;
                }
            }
        }


        private void DoUpdate(IChangeSet<TObject, TKey> changes)
        {

            foreach (var change in changes)
            {
                switch (change.Reason)
                {
                    case ChangeReason.Add:
                        _target.Insert(change.CurrentIndex, change.Current);
                        break;
                    case ChangeReason.Remove:
                        _target.RemoveAt(change.CurrentIndex);
                        break;
                    case ChangeReason.Moved:
                        _target.Move(change.PreviousIndex, change.CurrentIndex);
                        break;
                    case ChangeReason.Update:
                        {
                            _target.RemoveAt(change.PreviousIndex);
                            _target.Insert(change.CurrentIndex, change.Current);
                        }
                        break;

                }
            }

        }
    }
}