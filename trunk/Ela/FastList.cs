﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Ela
{
	public class FastList<T> : IEnumerable<T>
	{
		#region Construction
		internal static readonly FastList<T> Empty = new FastList<T>();
		private T[] array;
		private const int DEFAULT_SIZE = 4;
		private int size;
		private int initialSize;

		public FastList() : this(DEFAULT_SIZE)
		{

		}


		internal FastList(int size)
		{
			this.initialSize = size;
			array = new T[size];
		}


		internal FastList(FastList<T> list)
		{
			this.initialSize = list.initialSize;
			array = (T[])list.array.Clone();
			size = list.size;
		}
		#endregion


		#region Methods
		public IEnumerator<T> GetEnumerator()
		{
			for (var i = 0; i < size; i++)
				yield return array[i];
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}


		public FastList<T> Clone()
		{
			var list = new FastList<T>();
			list.array = (T[])array.Clone();
			list.size = size;
			list.initialSize = initialSize;
			return list;
		}


		public T[] ToArray()
		{
			var arr = new T[size];
			Array.Copy(array, arr, size);
			return arr;
		}


		internal T[] GetRawArray()
		{
			return array;
		}


		internal void Clear()
		{
			size = 0;
			array = new T[initialSize];
		}


		internal void AddRange(IEnumerable<T> seq)
		{
			foreach (var s in seq)
				Add(s);
		}


		internal void Add(T val)
		{
			if (size == array.Length)
			{
				var dest = new T[array.Length == 0 ? DEFAULT_SIZE : array.Length * 2];
				Array.Copy(array, 0, dest, 0, size);
				array = dest;
			}

			array[size++] = val;
		}


		internal bool Remove(T val)
		{
			var index = Array.IndexOf(array, val);

			if (index >= 0)
			{
				size--;
				
				if (index < size)
					Array.Copy(array, index + 1, array, index, size - index);
				
				array[size] = default(T);
				return true;
			}
			else
				return false;
		}


		internal void Trim()
		{
			if (array.Length > size)
			{
				var newArr = new T[size];
				Array.Copy(array, newArr, size);
				array = newArr;
			}
		}
		#endregion


		#region Properties
		public int Count
		{
			get { return size; }
		}


		public T this[int index]
		{
			get { return array[index]; }
			internal set { array[index] = value; }
		}
		#endregion
	}
}