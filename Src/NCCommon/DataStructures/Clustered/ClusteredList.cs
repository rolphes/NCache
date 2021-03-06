﻿// ==++==
// 
//   Copyright (c). 2015. Microsoft Corporation.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ==--==
/*============================================================
**
** Class:  ClusteredList
** 
** <OWNER>[....]</OWNER>
**
** Purpose: Implements a generic, dynamically sized clustered
** list. 
**
** 
===========================================================*/

using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
#if DEBUG
using System.Diagnostics.Contracts;
#endif
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Runtime;

namespace Alachisoft.NCache.Common.DataStructures.Clustered
{
    // Implements a variable-size List that uses an array of objects to store the
    // elements. A List has a capacity, which is the allocated length
    // of the internal array. As elements are added to a List, the capacity
    // of the List is automatically increased as required by reallocating the
    // internal array.
    // 
#if DEBUG
    [DebuggerDisplay("Count = {Count}")]
#endif
    [Serializable]
    public class ClusteredList<T> : IList<T>, System.Collections.IList, ICloneable
    {
        private const int _defaultCapacity = 4;

        private ClusteredArray<T> _items;
#if DEBUG
        [ContractPublicPropertyName("Count")]
#endif
        private int _size;
        private int _version;
        [NonSerialized]
        private Object _syncRoot;

        static readonly ClusteredArray<T> _emptyArray = new ClusteredArray<T>(0);

        // Constructs a List. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to 16, and then increased in multiples of two as required.
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public ClusteredList()
        {
            _items = _emptyArray;
        }

        // Constructs a List with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        // 
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public ClusteredList(int capacity)
        {
            if (capacity < 0) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
#if DEBUG
            Contract.EndContractBlock();
#endif
            if (capacity == 0)
                _items = _emptyArray;
            else
                _items = new ClusteredArray<T>(capacity);
        }

        // Constructs a List, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        // 
        public ClusteredList(IEnumerable<T> collection)
        {
            if (collection == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
#if DEBUG
            Contract.EndContractBlock();
#endif
            ICollection<T> c = collection as ICollection<T>;
            if (c != null)
            {
                int count = c.Count;
                if (count == 0)
                {
                    _items = _emptyArray;
                }
                else
                {
                    _items = new ClusteredArray<T>(count);
                    AddRange(c);
                }
            }
            else
            {
                _size = 0;
                _items = _emptyArray;
                // This enumerable could be empty.  Let Add allocate a new array, if needed.
                // Note it will also go to _defaultCapacity first, not 1, then 2, etc.

                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Add(en.Current);
                    }
                }
            }
        }

        // Gets and sets the capacity of this list.  The capacity is the size of
        // the internal array used to hold items.  When set, the internal 
        // array of the list is reallocated to the given capacity.
        // 
        public int Capacity
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            get
            {
#if DEBUG
                Contract.Ensures(Contract.Result<int>() >= 0);
#endif
                return _items.Length;
            }
            set
            {
                if (value < _size)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_SmallCapacity);
                }
#if DEBUG
                Contract.EndContractBlock();
#endif
                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        ClusteredArray<T> array = new ClusteredArray<T>(value);
                        if (this._size > 0)
                        {
                            ClusteredArray<T>.Copy(this._items, 0, array, 0, this._size);
                        }
                        this._items = array;
                        return;
                    }
                    else
                    {
                        this._items = new ClusteredArray<T>(_defaultCapacity);
                    }

                }
            }
        }

        // Read-only property describing how many elements are in the List.
        public int Count
        {
            get
            {
#if DEBUG
                Contract.Ensures(Contract.Result<int>() >= 0);
#endif
                return _size;
            }
        }

        bool System.Collections.IList.IsFixedSize
        {
            get { return false; }
        }


        // Is this List read-only?
        bool ICollection<T>.IsReadOnly
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            get { return false; }
        }

        bool System.Collections.IList.IsReadOnly
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            get { return false; }
        }

        // Is this List synchronized (thread-safe)?
        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        // Synchronization root for this object.
        Object System.Collections.ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }
        // Sets or Gets the element at the given index.
        // 
        public T this[int index]
        {
#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)_size)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                }
#if DEBUG
                Contract.EndContractBlock();
#endif
                return _items[index];
            }

