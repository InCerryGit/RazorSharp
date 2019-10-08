using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RazorSharp.Core;
using RazorSharp.CoreClr.Meta;
using RazorSharp.Import;
using RazorSharp.Import.Attributes;
using RazorSharp.Import.Enums;
using RazorSharp.Memory;
using RazorSharp.Memory.Pointers;

namespace RazorSharp.Interop.Utilities
{
	/// <summary>
	/// Provides utilities for creating delegates from function pointers.
	/// </summary>
	[ImportNamespace]
	public static unsafe class DelegateCreator
	{
		static DelegateCreator()
		{
			ImportManager.Value.Load(typeof(DelegateCreator), Clr.Value.Imports);
		}

		[ImportMapDesignation]
		private static readonly ImportMap Imports = new ImportMap();
		
		[ImportForwardCall("COMDelegate", nameof(ConvertToDelegate), ImportCallOptions.Map)]
		private static void* ConvertToDelegate(void* fn, void* mt)
		{
			return Functions.Native.CallReturnPointer((void*) Imports[nameof(ConvertToDelegate)], 
			                                         fn, mt);
		}
		
		public static Delegate CreateDelegateSafe(Pointer<byte> ptr, Type t)
		{
			return Marshal.GetDelegateForFunctionPointer(ptr.Address, t);
		}

		public static TDelegate CreateDelegateSafe<TDelegate>(Pointer<byte> ptr)
		{
			return Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr.Address);
		}

		/// <summary>
		///     Creates a <see cref="Delegate" /> from a function pointer.
		/// <remarks>
		/// Similar to <see cref="Marshal.GetDelegateForFunctionPointer"/>
		/// </remarks>
		/// </summary>
		/// <param name="ptr">Function pointer</param>
		/// <param name="t"><see cref="Delegate" /> type</param>
		/// <returns>A <see cref="Delegate" /> from <paramref name="ptr" /></returns>
		public static Delegate CreateDelegate(Pointer<byte> ptr, Type t)
		{
			MetaType mt = t;
			return Converter.ToObject<Delegate>(ConvertToDelegate(ptr.ToPointer(), mt.NativePointer));
		}

		/// <summary>
		///     Creates a <see cref="Delegate" /> from a function pointer.
		/// <remarks>
		/// Similar to <see cref="Marshal.GetDelegateForFunctionPointer{TDelegate}"/>
		/// </remarks>
		/// </summary>
		/// <param name="ptr">Function pointer</param>
		/// <typeparam name="TDelegate"><see cref="Delegate" /> type</typeparam>
		/// <returns>A <see cref="Delegate" /> from <paramref name="ptr" /></returns>
		public static TDelegate CreateDelegate<TDelegate>(Pointer<byte> ptr) where TDelegate : Delegate
		{
			return (TDelegate) DelegateCreator.CreateDelegate(ptr, typeof(TDelegate));
		}
	}
}