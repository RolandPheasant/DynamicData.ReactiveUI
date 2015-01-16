using System;
using System.Collections.Generic;
using ReactiveUI;

namespace DynamicData.ReactiveUI
{
    public class ReactiveListAdaptor<TObject, TKey> : IChangeSetAdaptor<TObject, TKey>
    {
        private IDictionary<TKey, TObject> _data;
        private bool _loaded;
        private readonly IReactiveList<TObject> _target;
        private readonly int _resetThreshold;
        
        public ReactiveListAdaptor(IReactiveList<TObject> target, int resetThreshold = 50)
        {
            if (target == null) throw new ArgumentNullException("target");
            _target = target;
            _resetThreshold = resetThreshold;
        }

        public void Adapt(IChangeSet<TObject, TKey> changes)
        {
            Clone(changes);

            if (changes.Count > _resetThreshold || !_loaded)
            {
                _loaded = true;
                using (_target.SuppressChangeNotifications())
                {
                    _target.Clear();
                    _target.AddRange(_data.Values);
                }

            }
            else
            {
                DoUpdate(changes);
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
                        _target.Add(change.Current);
                        break;
                    case ChangeReason.Remove:
                        _target.Remove(change.Current);
                        break;
                    case ChangeReason.Update:
                    {
                        _target.Remove(change.Previous.Value);
                        _target.Add(change.Current);
                    }
                        break;
                }
            }

        }
    }
}