#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            set
            {
                if ((uint)index >= (uint)_size)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                }
#if DEBUG
                Contract.EndContractBlock();
#endif
                _items[index] = value;
                _version++;
            }
        }

        private static bool IsCompatibleObject(object value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>. 
            return ((value is T) || (value == null && default(T) == null));
        }

        Object System.Collections.IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);

                try
                {
                    this[index] = (T)value;
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(T));
                }
            }
        }

        // Adds the given object to the end of this list. The size of the list is
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element.
        //
        public void Add(T item)
        {
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            _items[_size++] = item;
            _version++;
        }

        int System.Collections.IList.Add(Object item)
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, ExceptionArgument.item);

            try
            {
                Add((T)item);
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));
            }

            return Count - 1;
        }


        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public void AddRange(IEnumerable<T> collection)
        {
#if DEBUG
            Contract.Ensures(Count >= Contract.OldValue(Count));
#endif
            InsertRange(_size, collection);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
#if DEBUG
            Contract.Ensures(Contract.Result<ReadOnlyCollection<T>>() != null);
#endif
            return new ReadOnlyCollection<T>(this);
        }

        //   Usman: Search requires sort and sort is a bit problem at the moment, given our whole clustered array scenario.

        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search.
        // 
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            throw new NotImplementedException("The structure doesn't support internal binary search.");
        }

        public int BinarySearch(T item)
        {
            throw new NotImplementedException("The structure doesn't support internal binary search.");
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            throw new NotImplementedException("The structure doesn't support internal binary search.");
        }


        // Clears the contents of List.

        public void Clear()
        {
            if (_size > 0)
            {
                ClusteredArray<T>.Clear(_items, 0, _size); // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
                _size = 0;
            }
            _version++;
        }




        // Contains returns true if the specified element is in the ArrayList.
        // It does a linear, O(n) search.  Equality is determined by calling
        // item.Equals().
        //
        // Changelog:
        // Usman: Optimized for clustered array through its internal calls.
        public bool Contains(T item)
        {
            int i = _items.IndexOf(item);
            if (i < 0 || i >= _size)
                return false;
            return true;
        }

        bool System.Collections.IList.Contains(Object item)
        {
            if (IsCompatibleObject(item))
            {
                return Contains((T)item);
            }
            return false;
        }

        public ClusteredList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.converter);
            }
            // @

#if DEBUG
            Contract.EndContractBlock();
#endif
            ClusteredList<TOutput> list = new ClusteredList<TOutput>(_size);
            for (int i = 0; i < _size; i++)
            {
                list._items[i] = converter(_items[i]);
            }
            list._size = _size;
            return list;
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        void System.Collections.ICollection.CopyTo(Array array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }
#if DEBUG
            Contract.EndContractBlock();
#endif
            try
            {
                _items.CopyTo(array, arrayIndex, 0, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
            }
        }

        // Copies a section of this list to the given array at the given index.
        // 
        // The method uses the Array.Copy method to copy the elements.
        // 
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (_size - index < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }
#if DEBUG
            Contract.EndContractBlock();
#endif
            // Delegate rest of error checking to Array.Copy.
            _items.CopyTo(array, arrayIndex, index, count);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // Delegate rest of error checking to Array.Copy.
            _items.CopyTo(array, arrayIndex, 0, _size);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the currect capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.
        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > _items.MaxArrayLength) newCapacity = _items.MaxArrayLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

