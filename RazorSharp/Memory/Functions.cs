#region

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using RazorSharp.CoreClr;
using RazorSharp.CoreClr.Structures;
using RazorSharp.Memory.Extern.Signatures;
using RazorSharp.Memory.Extern.Signatures.Attributes;
using RazorSharp.Memory.Extern.Symbols;
using RazorSharp.Memory.Extern.Symbols.Attributes;
using RazorSharp.Memory.Pointers;
using RazorSharp.Native.Win32;

#endregion

namespace RazorSharp.Memory
{
	/// <summary>
	///     Methods of finding and executing DLL functions:
	///     <para>
	///         1. Sig scanning (<see cref="SignatureCall" />) (<see cref="Memory.MemScanner" />) (
	///         <see cref="SigcallAttribute" />)
	///     </para>
	///     <list type="bullet">
	///         <item>
	///             <description>Runtime</description>
	///         </item>
	///         <item>
	///             <description>If the function is not DLL-exported</description>
	///         </item>
	///         <item>
	///             <description>Requirements: byte signature of the function</description>
	///         </item>
	///     </list>
	///     <para>2. <see cref="DllImportAttribute" /> attribute</para>
	///     <list type="bullet">
	///         <item>
	///             <description>Compile-time</description>
	///         </item>
	///         <item>
	///             <description>Requirements: function is DLL-exported</description>
	///         </item>
	///     </list>
	///     <para>3. <see cref="GetFunction{TDelegate}" /></para>
	///     <list type="bullet">
	///         <item>
	///             <description>Runtime</description>
	///         </item>
	///         <item>
	///             <description>Requirements: function is DLL-exported</description>
	///         </item>
	///     </list>
	///     <para>4. <see cref="ProcessApi.GetProcAddress" /></para>
	///     <list type="bullet">
	///         <item>
	///             <description>Runtime</description>
	///         </item>
	///         <item>
	///             <description>Requirements: function is DLL-exported</description>
	///         </item>
	///     </list>
	///     <para>5. <see cref="Symload" /> (<see cref="SymcallAttribute" />)</para>
	///     <list type="bullet">
	///         <item>
	///             <description>Runtime</description>
	///         </item>
	///         <item>
	///             <description>If the function is not DLL-exported</description>
	///         </item>
	///         <item>
	///             <description>Requirements: symbol/name of the function</description>
	///         </item>
	///     </list>
	/// </summary>
	public static unsafe class Functions
	{
		static Functions()
		{
			const string SET_ENTRY_POINT = "MethodDesc::SetStableEntryPointInterlocked";
			SetEntryPoint = Runtime.GetClrFunction<SetEntryPointDelegate>(SET_ENTRY_POINT);

			/*const string GET_DELEGATE = "GetDelegateForFunctionPointerInternal";
			GetDelegate = (GetDelegateDelegate) typeof(Marshal)
			                                   .GetAnyMethod(GET_DELEGATE)
			                                   .CreateDelegate(typeof(GetDelegateDelegate));*/
		}

		/// <summary>
		/// Gets an exported function
		/// </summary>
		public static TDelegate GetFunction<TDelegate>(string dllName, string fn) where TDelegate : Delegate
		{
			var hModule = ProcessApi.GetModuleHandle(dllName);
			var hFn     = ProcessApi.GetProcAddress(hModule, fn);
			return GetDelegateForFunctionPointer<TDelegate>(hFn);
		}

		#region Set entry point

		/// <summary>
		///     We implement <see cref="SetEntryPointDelegate" /> as a <see cref="Delegate" /> initially because
		///     <see cref="MethodDesc.SetStableEntryPointInterlocked" /> has not been bound yet, and in order to bind
		///     it we have to use this function.
		/// </summary>
		/// <param name="value"><c>this</c> pointer of a <see cref="MethodDesc" /></param>
		/// <param name="pCode">Entry point</param>
		private delegate long SetEntryPointDelegate(MethodDesc* value, ulong pCode);

		private static readonly SetEntryPointDelegate SetEntryPoint;


		/// <summary>
		///     <remarks>
		///         Equal to <see cref="MethodDesc.SetStableEntryPoint" />, but this is implemented via a <see cref="Delegate" />
		///     </remarks>
		/// </summary>
		public static void SetStableEntryPoint(MethodInfo mi, IntPtr pCode)
		{
			var  pMd    = (MethodDesc*) mi.MethodHandle.Value;
			long result = SetEntryPoint(pMd, (ulong) pCode);
			if (!(result > 0)) {
				Global.Log.Warning("Could not set entry point for {Method}", mi.Name);
			}

			//Conditions.Assert(result >0);
		}

		#endregion

		#region Get delegate

		public static TDelegate GetDelegateForFunctionPointer<TDelegate>(Pointer<byte> ptr) where TDelegate : Delegate
		{
			return (TDelegate) GetDelegateForFunctionPointer(ptr, typeof(TDelegate));
		}

		public static Delegate GetDelegateForFunctionPointer(Pointer<byte> ptr, Type t)
		{
//			Conditions.NotNull(GetDelegate, nameof(GetDelegate));
//			return GetDelegate(ptr, t);

			return Marshal.GetDelegateForFunctionPointer(ptr.Address, t);
		}

		public static void Swap(MethodInfo dest, MethodInfo src)
		{
			var srcCode = src.MethodHandle.GetFunctionPointer();
			SetStableEntryPoint(dest, srcCode);
		}

		#endregion
	}
}