using System;
using System.Collections.Generic;
using System.Linq;

namespace RainbowMage.ActServer.Collections
{
    public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey: IComparable<TKey>
    {
        IList<KeyValuePair<TKey, TValue>> dict;

        public OrderedDictionary()
        {
            dict = new List<KeyValuePair<TKey, TValue>>();
        }

        public void Sort(Comparison<KeyValuePair<TKey, TValue>> comparison)
        {
            ((List<KeyValuePair<TKey, TValue>>)dict).Sort(comparison);
        }

        #region IDictionary<TKey, TValue>
        public void Add(TKey key, TValue value)
        {
            if (!dict.Any(x => x.Key.CompareTo(key) == 0))
            {
                dict.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
            else
            {
                throw new ArgumentException("The key already exist.", "key");
            }
        }

        public bool ContainsKey(TKey key)
        {
            return dict.Any(x => x.Key.CompareTo(key) == 0);
        }

        public ICollection<TKey> Keys
        {
            get { return dict.Select(x => x.Key).ToArray(); }
        }

        public bool Remove(TKey key)
        {
            var item = dict.FirstOrDefault(x => x.Key.CompareTo(key) == 0);
            if (!item.Equals(default(KeyValuePair<TKey, TValue>)))
            {
                return dict.Remove(item);
            }
            else
            {
                return false;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var item = dict.FirstOrDefault(x => x.Key.CompareTo(key) == 0);
            if (!item.Equals(default(KeyValuePair<TKey, TValue>)))
            {
                value = item.Value;
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        public ICollection<TValue> Values
        {
            get { return dict.Select(x => x.Value).ToArray(); }
        }

        public TValue this[TKey key]
        {
            get
            {
                var item = dict.FirstOrDefault(x => x.Key.CompareTo(key) == 0);
                if (!item.Equals(default(KeyValuePair<TKey, TValue>)))
                {
                    return item.Value;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            set
            {
                var item = dict.FirstOrDefault(x => x.Key.CompareTo(key) == 0);
                if (!item.Equals(default(KeyValuePair<TKey, TValue>)))
                {
                    var index = dict.IndexOf(item);
                    dict[index] = new KeyValuePair<TKey, TValue>(key, value);
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            dict.Add(item);
        }

        public void Clear()
        {
            dict.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dict.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return dict.Count; }
        }

        public bool IsReadOnly
        {
            get { return dict.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return dict.Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return dict.GetEnumerator();
        }
        #endregion
    }
}