#if FEATURE_LIST_PREDICATES || FEATURE_NETCORE
        //public bool Exists(Predicate<T> match) {
        //    return FindIndex(match) != -1;
        //}

        //public T Find(Predicate<T> match) {
        //    if( match == null) {
        //        ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
        //    }
        //    Contract.EndContractBlock();

        //    for(int i = 0 ; i < _size; i++) {
        //        if(match(_items[i])) {
        //            return _items[i];
        //        }
        //    }
        //    return default(T);
        //}
  
        //public List<T> FindAll(Predicate<T> match) { 
        //    if( match == null) {
        //        ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
        //    }
        //    Contract.EndContractBlock();

        //    List<T> list = new List<T>(); 
        //    for(int i = 0 ; i < _size; i++) {
        //        if(match(_items[i])) {
        //            list.Add(_items[i]);
        //        }
        //    }
        //    return list;
        //}
  
        //public int FindIndex(Predicate<T> match) {
        //    Contract.Ensures(Contract.Result<int>() >= -1);
        //    Contract.Ensures(Contract.Result<int>() < Count);
        //    return FindIndex(0, _size, match);
        //}
  
        //public int FindIndex(int startIndex, Predicate<T> match) {
        //    Contract.Ensures(Contract.Result<int>() >= -1);
        //    Contract.Ensures(Contract.Result<int>() < startIndex + Count);
        //    return FindIndex(startIndex, _size - startIndex, match);
        //}
 
        //public int FindIndex(int startIndex, int count, Predicate<T> match) {
        //    if( (uint)startIndex > (uint)_size ) {
        //        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);                
        //    }

        //    if (count < 0 || startIndex > _size - count) {
        //        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
        //    }

        //    if( match == null) {
        //        ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
        //    }
        //    Contract.Ensures(Contract.Result<int>() >= -1);
        //    Contract.Ensures(Contract.Result<int>() < startIndex + count);
        //    Contract.EndContractBlock();

        //    int endIndex = startIndex + count;
        //    for( int i = startIndex; i < endIndex; i++) {
        //        if( match(_items[i])) return i;
        //    }
        //    return -1;
        //}
 
        //public T FindLast(Predicate<T> match) {
        //    if( match == null) {
        //        ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
        //    }
        //    Contract.EndContractBlock();

        //    for(int i = _size - 1 ; i >= 0; i--) {
        //        if(match(_items[i])) {
        //            return _items[i];
        //        }
        //    }
        //    return default(T);
        //}

        //public int FindLastIndex(Predicate<T> match) {
        //    Contract.Ensures(Contract.Result<int>() >= -1);
        //    Contract.Ensures(Contract.Result<int>() < Count);
        //    return FindLastIndex(_size - 1, _size, match);
        //}
   
        //public int FindLastIndex(int startIndex, Predicate<T> match) {
        //    Contract.Ensures(Contract.Result<int>() >= -1);
        //    Contract.Ensures(Contract.Result<int>() <= startIndex);
        //    return FindLastIndex(startIndex, startIndex + 1, match);
        //}

        //public int FindLastIndex(int startIndex, int count, Predicate<T> match) {
        //    if( match == null) {
        //        ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
        //    }
        //    Contract.Ensures(Contract.Result<int>() >= -1);
        //    Contract.Ensures(Contract.Result<int>() <= startIndex);
        //    Contract.EndContractBlock();

        //    if(_size == 0) {
        //        // Special case for 0 length List
        //        if( startIndex != -1) {
        //            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
        //        }
        //    }
        //    else {
        //        // Make sure we're not out of range            
        //        if ( (uint)startIndex >= (uint)_size) {
        //            ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
        //        }
        //    }
            
        //    // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
        //    if (count < 0 || startIndex - count + 1 < 0) {
        //        ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
        //    }
                        
        //    int endIndex = startIndex - count;
        //    for( int i = startIndex; i > endIndex; i--) {
        //        if( match(_items[i])) {
        //            return i;
        //        }
        //    }
        //    return -1;
        //}
#endif // FEATURE_LIST_PREDICATES || FEATURE_NETCORE

        public void ForEach(Action<T> action)
        {
            if (action == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }
#if DEBUG
            Contract.EndContractBlock();
#endif
            int version = _version;

            for (int i = 0; i < _size; i++)
            {
                if (version != _version && BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    break;
                }
                action(_items[i]);
            }

            if (version != _version && BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        //
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public ClusteredListEnumerator GetEnumerator()
        {
            return new ClusteredListEnumerator(this);
        }

        /// <internalonly/>
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new ClusteredListEnumerator(this);
        }

#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ClusteredListEnumerator(this);
        }

        public ClusteredList<T> GetRange(int index, int count)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            }
