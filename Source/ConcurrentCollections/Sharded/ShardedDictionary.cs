using System;
using System.Collections.Generic;
using System.Text;

namespace ConcurrentCollections.Sharded
{
    public class ShardedDictionary<K, V>
        : ShardedCollection<KeyValuePair<K, V>, Dictionary<K, V>>,
        IDictionary<K, V>
    {
        #region fields
        ValueCollection myValueCollection;
        KeyCollection myKeyCollection;
        #endregion

        #region constructors
        public ShardedDictionary(int shardCount)
            :base(shardCount)
        {
            myValueCollection = new ShardedDictionary<K, V>.ValueCollection(this);
            myKeyCollection = new ShardedDictionary<K, V>.KeyCollection(this);
        }
        #endregion

        /// <summary>
        /// Determines whether the specified value is contained within the collection
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// 	<c>true</c> if the specified value contains value; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsValue(V value)
        {
            return Loop<V>(ContainsValueTransaction, value);
        }

        private bool ContainsValueTransaction(Dictionary<K, V> collection, V value)
        {
            return collection.ContainsValue(value);
        }

        #region IDictionary<K,V> Members
        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public void Add(K key, V value)
        {
            base.Add(new KeyValuePair<K, V>(key, value));
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the key; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="key"/> is null.</exception>
        public bool ContainsKey(K key)
        {
            return ShardAwareTransaction<bool>(ContainsKeyTransaction, new KeyValuePair<K, V>(key, default(V)));
        }

        /// <summary>
        /// Determines whether [contains key transaction] [the specified v].
        /// </summary>
        /// <param name="v">The v.</param>
        /// <param name="d">The d.</param>
        /// <returns>
        /// 	<c>true</c> if [contains key transaction] [the specified v]; otherwise, <c>false</c>.
        /// </returns>
        private bool ContainsKeyTransaction(KeyValuePair<K, V> v, Dictionary<K, V> d)
        {
            return d.ContainsKey(v.Key);
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.</returns>
        public ICollection<K> Keys
        {
            get
            {
                return myKeyCollection;
            }
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="key"/> is null.</exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.</exception>
        public bool Remove(K key)
        {
            return ShardAwareTransaction<bool>(RemoveTransaction, new KeyValuePair<K, V>(key, default(V)));
        }

        /// <summary>
        /// Removes the transaction.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        private bool RemoveTransaction(KeyValuePair<K, V> v, Dictionary<K, V> d)
        {
            return d.Remove(v.Key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value"/> parameter. This parameter is passed uninitialized.</param>
        /// <returns>
        /// true if the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="key"/> is null.</exception>
        public bool TryGetValue(K key, out V value)
        {
            KeyValuePair<bool, V> r = ShardAwareTransaction<KeyValuePair<bool, V>>(TryGetValueTransaction, new KeyValuePair<K, V>(key, default(V)));
            value = r.Value;
            return r.Key;
        }

        /// <summary>
        /// Tries the get value transaction.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        private KeyValuePair<bool, V> TryGetValueTransaction(KeyValuePair<K, V> v, Dictionary<K, V> d)
        {
            V val;
            return new KeyValuePair<bool, V>(d.TryGetValue(v.Key, out val), val);
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <value></value>
        /// <returns>An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.</returns>
        public ICollection<V> Values
        {
            get
            {
                return myValueCollection;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="V"/> with the specified key.
        /// </summary>
        /// <value></value>
        public V this[K key]
        {
            get
            {
                V val;
                if (TryGetValue(key, out val))
                    return val;
                else
                    throw new KeyNotFoundException("Key \"" + key.ToString() + "\" not present in sharded dictionary");
            }
            set
            {
                Add(new KeyValuePair<K, V>(key, value));
            }
        }
        #endregion

        #region IEnumerable Members
        public new System.Collections.IEnumerator GetEnumerator()
        {
            return base.GetEnumerator();
        }
        #endregion

        protected override int CalculateHash(KeyValuePair<K, V> value)
        {
            return Math.Abs(value.Key.GetHashCode() % base.ShardCount);
        }

        #region enumerators
        public class KeyEnumerator<T>
            :IEnumerator<T>,
            IEnumerable<T>
        {
            ShardedEnumerator<KeyValuePair<T, V>, ShardedCollection<KeyValuePair<T, V>, Dictionary<T, V>>> enumerator;

            internal KeyEnumerator(ShardedEnumerator<KeyValuePair<T, V>, ShardedCollection<KeyValuePair<T, V>, Dictionary<T, V>>> enumerator)
            {
                this.enumerator = enumerator;
            }

            #region IEnumerator<T> Members
            public T Current
            {
                get
                {
                    return enumerator.Current.Key;
                }
            }
            #endregion

            #region IDisposable Members
            public void Dispose()
            {
                enumerator.Dispose();
            }
            #endregion

            #region IEnumerator Members
            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return (this as IEnumerator<K>).Current;
                }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }
            #endregion

            #region IEnumerable<T> Members
            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }
            #endregion

            #region IEnumerable Members
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this;
            }
            #endregion
        }

        public class ValueEnumerator<T>
            : IEnumerator<T>,
            IEnumerable<T>
        {
            ShardedEnumerator<KeyValuePair<K, T>, ShardedCollection<KeyValuePair<K, T>, Dictionary<K, T>>> enumerator;

            internal ValueEnumerator(ShardedEnumerator<KeyValuePair<K, T>, ShardedCollection<KeyValuePair<K, T>, Dictionary<K, T>>> enumerator)
            {
                this.enumerator = enumerator;
            }

            #region IEnumerator<T> Members
            public T Current
            {
                get
                {
                    return enumerator.Current.Value;
                }
            }
            #endregion

            #region IDisposable Members
            public void Dispose()
            {
                enumerator.Dispose();
            }
            #endregion

            #region IEnumerator Members
            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return enumerator.Current.Value;
                }
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }
            #endregion

            #region IEnumerable<T> Members
            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }
            #endregion

            #region IEnumerable Members
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this;
            }
            #endregion
        }
        #endregion

        #region value/key collections
        public class ValueCollection
            :ICollection<V>
        {
            private ShardedDictionary<K, V> collection;

            internal ValueCollection(ShardedDictionary<K, V> collection)
            {
                this.collection = collection;
            }

            #region ICollection<V> Members
            /// <summary>
            /// Not supported, value collections are readonly
            /// </summary>
            /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
            public void Add(V item)
            {
                throw new NotSupportedException("ValueCollection is readonly");
            }

            /// <summary>
            /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
            public void Clear()
            {
                collection.Clear();
            }

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
            /// </returns>
            public bool Contains(V item)
            {
                return collection.ContainsValue(item);
            }

            /// <summary>
            /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            /// <exception cref="T:System.ArgumentNullException">
            /// 	<paramref name="array"/> is null.</exception>
            /// <exception cref="T:System.ArgumentOutOfRangeException">
            /// 	<paramref name="arrayIndex"/> is less than 0.</exception>
            /// <exception cref="T:System.ArgumentException">
            /// 	<paramref name="array"/> is multidimensional.-or-<paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type <paramref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
            public void CopyTo(V[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException("Array cannot be null");
                if (arrayIndex <= 0)
                    throw new ArgumentOutOfRangeException("arrayIndex must be greater than or equal to 0");
                if (array.Rank != 1)
                    throw new ArgumentException("Array must be of rank 1");
                if (array.Length <= arrayIndex)
                    throw new ArgumentException("arrayIndex cannot be >= array.length");

                foreach (var item in this)
                {
                    array[arrayIndex] = item;
                    arrayIndex++;
                }
            }

            /// <summary>
            /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            /// <value></value>
            /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</returns>
            public int Count
            {
                get
                {
                    return collection.Count;
                }
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
            /// </summary>
            /// <value></value>
            /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.</returns>
            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            /// <summary>
            /// throws a NotSupportedException
            /// </summary>
            public bool Remove(V item)
            {
                throw new NotSupportedException("Cannot remove by value, must remove by key");
            }
            #endregion

            #region IEnumerable<V> Members
            public IEnumerator<V> GetEnumerator()
            {
                return new ValueEnumerator<V>(collection.GetEnumerator() as ShardedEnumerator<KeyValuePair<K, V>, ShardedCollection<KeyValuePair<K, V>, Dictionary<K, V>>>);
            }
            #endregion

            #region IEnumerable Members
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return (this as IEnumerable<V>).GetEnumerator();
            }
            #endregion
        }

        public class KeyCollection
            : ICollection<K>
        {
            private ShardedDictionary<K, V> collection;

            internal KeyCollection(ShardedDictionary<K, V> collection)
            {
                this.collection = collection;
            }

            #region ICollection<K> Members
            /// <summary>
            /// Not supported, KeyCollections are readonly
            /// </summary>
            /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
            public void Add(K item)
            {
                throw new NotSupportedException("Key collection is readonly");
            }

            /// <summary>
            /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only. </exception>
            public void Clear()
            {
                collection.Clear();
            }

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
            /// </returns>
            public bool Contains(K item)
            {
                return collection.ContainsKey(item);
            }

            /// <summary>
            /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            /// <exception cref="T:System.ArgumentNullException">
            /// 	<paramref name="array"/> is null.</exception>
            /// <exception cref="T:System.ArgumentOutOfRangeException">
            /// 	<paramref name="arrayIndex"/> is less than 0.</exception>
            /// <exception cref="T:System.ArgumentException">
            /// 	<paramref name="array"/> is multidimensional.-or-<paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.-or-The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.-or-Type <paramref name="T"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
            public void CopyTo(K[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException("Array cannot be null");
                if (arrayIndex <= 0)
                    throw new ArgumentOutOfRangeException("arrayIndex must be greater than or equal to 0");
                if (array.Rank != 1)
                    throw new ArgumentException("Array must be of rank 1");
                if (array.Length <= arrayIndex)
                    throw new ArgumentException("arrayIndex cannot be >= array.length");

                foreach (var item in this)
                {
                    array[arrayIndex] = item;
                    arrayIndex++;
                }
            }

            /// <summary>
            /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            /// <value></value>
            /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</returns>
            public int Count
            {
                get
                {
                    return collection.Count;
                }
            }

            /// <summary>
            /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
            /// </summary>
            /// <value></value>
            /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.</returns>
            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            /// <summary>
            /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </summary>
            /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
            /// <returns>
            /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
            /// </returns>
            /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
            public bool Remove(K item)
            {
                return collection.Remove(item);
            }
            #endregion

            #region IEnumerable<K> Members
            public IEnumerator<K> GetEnumerator()
            {
                return new KeyEnumerator<K>(collection.GetEnumerator() as ShardedEnumerator<KeyValuePair<K, V>, ShardedCollection<KeyValuePair<K, V>, Dictionary<K, V>>>);
            }
            #endregion

            #region IEnumerable Members
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return (this as IEnumerable<K>).GetEnumerator();
            }
            #endregion
        }
        #endregion

        #region test
#if DEBUG
        public new static void Test(int size)
        {
            for (int s = 1; s < size + 1; s++)
            {
                Console.WriteLine("Testing with " + s + " shards");

                ShardedDictionary<string, int> dict = new ShardedDictionary<string, int>(10);
                HashSet<KeyValuePair<string, int>> set = new HashSet<KeyValuePair<string, int>>();

                DateTime start = DateTime.Now;
                for (int i = 0; i < 100; i++)
                {
                    dict.Add(i.ToString(), i);
                    set.Add(new KeyValuePair<string, int>(i.ToString(), i));
                }
                TimeSpan t = DateTime.Now - start;
                Console.WriteLine("adding took " + t.TotalMilliseconds / 100f + "ms per op");

                start = DateTime.Now;
                foreach (KeyValuePair<string, int> item in dict)
                {
                    if (!set.Remove(item))
                        throw new Exception("Set did not contain given value!");
                }
                if (set.Count != 0)
                    throw new Exception("Set is not empty!");
                t = DateTime.Now - start;
                Console.WriteLine("iterating through entire collection took " + t.TotalMilliseconds / ((float)dict.Count) + "ms per op");

                if (dict.Count != 100)
                    throw new Exception("Incorrect count");

                start = DateTime.Now;
                for (int i = 50; i < 100; i++)
                {
                    if (!dict.Remove(i.ToString()))
                        throw new Exception("Did not remove item");
                }
                t = DateTime.Now - start;
                Console.WriteLine("removing existant items took " + t.TotalMilliseconds / 50f + "ms per op");

                if (dict.Count != 50)
                    throw new Exception("Incorrect count");

                start = DateTime.Now;
                for (int i = 50; i < 1000; i++)
                {
                    if (dict.Remove(i.ToString()))
                        throw new Exception("removed item which should not be in collection");
                }
                t = DateTime.Now - start;
                Console.WriteLine("removing non existant items took " + t.TotalMilliseconds / ((float)(1000 - 50)) + "ms per op");


                start = DateTime.Now;
                for (int i = 0; i < 50; i++)
                {
                    if (!dict.ContainsKey(i.ToString()))
                        throw new Exception("Dictionary does not contain this key!");
                }
                t = DateTime.Now - start;
                Console.WriteLine("key containment test on existant items took " + t.TotalMilliseconds / 50f + "ms per op");


                start = DateTime.Now;
                for (int i = 50; i < 1000; i++)
                {
                    if (dict.ContainsKey(i.ToString()))
                        throw new Exception("Dictionary contains a nonexistant key!");
                }
                t = DateTime.Now - start;
                Console.WriteLine("key containment test on nonexistant items " + t.TotalMilliseconds / ((float)(1000 - 50)) + "ms per op");

                start = DateTime.Now;
                for (int i = 0; i < 50; i++)
                {
                    if (!dict.ContainsValue(i))
                        throw new Exception("Dictionary does not contain this value!");
                }
                t = DateTime.Now - start;
                Console.WriteLine("value containment test on existant items took " + t.TotalMilliseconds / 50f + "ms per op");

                start = DateTime.Now;
                for (int i = 50; i < 1000; i++)
                {
                    if (dict.ContainsValue(i))
                        throw new Exception("Dictionary contains a nonexistant value!");
                }
                t = DateTime.Now - start;
                Console.WriteLine("value containment test on nonexistant items took " + t.TotalMilliseconds / ((float)(1000 - 50)) + "ms per op");

                foreach (var item in dict.Values)
                {
                    Console.WriteLine(item);
                    if (!dict.ContainsValue(item))
                        throw new Exception("Item is in value collection but not original collection!");
                }
                foreach (var item in dict.Keys)
                {
                    Console.WriteLine(item);
                    if (!dict.ContainsKey(item))
                        throw new Exception("Item is in key collection but not original collection!");
                }

                dict.Values.Clear();
                if (dict.Count != 0)
                    throw new Exception("Count is not zero after clear operation");
                dict.Add("1", 1);

                if (!dict.Values.Contains(1))
                    throw new Exception("value is in dictionary but not value collection!");
                if (!dict.Keys.Contains("1"))
                    throw new Exception("key is in dictionary but not key collection");
            }
        }
#endif
        #endregion
    }
}
