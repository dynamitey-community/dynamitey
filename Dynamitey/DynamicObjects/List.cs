// 
//  Copyright 2011 Ekon Benefits
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.CSharp.RuntimeBinder;

namespace Dynamitey.DynamicObjects
{
    /// <summary>
    /// Expando-Type List for dynamic objects
    /// </summary>
   
    public class List : BaseDictionary, IList<object>, IDictionary<string, object>, INotifyCollectionChanged, IList

    {

        /// <summary>
        /// Wrapped list
        /// </summary>
       
        protected IList<object> _list;


        private static readonly object ListLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="List"/> class.
        /// </summary>
        /// <param name="contents">The contents.</param>
        /// <param name="members">The members.</param>
        [RequiresDynamicCode("Constructing any BaseObject-derived type instantiates System.Dynamic.DynamicObject, whose default constructor requires the DLR's runtime code generation; not supported when AOT-compiled.")]
        public List(
            IEnumerable<object>? contents =null,
            IEnumerable<KeyValuePair<string, object>>? members =null):base(members)
        {
            if (contents == null)
            {
                _list = new List<object>();
                return;
            }
            _list = contents is IList<object> tContents
                ? tContents
                : contents.ToList();
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<dynamic> GetEnumerator()
        {
            return _list.GetEnumerator();
        }



        /// <summary>
        /// Adds the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "The item parameter is statically typed 'dynamic', so this call binds through the DLR binder even though the underlying operation is a plain list add. " +
            "Can't carry [RequiresUnreferencedCode] itself: it implements a plain BCL " +
            "collection interface member that isn't annotated, and the two must match. The " +
            "actionable warning belongs to whichever 'dynamic'-typed argument the caller " +
            "supplied, which already carries this requirement at its own declaration.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same reasoning as the IL2026 suppression on this member.")]
        public void Add(dynamic item)
        {
            InsertHelper(item);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            lock (ListLock)
            {
                _list.Clear();

            } 
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        /// <summary>
        /// Determines whether [contains] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// 	<c>true</c> if [contains] [the specified item]; otherwise, <c>false</c>.
        /// </returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "The item parameter is statically typed 'dynamic', so this call binds through the DLR binder even though the underlying operation is a plain list lookup. " +
            "Can't carry [RequiresUnreferencedCode] itself: it implements a plain BCL " +
            "collection interface member that isn't annotated, and the two must match. The " +
            "actionable warning belongs to whichever 'dynamic'-typed argument the caller " +
            "supplied, which already carries this requirement at its own declaration.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same reasoning as the IL2026 suppression on this member.")]
        // dynamic? rather than plain dynamic: this method is the implicit implementation of both
        // ICollection<object>.Contains(object item) (non-null) and, via the non-generic IList this
        // class also implements, IList.Contains(object? value) - the second genuinely needs a
        // nullable parameter to match, and satisfying it doesn't weaken the first.
        public bool Contains(dynamic? item)
        {
            return _list.Contains(item);
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(object[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }



        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count => _list.Count;


        /// <summary>
        /// Indexes the of.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "The item parameter is statically typed 'dynamic', so this call binds through the DLR binder even though the underlying operation is a plain list lookup. " +
            "Can't carry [RequiresUnreferencedCode] itself: it implements a plain BCL " +
            "collection interface member that isn't annotated, and the two must match. The " +
            "actionable warning belongs to whichever 'dynamic'-typed argument the caller " +
            "supplied, which already carries this requirement at its own declaration.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same reasoning as the IL2026 suppression on this member.")]
        // See Contains above for why this is dynamic? rather than plain dynamic.
        public int IndexOf(dynamic? item)
        {
            lock (ListLock)
            {
                return _list.IndexOf(item);
            }
        }

        /// <summary>
        /// Inserts the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "The item parameter is statically typed 'dynamic', so this call binds through the DLR binder even though the underlying operation is a plain list insert. " +
            "Can't carry [RequiresUnreferencedCode] itself: it implements a plain BCL " +
            "collection interface member that isn't annotated, and the two must match. The " +
            "actionable warning belongs to whichever 'dynamic'-typed argument the caller " +
            "supplied, which already carries this requirement at its own declaration.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same reasoning as the IL2026 suppression on this member.")]
        // See Contains above for why this is dynamic? rather than plain dynamic.
        public void Insert(int index, dynamic? item)
        {
            InsertHelper(item,index);
        }

        private void InsertHelper(object? item, int? index = null)
        {
            // _list's element type is non-null to match IList<object>, but callers reach this
            // through 'dynamic' entry points that don't check for null - preserved as-is.
            lock (ListLock)
            {
                if (!index.HasValue)
                {
                    index = _list.Count;
                    _list.Add(item!);

                }
                else
                {
                    _list.Insert(index.Value, item!);
                }
            }
            OnCollectionChanged(NotifyCollectionChangedAction.Add, newItem: item, newIndex: index);
        }

        /// <summary>
        /// Removes at.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveAt(int index)
        {
            RemoveHelper(index: index);
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
            "The item parameter is statically typed 'dynamic', so this call binds through the DLR binder even though the underlying operation is a plain list remove. " +
            "Can't carry [RequiresUnreferencedCode] itself: it implements a plain BCL " +
            "collection interface member that isn't annotated, and the two must match. The " +
            "actionable warning belongs to whichever 'dynamic'-typed argument the caller " +
            "supplied, which already carries this requirement at its own declaration.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same reasoning as the IL2026 suppression on this member.")]
        public bool Remove(dynamic item)
        {
            return RemoveHelper(item);
        }

        private bool RemoveHelper(object? item = null, int? index = null)
        {
      
            lock (ListLock)
            {
                if (item != null)
                {
                    index = _list.IndexOf(item);
                    if (index < 0)
                        return false;
                }

                item  = item ?? _list[index.GetValueOrDefault()];
                _list.RemoveAt(index.GetValueOrDefault());
            } 
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, oldItem: item, oldIndex: index);

            return true;
        }

        /// <summary>
        /// Gets or sets the <see cref="object" /> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="object" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public dynamic this[int index]
        {
            get => _list[index];

            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "value is statically typed 'dynamic', so assigning it into _list[index] binds through the DLR binder even though the underlying operation is a plain list write. " +
                "Can't carry [RequiresUnreferencedCode] itself: it implements IList<T>/IList indexer setters that aren't annotated, and the two must match. The actionable warning belongs to the 'dynamic'-typed value the caller supplied.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same reasoning as the IL2026 suppression on this member.")]
            set
            {
                object tOld;
                lock (ListLock)
                {
                    tOld = _list[index];
                    _list[index] = value;
                }

                OnCollectionChanged(NotifyCollectionChangedAction.Replace, tOld, value, index);
            }
        }

        // Separate from the dynamic indexer above: IList<object>.this[int] needs a non-null
        // getter (satisfied by the property above), but the non-generic IList.this[int] setter
        // is nullable - the same split as Contains/IndexOf/Insert above, except here the getter
        // and setter can't share one property declaration with two different nullabilities, so
        // the non-generic interface gets its own explicit implementation instead.
        object? IList.this[int index]
        {
            get => this[index];
            set => this[index] = value!;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        /// <summary>
        /// Called when [collection changed].
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="oldItem">The old item.</param>
        /// <param name="newItem">The new item.</param>
        /// <param name="oldIndex">The old index.</param>
        /// <param name="newIndex">The new index.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, object? oldItem = null, object? newItem = null, int? oldIndex = null, int? newIndex = null)

        {
            if (CollectionChanged != null)
            {
                switch (action)
                {
                    case NotifyCollectionChangedAction.Add:
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, newItem, newIndex.GetValueOrDefault()));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, oldItem, oldIndex.GetValueOrDefault()));
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, oldItem, newItem, oldIndex.GetValueOrDefault()));
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        CollectionChanged(this,new NotifyCollectionChangedEventArgs(action));
                        break;
                }
            }

            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    OnPropertyChanged("Count");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    OnPropertyChanged("Count");
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    OnPropertyChanged("Count");
                    break;
            }
        }

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        dynamic IDictionary<string, object>.this[string key]
        {

            get => _dictionary[key];

            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "value is statically typed 'dynamic', so this call binds through the DLR binder even though SetProperty takes a plain object. " +
                "Can't carry [RequiresUnreferencedCode] itself: it implements IDictionary<TKey,TValue>'s indexer setter, which isn't annotated, and the two must match. The actionable warning belongs to the 'dynamic'-typed value the caller supplied.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Same reasoning as the IL2026 suppression on this member.")]
            set => SetProperty(key, value);
        }

        /// <summary>
        /// Determines whether the specified <see cref="List"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>
        /// <c>true</c> when both instances are views over the same backing stores - the same
        /// <see cref="IList{T}"/> of elements and the same dictionary of dynamic properties.
        /// </returns>
        /// <remarks>
        /// This is deliberately store identity, not content comparison: these types are mutable
        /// views over a store someone else owns, so two wrappers over one store are one value,
        /// while two stores that merely happen to hold equal data are not. Comparing content would
        /// also force a content-derived <see cref="GetHashCode"/> on a mutable type, which makes an
        /// instance unfindable in a hash container as soon as it is mutated.
        /// <para>
        /// This previously opened with <c>base.Equals(other)</c>, which resolves to
        /// <see cref="BaseDictionary.Equals(object)"/> - a method whose body type-tests against
        /// <c>typeof(Dictionary)</c> and so, for a <see cref="List"/>, compared the backing
        /// dictionary against the <see cref="List"/> itself and returned false. That made the whole
        /// method return false unconditionally and left the element comparison below it
        /// unreachable, even for two wrappers over one <see cref="IList{T}"/>. See issue #52.
        /// </para>
        /// </remarks>
        public bool Equals(List? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            // Compare the backing dictionary directly rather than through base.Equals(object):
            // both fields are protected, and going through the base method is what routed this
            // into the typeof(Dictionary) test that made it always false.
            //
            // ReferenceEquals, not Equals: these fields are interface-typed, so the concrete store
            // is whatever the caller passed. The static object.Equals dispatches virtually, so a
            // store type that overrides Equals with content semantics would silently turn this into
            // a content comparison - the exact thing this contract exists to avoid. The BCL
            // collections normally passed here do not override Equals, so this is the same result
            // for them; it only closes the gap for a store that does.
            return ReferenceEquals(other._dictionary, _dictionary) && ReferenceEquals(other._list, _list);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as List);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ _list.GetHashCode();
            }
        }


        /// <summary>
        /// Gets or sets the override getting item method names. USED for GetItemProperties
        /// </summary>
        /// <value>The override getting item method names.</value>
        public Func<IEnumerable<object>, IEnumerable<string>>? OverrideGettingItemMethodNames { get; set; }



        /// <summary>
        /// Gets the represented item. USED fOR GetItemProperties
        /// </summary>
        /// <returns></returns>
        protected virtual dynamic? GetRepresentedItem()
        {
            var tItem = ((IEnumerable<object>)this).FirstOrDefault();
            return tItem;
        }


        #region Implementation of ICollection

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The index.</param>
        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }
        private readonly object _syncRoot = new object();


        /// <summary>
        /// Gets the sync root.
        /// </summary>
        /// <value>
        /// The sync root.
        /// </value>
        public object SyncRoot => _syncRoot;


        /// <summary>
        /// Gets a value indicating whether this instance is synchronized.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is synchronized; otherwise, <c>false</c>.
        /// </value>
        public bool IsSynchronized => false;

        #endregion

        #region Implementation of IList


        int IList.Add(object? value)
        {
            // Add(dynamic item) only implements ICollection<object>.Add(object item) (non-null);
            // IList.Add itself accepts null, and forwarding it unchecked is the pre-existing
            // behavior (whatever Add(dynamic) then does with a null item is unaffected by this).
            Add(value!);
            return Count - 1;
        }

        void IList.Remove(object? value)
        {
            Remove(value!);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is fixed size.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is fixed size; otherwise, <c>false</c>.
        /// </value>
        public bool IsFixedSize => false;

        #endregion
    }
}