#if DEBUG
            Contract.Ensures(Contract.Result<ClusteredList<T>>() != null);
            Contract.EndContractBlock();
#endif
            ClusteredList<T> list = new ClusteredList<T>(count);
            ClusteredArray<T>.Copy(_items, index, list._items, 0, count);
            list._size = count;
            return list;
        }


        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public int IndexOf(T item)
        {
#if DEBUG
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
#endif
            int index = _items.IndexOf(item);
            if (index >= _size)
                index = -1;
            return index;
        }

#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        int System.Collections.IList.IndexOf(Object item)
        {
            if (IsCompatibleObject(item))
            {
                return IndexOf((T)item);
            }
            return -1;
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and ending at count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item, int index)
        {
            if (index > _size)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
#if DEBUG
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
#endif
            int found = _items.IndexOf(item);
            if (found < index)
                return -1;
            return found;
        }

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards, starting at index
        // index and upto count number of elements. The
        // elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item, int index, int count)
        {
            if (index > _size)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);

            if (count < 0 || index > _size - count) ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_Count);
#if DEBUG
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            Contract.EndContractBlock();
#endif
            int found = _items.IndexOf(item);
            if (found < index || found > index + count)
                return -1;
            return found;
        }

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        // 
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint)index > (uint)_size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_ListInsert);
            }
#if DEBUG
            Contract.EndContractBlock();
#endif
            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size)
            {
                ClusteredArray<T>.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
            _version++;
        }

        void System.Collections.IList.Insert(int index, Object item)
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, ExceptionArgument.item);

            try
            {
                Insert(index, (T)item);
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));
            }
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the List's size.
        //
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
            }

            if ((uint)index > (uint)_size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
            }
#if DEBUG
            Contract.EndContractBlock();
#endif
            ICollection<T> c = collection as ICollection<T>;
            if (c != null)
            {    // if collection is ICollection<T>
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_size + count);
                    if (index < _size)
                    {
                        ClusteredArray<T>.Copy(_items, index, _items, index + count, _size - index);
                    }

                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (this == c)
                    {
                        // Copy first part of _items to insert location
                        ClusteredArray<T>.Copy(_items, 0, _items, index, index);
                        // Copy last part of _items back to inserted location
                        ClusteredArray<T>.Copy(_items, index + count, _items, index * 2, _size - index);
                    }
                    else
                    {
                        T[] itemsToInsert = new T[count];
                        c.CopyTo(itemsToInsert, 0);
                        _items.CopyFrom(itemsToInsert, 0, index, count);
                    }
                    _size += count;
                }
            }
            else
            {
                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Insert(index++, en.Current);
                    }
                }
            }
            _version++;
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end 
        // and ending at the first element in the list. The elements of the list 
        // are compared to the given value using the Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item)
        {
#if DEBUG
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
#endif
            if (_size == 0)
            {  // Special case for empty list
                return -1;
            }
            else
            {
                return LastIndexOf(item, _size - 1, _size);
            }
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and ending at the first element in the list. The 
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index)
        {
            if (index >= _size)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_Index);
#if DEBUG
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(((Count == 0) && (Contract.Result<int>() == -1)) || ((Count > 0) && (Contract.Result<int>() <= index)));
            Contract.EndContractBlock();
#endif
            return LastIndexOf(item, index, index + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and upto count elements. The elements of
        // the list are compared to the given value using the Object.Equals
        // method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index, int count)
        {
            if ((Count != 0) && (index < 0))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if ((Count != 0) && (count < 0))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }
#if DEBUG
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(((Count == 0) && (Contract.Result<int>() == -1)) || ((Count > 0) && (Contract.Result<int>() <= index)));
            Contract.EndContractBlock();
#endif
            if (_size == 0)
            {  // Special case for empty list
                return -1;
            }

            if (index >= _size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_BiggerThanCollection);
            }

            if (count > index + 1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_BiggerThanCollection);
            }

            int found = _items.LastIndexOf(item);
            if (found < index || found > index + count)
                return -1;
            return found;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
