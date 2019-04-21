﻿#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using InlineIL;
using RazorCommon;
using RazorCommon.Diagnostics;
using RazorCommon.Extensions;
using RazorCommon.Strings;
using RazorCommon.Utilities;
using RazorSharp;
using RazorSharp.CoreClr;
using RazorSharp.CoreClr.Structures;
using RazorSharp.CoreClr.Structures.ILMethods;
using RazorSharp.CoreJit;
using RazorSharp.Memory;
using RazorSharp.Memory.Extern.Symbols;
using RazorSharp.Memory.Extern.Symbols.Attributes;
using RazorSharp.Memory.Pointers;
using RazorSharp.Native;
using RazorSharp.Native.Symbols;
using RazorSharp.Native.Win32;
using RazorSharp.Utilities;
using CSUnsafe = System.Runtime.CompilerServices.Unsafe;
using Unsafe = RazorSharp.Memory.Unsafe;
using System.Net.Http;
using SharpPdb.Windows;
using SharpPdb.Windows.SymbolRecords;
using SharpUtilities;

#endregion


namespace Test
{
	#region

	using DWORD = UInt32;
	using Ptr = Pointer<byte>;

	#endregion


	public static unsafe class Program
	{
		// Common library: RazorCommon
		// Testing library: Sandbox


		private static void __Compile(Type t, string n)
		{
			RuntimeHelpers.PrepareMethod(t.GetAnyMethod(n).MethodHandle);
		}

		static string[] GetPathValues()
		{
			var raw = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Machine);
			Conditions.NotNull(raw, nameof(raw));
			return raw.Split(';');
		}

		const string pdb2 = @"C:\Users\Deci\CLionProjects\NativeSharp\cmake-build-debug\NativeSharp.pdb";
		const string dll  = @"C:\Users\Deci\CLionProjects\NativeSharp\cmake-build-debug\NativeSharp.dll";

		[SymNamespace(pdb2, "NativeSharp.dll")]
		private struct MyStruct
		{
			[SymField(UseMemberNameOnly = true)]
			public int g_int;

			[Symcall(UseMemberNameOnly = true)]
			public void hello() { }
		}


		private static void Cmp(string n)
		{
			Console.WriteLine("\n-- {0} -- ", n);

			var se     = SymbolEnvironment.Instance;
			var pdb    = new PdbSymbols(Clr.ClrPdb);
			var mi     = new ModuleInfo(Clr.ClrPdb, Clr.ClrModule, SymbolRetrievalMode.PdbReader);
			var txtseg = pdb.File.DbiStream.SectionHeaders.First(f => f.Name == ".text");


			var realAddr = (long) txtseg.VirtualAddress + Clr.ClrModule.BaseAddress.ToInt64();
			//Console.WriteLine("Pdb txt seg {0:X}", realAddr);
			//Console.WriteLine("Kernel txt seg {0:P}", Segments.GetSegment(".text").SectionAddress);

			//Console.WriteLine("possible addr {0:X}", realAddr+pdb.GetSymOffset2(n));
			Console.WriteLine("Kernel (addr: {0:P}) (ofs: {1:X}) (raw addr: {2:X})",
			                  Runtime.GetClrSymAddress(n),
			                  se.GetSymOffset(n),
			                  se.GetSymbol(n).Address);

			Console.WriteLine("Pdb (addr: {0:P}) (ofs: {1:X} raw ofs: {2:X})",
			                  mi.GetSymAddress(n),
			                  pdb.GetSymOffset(n),
			                  pdb.GetSymOffset2(n));

			Console.WriteLine("Delta: {0:X}", Math.Abs(pdb.GetSymOffset(n)-se.GetSymOffset(n)));


			var sym = pdb.GetSymbol(n);

			var sref = sym.SymbolStream.References[sym.SymbolStreamIndex];

			var    reader = sym.SymbolStream.Reader;
			ushort len    = sref.DataLen;
			uint   ofs    = sref.DataOffset;


			reader.Position = ofs;
//			Console.WriteLine(sym.Flags);
//			Console.WriteLine((PublicSymbolFlags) reader.ReadUint());
		}

		static readonly int[] Empty = new int[0];

		public static int[] Locate(this byte[] self, byte[] candidate)
		{
			if (IsEmptyLocate(self, candidate))
				return Empty;

			var list = new List<int>();

			for (int i = 0; i < self.Length; i++) {
				if (!IsMatch(self, i, candidate))
					continue;

				list.Add(i);
			}

			return list.Count == 0 ? Empty : list.ToArray();
		}

		static bool IsMatch(byte[] array, int position, byte[] candidate)
		{
			if (candidate.Length > (array.Length - position))
				return false;

			for (int i = 0; i < candidate.Length; i++)
				if (array[position + i] != candidate[i])
					return false;

			return true;
		}

		static bool IsEmptyLocate(byte[] array, byte[] candidate)
		{
			return array == null
			       || candidate == null
			       || array.Length == 0
			       || candidate.Length == 0
			       || candidate.Length > array.Length;
		}

		public static void Main(string[] args)
		{
			ModuleInitializer.GlobalSetup();


			Cmp("JIT_GetRuntimeType");
			Cmp("g_pGCHeap");
			Cmp("g_pStringClass");

			ModuleInitializer.GlobalClose();
		}
	}
}