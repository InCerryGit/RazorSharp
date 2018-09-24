using System;
using System.Diagnostics;
using NUnit.Framework;
using RazorSharp.CLR;
using RazorSharp.CLR.Structures;
using RazorSharp.Memory;
using RazorSharp.Pointers;

namespace Test.Testing.Tests
{

	[TestFixture]
	public class AddrInRangeTests
	{


		[Test]
		public void Test()
		{
			Debug.Assert(Mem.IsAddressInRange(Mem.StackBase, Mem.StackLimit, Mem.StackLimit));
			Debug.Assert(Mem.IsAddressInRange(GCHeap.HighestAddress, GCHeap.LowestAddress, GCHeap.LowestAddress));
			int[]        rg    = {1, 2, 3, 4, 5};
			Pointer<int> rgPtr = Mem.AllocUnmanaged<int>(5);
			rgPtr.Init(1, 2, 3, 4, 5);

			Debug.Assert(rgPtr.Contains(1, 5));

			Mem.Free(rgPtr.Address);

			IntPtr orig = rgPtr.Address;

			for (int i = 0; i < 5; i++) {
				Debug.Assert(InRange(rgPtr.Address));
				rgPtr++;
			}


			Debug.Assert(!InRange(rgPtr.Address));


			bool InRange(IntPtr p)
			{
				return Mem.IsAddressInRange(PointerUtils.Offset<int>(orig, 5), p, orig);
			}
		}
	}

}