#if DEBUG
        [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0 && index < _size)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        void System.Collections.IList.Remove(Object item)
        {
            if (IsCompatibleObject(item))
            {
                Remove((T)item);
            }
        }

        // This method removes all items which matches the predicate.
        // The complexity is O(n).   
        public int RemoveAll(Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }
#if DEBUG
            Contract.Ensures(Contract.Result<int>() >= 0);
            Contract.Ensures(Contract.Result<int>() <= Contract.OldValue(Count));
            Contract.EndContractBlock();
#endif
            int freeIndex = 0;   // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < _size && !match(_items[freeIndex])) freeIndex++;
            if (freeIndex >= _size) return 0;

            int current = freeIndex + 1;
            while (current < _size)
            {
                // Find the first item which needs to be kept.
                while (current < _size && match(_items[current])) current++;

                if (current < _size)
                {
                    // copy item to the free slot.
                    _items[freeIndex++] = _items[current++];
                }
            }

            ClusteredArray<T>.Clear(_items, freeIndex, _size - freeIndex);
            int result = _size - freeIndex;
            _size = freeIndex;
            _version++;
            return result;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public void RemoveAt(int index)
        {
            //if ((uint)index > (uint)_size)
            //{
            //    ThrowHelper.ThrowArgumentOutOfRangeException();
            //}
#if DEBUG
            Contract.EndContractBlock();
#endif
            _size--;
            if (index < _size)
            {
                ClusteredArray<T>.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T);
            _version++;
        }

        // Removes a range of elements from this list.
        // 
        public void RemoveRange(int index, int count)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_size - index < count)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
#if DEBUG
            Contract.EndContractBlock();
#endif
            if (count > 0)
            {
                int i = _size;
                _size -= count;
                if (index < _size)
                {
                    ClusteredArray<T>.Copy(_items, index + count, _items, index, _size - index);
                }
                ClusteredArray<T>.Clear(_items, _size, count);
                _version++;
            }
        }


        // Usman: Not sure if reversal is necessary

        //Reverses the elements in this list.
        public void Reverse()
        {
            throw new NotImplementedException("The structure does not support reversal. ");
        }

        //Reverses the elements in a range of this list. Following a call to this
        //method, an element in the range given by index and count
        //which was previously located at index i will now be located at
        //index index + (index + count - i - 1).

        //This method uses the Array.Reverse method to reverse the
        //elements.

        public void Reverse(int index, int count)
        {
            throw new NotImplementedException("This structure does not support internal sorting. ");
        }



        //Usman: Need an algorithm to sort clustered data.

        // Sorts the elements in this list.  Uses the default comparer and 
        // Array.Sort.
        public void Sort()
        {
            throw new NotImplementedException("This structure does not support internal sorting. ");
        }

        // Sorts the elements in this list.  Uses Array.Sort with the
        // provided comparer.
        public void Sort(IComparer<T> comparer)
        {
            throw new NotImplementedException("This structure does not support internal sorting. ");
        }

        // Sorts the elements in a section of this list. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented by all
        // elements of the list.
        // 
        // This method uses the Array.Sort method to sort the elements.
        // 
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            throw new NotImplementedException("This structure does not support internal sorting. ");
        }

        public void Sort(Comparison<T> comparison)
        {
            throw new NotImplementedException("This structure does not support internal sorting. ");
        }

        //ToArray returns a new Object array containing the contents of the List.
        //This requires copying the List, which is an O(n) operation.


        public T[] ToArray()
        {
#if DEBUG
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == Count);
#endif
            T[] array = new T[_size];
            _items.CopyTo(array, 0, 0, _size);
            return array;
        }

        public T[][] ToInternalArray()
        {
            return _items.ToArray();
        }

        // Sets the capacity of this list to the size of the list. This method can
        // be used to minimize a list's memory overhead once it is known that no
        // new elements will be added to the list. To completely clear a list and
        // release all memory referenced by the list, execute the following
        // statements:
        // 
        // list.Clear();
        // list.TrimExcess();
        // 
        public void TrimExcess()
        {
            int threshold = (int)(((double)_items.Length) * 0.9);
            if (_size < threshold)
            {
                Capacity = _size;
            }
        }

