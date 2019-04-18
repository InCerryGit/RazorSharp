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

		const string pdb = @"C:\Users\Deci\CLionProjects\NativeSharp\cmake-build-debug\NativeSharp.pdb";
		
		[SymNamespace(pdb,"NativeSharp.dll")]
		struct MyStruct
		{
			
			[SymField]
			public int g_int;

			[Symcall]
			public void hello()
			{
				
			}
		}

		public static void Main(string[] args)
		{
//			ModuleInitializer.GlobalSetup();



			/*var dll = @"C:\Users\Deci\CLionProjects\NativeSharp\cmake-build-debug\NativeSharp.dll";
			var plib = Kernel32.LoadLibrary(dll);
			
			
			
			var ms = new MyStruct();
			Symload.Load(typeof(MyStruct), ms);
			Console.WriteLine(ms.g_int);
			
			
			Kernel32.FreeLibrary(plib);*/

			Console.WriteLine(typeof(string).GetMetaType());


//			ModuleInitializer.GlobalClose();
		}
	}
}