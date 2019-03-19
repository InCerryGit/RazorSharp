using System;
using System.Collections;
using System.Collections.Generic;
using RazorSharp.Memory;

namespace RazorSharp.Pointers
{
	// todo: WIP
	public struct AllocPointer<T> : IEnumerable<T>
	{
		private Pointer<T> m_ptr;

		public int Length {
			get { return AllocHelper.GetLength(m_ptr); }
			set { this = AllocHelper.ReAlloc(m_ptr, value); }
		}

		public int  Size        => AllocHelper.GetSize(m_ptr);
		public bool IsAllocated => AllocHelper.IsAllocated(m_ptr);

		public ref T this[int index] {
			get {
				var cpy = m_ptr + index;
				
				if (!Mem.IsAddressInRange(Limit.m_ptr.Address, cpy.Address, Origin.m_ptr.Address)) {
					throw new IndexOutOfRangeException();
				}

				return ref m_ptr[index];
			}
		}

		public Pointer<T> Pointer {
			get { return m_ptr; }
//			set { m_ptr = value; }
		}

		public AllocPointer<T> Limit {
			get { return AllocHelper.GetLimit(m_ptr); }
		}

		public AllocPointer<T> Origin {
			get { return AllocHelper.GetOrigin(m_ptr); }
		}

		public int Offset {
			get { return AllocHelper.GetOffset(m_ptr); }
		}

		public static AllocPointer<T> operator ++(AllocPointer<T> ptr)
		{
			if (ptr.m_ptr + 1 >= ptr.Limit.m_ptr) {
				return ptr;
			}
			else {
				ptr.m_ptr++;
				return ptr;
			}
			
		}


		public void Clear()
		{
			Origin.m_ptr.Zero(Length);
		}

		public void Free()
		{
//			Clear();
			AllocHelper.Free(m_ptr);
			m_ptr = null;
		}

		public AllocPointer(Pointer<T> ptr)
		{
			m_ptr = ptr;
		}

		public static implicit operator AllocPointer<T>(Pointer<T> ptr)
		{
			return new AllocPointer<T>(ptr);
		}


		public T[] ToArray()
		{
			return m_ptr.CopyOut(Length);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return m_ptr.GetEnumerator(Length);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public override string ToString()
		{
			return Pointer.ToTable(Length).ToMarkDownString();
		}
	}
}