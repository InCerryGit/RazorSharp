﻿#region

//using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using BenchmarkDotNet.Running;
using RazorSharp;
using RazorSharp.Analysis;
using RazorSharp.CLR;
using RazorSharp.CLR.Fixed;
using RazorSharp.CLR.Structures;
using RazorSharp.CLR.Structures.EE;
using RazorSharp.CLR.Structures.ILMethods;
using RazorSharp.Common;
using RazorSharp.Memory;
using RazorSharp.Pointers;
using RazorSharp.Utilities;
using Test.Testing.Benchmarking;
using Test.Testing.Types;
using static RazorSharp.Unsafe;
using Unsafe = RazorSharp.Unsafe;

#endregion

namespace Test
{

	#region

	using DWORD = UInt32;
	using CSUnsafe = System.Runtime.CompilerServices.Unsafe;

	#endregion

	/**
	 * RazorSharp
	 *
	 * History:
	 * 	- RazorSharp (deci-common-c)
	 * 	- RazorSharpNeue
	 * 	- RazorCLR
	 * 	- RazorSharp
	 *
	 * Notes:
	 *  - 32-bit is not fully supported
	 *  - Most types are probably not thread-safe
	 *
	 * Goals:
	 *  - Provide identical and better functionality of ClrMD, SOS, and Reflection
	 * 	  but in a faster and more efficient way
	 */
	internal static unsafe class Program
	{


#if DEBUG
		static Program()
		{
			StandardOut.ModConsole();

			/**
			 * RazorSharp is tested on and targets:
			 *
			 * - x64
			 * - Windows
			 * - .NET CLR 4.7.2
			 * - Workstation Concurrent GC
			 *
			 */
			RazorContract.Assert(IntPtr.Size == 8);
			RazorContract.Assert(Environment.Is64BitProcess);
			RazorContract.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

			/**
			 * 4.0.30319.42000
			 * The version we've been testing and targeting.
			 * Other versions will probably work but we're just making sure
			 * todo - determine compatibility
			 */
			RazorContract.Assert(Environment.Version.Major == 4);
			RazorContract.Assert(Environment.Version.Minor == 0);
			RazorContract.Assert(Environment.Version.Build == 30319);
			RazorContract.Assert(Environment.Version.Revision == 42000);

			RazorContract.Assert(!GCSettings.IsServerGC);
			bool isRunningOnMono = Type.GetType("Mono.Runtime") != null;
			RazorContract.Assert(!isRunningOnMono);

//			Logger.Log(Flags.Info, "Architecture: x64");
//			Logger.Log(Flags.Info, "Byte order: {0}", BitConverter.IsLittleEndian ? "Little Endian" : "Big Endian");
//			Logger.Log(Flags.Info, "CLR {0}", Environment.Version);
		}
#endif

		// todo: protect address-sensitive functions
		// todo: replace native pointers* with Pointer<T> for consistency
		// todo: RazorSharp, ClrMD, Reflection comparison
		// todo: Contract-oriented programming

		/**
		 * >> Entry point
		 */
		public static void Main(string[] args)
		{
			// todo: read module memory


			int          z      = 0xFF;
			Pointer<int> pInt32 = AddressOf(ref z);
			Console.WriteLine(pInt32);


			Pointer<byte> pNull = 0UL;
			Console.WriteLine(pNull);

			var ptrUnmanagedInstance = Mem.AllocUnmanagedInstance<Val>();
			Console.WriteLine(ptrUnmanagedInstance);

			Mem.Free(ptrUnmanagedInstance);


			const string  s      = "foo";
			Pointer<char> lpChar = Unsafe.AddressOfHeap(s).Reinterpret<char>();


			Debug.Assert(lpChar.IndexOf('f', HeapSize(s)) == 6);
			Debug.Assert(lpChar.Read<int>(lpChar.IndexOf(3, HeapSize(s))) == 3);
			Debug.Assert(lpChar.Contains(3, HeapSize(s)));
			Debug.Assert(lpChar.Contains('f', HeapSize(s)));

			lpChar.Add(RuntimeHelpers.OffsetToStringData);
			Console.WriteLine(lpChar);

			inline(lpChar, s);

			void inline(Pointer<char> pChar, string v)
			{
				for (int i = 0; i < v.Length; i++) {
					Debug.Assert(pChar[i] == v[i]);
					Debug.Assert(pChar.IndexOf(v[i], v.Length) == v.IndexOf(v[i]));
					Debug.Assert(pChar.Contains(v[i], v.Length) == v.Contains(v[i]));
				}
			}

			Console.WriteLine(lpChar.IndexOf('f', HeapSize(s)));
			Console.WriteLine(Collections.ToString(list: lpChar.CopyOut(s.Length)));

			int[]        rgx    = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
			Pointer<int> native = Mem.AllocUnmanaged<int>(10);
			native.Init(rgx);
			inlineAssert(native, rgx);

			Mem.Free(native);

			Pointer<char> pHChar = Marshal.StringToHGlobalUni("g");
			Console.WriteLine(pHChar);
			Mem.Free(pHChar);

			Pointer<byte> pHCharLpc = Marshal.StringToHGlobalAnsi("g");
			Console.WriteLine(pHCharLpc.ReadString(StringTypes.AnsiStr));
			Mem.Free(pHCharLpc);

			string        sz  = "foo";
			Pointer<char> ptr = Unsafe.AddressOfHeap(sz, OffsetType.StringData).Address;
			ptr[0] = 'b';
			Debug.Assert(sz[0] == 'b');

			Pointer<byte> nil = 0L;
			if (nil.TryRead(out int val32)) {
				Console.WriteLine(val32);
			}

			Pointer<Pointer<byte>> x = Unsafe.AddressOf(ref nil);


			Console.WriteLine(x);

			Pointer<string> lpAlloc = AllocPool.Alloc<string>();
			Console.WriteLine(lpAlloc);
			lpAlloc.Reference = "nil";
			Console.WriteLine(lpAlloc);
			AllocPool.Free(lpAlloc);
			Console.WriteLine(lpAlloc);



		}