#if FEATURE_LIST_PREDICATES || FEATURE_NETCORE
        //public bool TrueForAll(Predicate<T> match) {
        //    if( match == null) {
        //        ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
        //    }
        //    Contract.EndContractBlock();

        //    for(int i = 0 ; i < _size; i++) {
        //        if( !match(_items[i])) {
        //            return false;
        //        }
        //    }
        //    return true;
        //} 
#endif // FEATURE_LIST_PREDICATES || FEATURE_NETCORE

        internal static IList<T> Synchronized(ClusteredList<T> list)
        {
            return new SynchronizedClusteredList(list);
        }

        [Serializable()]
        internal class SynchronizedClusteredList : IList<T>
        {
            private ClusteredList<T> _list;
            private Object _root;

            internal SynchronizedClusteredList(ClusteredList<T> list)
            {
                _list = list;
                _root = ((System.Collections.ICollection)list).SyncRoot;
            }

            public int Count
            {
                get
                {
                    lock (_root)
                    {
                        return _list.Count;
                    }
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return ((ICollection<T>)_list).IsReadOnly;
                }
            }

            public void Add(T item)
            {
                lock (_root)
                {
                    _list.Add(item);
                }
            }

            public void Clear()
            {
                lock (_root)
                {
                    _list.Clear();
                }
            }

            public bool Contains(T item)
            {
                lock (_root)
                {
                    return _list.Contains(item);
                }
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                lock (_root)
                {
                    _list.CopyTo(array, arrayIndex);
                }
            }

            public bool Remove(T item)
            {
                lock (_root)
                {
                    return _list.Remove(item);
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                lock (_root)
                {
                    return _list.GetEnumerator();
                }
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                lock (_root)
                {
                    return ((IEnumerable<T>)_list).GetEnumerator();
                }
            }

            public T this[int index]
            {
                get
                {
                    lock (_root)
                    {
                        return _list[index];
                    }
                }
                set
                {
                    lock (_root)
                    {
                        _list[index] = value;
                    }
                }
            }

            public int IndexOf(T item)
            {
                lock (_root)
                {
                    return _list.IndexOf(item);
                }
            }

            public void Insert(int index, T item)
            {
                lock (_root)
                {
                    _list.Insert(index, item);
                }
            }

            public void RemoveAt(int index)
            {
                lock (_root)
                {
                    _list.RemoveAt(index);
                }
            }
        }

        [Serializable]
        public struct ClusteredListEnumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private ClusteredList<T> list;
            private int index;
            private int version;
            private T current;

            internal ClusteredListEnumerator(ClusteredList<T> list)
            {
                this.list = list;
                index = 0;
                version = list._version;
                current = default(T);
            }

#if DEBUG
            [TargetedPatchingOptOut("Performance critical to inline across NGen image boundaries")]
#endif
            public void Dispose()
            {
            }

            public bool MoveNext()
            {

                ClusteredList<T> localList = list;

                if (version == localList._version && ((uint)index < (uint)localList._size))
                {
                    current = localList._items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (version != list._version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                index = list._size + 1;
                current = default(T);
                return false;
            }

            public T Current
            {
                get
                {
                    return current;
                }
            }

            Object System.Collections.IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == list._size + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumOpCantHappen);
                    }
                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (version != list._version)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_EnumFailedVersion);
                }

                index = 0;
                current = default(T);
            }

        }

        public object Clone()
        {
            ClusteredList<T> clone = new ClusteredList<T>(this.Capacity);
            clone._items = (ClusteredArray<T>)this._items.Clone();
            clone._size = this._size;
            clone._version = this._version;
            return clone;
        }
    }
}