		private static int add(int a, int b)
		{
			return a + b;
		}


		static void inlineAssert<T>(Pointer<T> pValue, IList<T> v)
		{
			Debug.Assert(pValue.SequenceEqual(v));
			for (int i = 0; i < v.Count; i++) {
				Debug.Assert(pValue[i].Equals(v[i]));
				Debug.Assert(pValue.Contains(v[i], v.Count) == v.Contains(v[i]));
				Debug.Assert(pValue.IndexOf(v[i], v.Count) == v.IndexOf(v[i]));
			}
		}


		private static Pointer<ILMethod> il(Type t, string name)
		{
			return Runtime.GetMethodDesc(t, name).Reference.GetILHeader();
		}


		private static void dmp<T>(ref T t) where T : class
		{
			ConsoleTable table = new ConsoleTable("Info", "Value");
			table.AddRow("Stack", Hex.ToHex(AddressOf(ref t).Address));
			table.AddRow("Heap", Hex.ToHex(AddressOfHeap(ref t).Address));
			table.AddRow("Size", AutoSizeOf(t));

			Console.WriteLine(table.ToMarkDownString());
		}

		private class Val : IComparable
		{
			private decimal d;

			public decimal val  { get; set; }
			public string  sval { get; set; }

			public int CompareTo(object obj)
			{
				throw new NotImplementedException();
			}

			public override string ToString()
			{
				return string.Format("{0}, {1}", val, sval);
			}
		}

		private struct Decimal512
		{
			private decimal a,
			                b,
			                c,
			                d;
		}

		[Conditional("DEBUG")]
		private static void @break()
		{
			Console.ReadLine();
		}


		#region todo

		/*static void Region()
		{
			var table   = new ConsoleTable("Region", "Address", "Size", "GC Segment", "GC Heap");
			var regions = GetRuntime().EnumerateMemoryRegions().OrderBy(x => x.Address).DistinctBy(x => x.Address);

			foreach (var region in regions) {
				table.AddRow(region.Type, Hex.ToHex(region.Address), region.Size,
					region.Type == ClrMemoryRegionType.GCSegment ? region.GCSegmentType.ToString() : "-",
					region.HeapNumber != -1 ? region.HeapNumber.ToString() : "-");
			}

			Console.WriteLine(table.ToMarkDownString());
		}

		static string AutoCreateFieldTable<T>(ref T t)
		{
			var table  = new ConsoleTable("Field", "Value");
			var fields = Runtime.GetFieldDescs<T>();
			foreach (var f in fields) {
				if (f.Reference.IsPointer)
					table.AddRow(f.Reference.Name, ReflectionUtil.GetPointerForPointerField(f, ref t).ToString("P"));
				else table.AddRow(f.Reference.Name, f.Reference.GetValue(t));
			}

			return table.ToMarkDownString();
		}

		private static ClrRuntime GetRuntime()
		{
			var dataTarget =
				DataTarget.AttachToProcess(Process.GetCurrentProcess().Id, UInt32.MaxValue, AttachFlag.Passive);
			return dataTarget.ClrVersions.Single().CreateRuntime();
		}*/

		private static TTo reinterpret_cast<TFrom, TTo>(TFrom tf)
		{
			return CSUnsafe.Read<TTo>(AddressOf(ref tf).ToPointer());
		}

		private static class WinDbg
		{
			public static void DumpObj<T>(ref T t)
			{
				Console.WriteLine(DumpObjInfo.Get(ref t));
			}

			private struct DumpObjInfo
			{
				private readonly string               m_szName;
				private readonly Pointer<MethodTable> m_pMT;
				private readonly Pointer<EEClass>     m_pEEClass;
				private readonly int                  m_cbSize;
				private readonly string               m_szStringValue;
				private readonly Pointer<FieldDesc>[] m_rgpFieldDescs;
				private          ConsoleTable         m_fieldTable;

				public static DumpObjInfo Get<T>(ref T t)
				{
					Pointer<MethodTable> mt = Runtime.ReadMethodTable(ref t);
					string               sz = t is string s ? s : "-";

					DumpObjInfo dump = new DumpObjInfo(mt.Reference.Name, mt, mt.Reference.EEClass, AutoSizeOf(t), sz,
						Runtime.GetFieldDescs<T>());
					dump.m_fieldTable = dump.FieldsTable(ref t);

					return dump;
				}


				private DumpObjInfo(string szName, Pointer<MethodTable> pMt, Pointer<EEClass> pEEClass,
					int cbSize, string szStringValue, Pointer<FieldDesc>[] rgpFieldDescs)
				{
					m_szName        = szName;
					m_pMT           = pMt;
					m_pEEClass      = pEEClass;
					m_cbSize        = cbSize;
					m_szStringValue = szStringValue;
					m_rgpFieldDescs = rgpFieldDescs;
					m_fieldTable    = null;
				}

				private ConsoleTable FieldsTable<T>(ref T t)
				{
					// A few differences:
					// - FieldInfo.Attributes is used for the Attr column; I don't know what WinDbg uses
					ConsoleTable table =
						new ConsoleTable("MT", "Field", "Offset", "Type", "VT", "Attr", "Value", "Name");
					foreach (Pointer<FieldDesc> v in m_rgpFieldDescs) {
						table.AddRow(
							Hex.ToHex(v.Reference.FieldMethodTable.Address),
							Hex.ToHex(v.Reference.Token),
							v.Reference.Offset,
							v.Reference.Info.FieldType.Name, v.Reference.Info.FieldType.IsValueType,
							v.Reference.Info.Attributes, v.Reference.GetValue(t),
							v.Reference.Name);
					}

					return table;
				}

				public override string ToString()
				{
					ConsoleTable table = new ConsoleTable("Attribute", "Value");
					table.AddRow("Name", m_szName);
					table.AddRow("MethodTable", Hex.ToHex(m_pMT.Address));
					table.AddRow("EEClass", Hex.ToHex(m_pEEClass.Address));
					table.AddRow("Size", String.Format("{0} ({1}) bytes", m_cbSize, Hex.ToHex(m_cbSize)));
					table.AddRow("String", m_szStringValue);

					return String.Format("{0}\nFields:\n{1}", table.ToMarkDownString(),
						m_fieldTable.ToMarkDownString());
				}
			}
		}


		private static void RunBenchmark<T>()
		{
			BenchmarkRunner.Run<T>();
		}

		private static void VmMap()
		{
			ConsoleTable table = new ConsoleTable("Low address", "High address", "Size");

			// Stack of current thread
			table.AddRow(Hex.ToHex(Mem.StackLimit), Hex.ToHex(Mem.StackBase),
				String.Format("{0} ({1} K)", Mem.StackSize, Mem.StackSize / Mem.BytesInKilobyte));
			Console.WriteLine(InspectorHelper.CreateLabelString("Stack:", table));

			table.Rows.RemoveAt(0);

			// GC heap
			table.AddRow(Hex.ToHex(GCHeap.LowestAddress), Hex.ToHex(GCHeap.HighestAddress),
				String.Format("{0} ({1} K)", GCHeap.Size, GCHeap.Size / Mem.BytesInKilobyte));
			Console.WriteLine(InspectorHelper.CreateLabelString("GC:", table));
		}

		private static T* AddrOf<T>(ref T t) where T : unmanaged
		{
			return (T*) AddressOf(ref t);
		}

		#endregion

// @formatter:off — disable formatter after this line
// @formatter:on — enable formatter after this line

		/**
		 * Dependencies:
		 *
		 * RazorSharp:
		 * 	- CompilerServices.Unsafe
		 *
		 * Test:
		 *  - CompilerServices.Unsafe
		 * 	- NUnit
		 *  - BenchmarkDotNet
		 *  - ClrMD
		 */

		/**
		 * Class this ptr:
		 *
		 * public IntPtr __this {
		 *		get {
		 *			var v = this;
		 *			var hThis = Unsafe.AddressOfHeap(ref v);
		 *			return hThis;
		 *		}
		 *	}
		 *
		 *
		 * Struct this ptr:
		 *
		 * public IntPtr __this {
		 *		get => Unsafe.AddressOf(ref this);
		 * }
		 */

		/**
		 * CLR										Used in										Equals
		 *
		 * MethodTable.BaseSize						Unsafe.BaseInstanceSize, Unsafe.HeapSize	-
		 * MethodTable.ComponentSize				Unsafe.HeapSize								-
		 * MethodTable.NumInstanceFieldBytes		Unsafe.BaseFieldsSize						-
		 * EEClass.m_cbNativeSize					Unsafe.NativeSize							Marshal.SizeOf, EEClassLayoutInfo.m_cbNativeSize
		 * EEClassLayoutInfo.m_cbNativeSize			-											Marshal.SizeOf, EEClass.m_cbNativeSize
		 * EEClassLayoutInfo.m_cbManagedSize		-											Unsafe.SizeOf, Unsafe.BaseFieldsSize (value types)
		 */


		/**
		 * #defines:
		 *
		 * FEATURE_COMINTEROP
		 * _TARGET_64BIT_
		 */

	}

}