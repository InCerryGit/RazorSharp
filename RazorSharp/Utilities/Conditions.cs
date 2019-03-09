#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using RazorSharp.Utilities.Exceptions;

#endregion

namespace RazorSharp.Utilities
{
	#region

	using AsrtCnd = AssertionConditionAttribute;
	using AsrtCndType = AssertionConditionType;

	#endregion

	/// <summary>
	///     Poor man's <see cref="System.Diagnostics.Contracts.Contract" /> because
	///     <see cref="System.Diagnostics.Contracts.Contract" /> is dead and doesn't work with JetBrains Rider
	/// </summary>
	internal static class Conditions
	{
		private const string COND_FALSE_HALT     = "cond:false => halt";
		private const string VALUE_NULL_HALT     = "value:null => halt";
		private const string STRING_FORMAT_PARAM = "msg";
		private const string NULLREF_EXCEPTION   = "value == null";


		internal static void CheckCompatibility()
		{
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

			/**
			 * RazorSharp is tested on and targets:
			 *
			 * - x64
			 * - Windows
			 * - .NET CLR 4.7.2
			 * - Workstation Concurrent GC
			 *
			 */
			//Requires64Bit();
			RequiresOS(OSPlatform.Windows);

			/**
			 * 4.0.30319.42000
			 * The version we've been testing and targeting.
			 * Other versions will probably work but we're just making sure
			 * todo - determine compatibility
			 */
			AssertAll(Environment.Version.Major == 4,
			          Environment.Version.Minor == 0,
			          Environment.Version.Build == 30319,
			          Environment.Version.Revision == 42000);

			Assert(!GCSettings.IsServerGC);
			RequiresDotNet();

			if (Debugger.IsAttached) {
				Global.Log.Warning("Debugging is enabled: some features may not work correctly");
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="notArray">
		///     <see cref="Action" /> to perform if <typeparamref name="TExpected" /> is <c>typeof(Array)</c>
		///     and <typeparamref name="TActual" /> is not <c>typeof(Array)</c>
		/// </param>
		/// <param name="notActual">
		///     <see cref="Action" /> to perform if <typeparamref name="TExpected" /> is not
		///     <typeparamref name="TActual" />
		/// </param>
		/// <typeparam name="TExpected">Expected <see cref="Type" /></typeparam>
		/// <typeparam name="TActual">Actual <see cref="Type" /></typeparam>
		private static void ResolveTypeAction<TExpected, TActual>(Action notArray, Action notActual)
		{
			if (typeof(TExpected) == typeof(Array)) {
				if (!typeof(TActual).IsArray) notArray();
			}
			else if (typeof(TExpected) != typeof(TActual)) {
				notActual();
			}
		}

		internal static bool AssertTypeEqual<TExpected, TActual>()
		{
			bool val = true;
			ResolveTypeAction<TExpected, TActual>(() => val = false, () => val = false);
			return val;
		}

		/// <summary>
		/// </summary>
		/// <param name="values"></param>
		/// <typeparam name="T"></typeparam>
		internal static void AssertEqual<T>(params T[] values)
		{
			if (values == null || values.Length == 0) return;

			Assert(values.All(v => v.Equals(values[0])));
		}

		#region Assert

		/// <summary>
		///     Checks for a condition; if <paramref name="cond" /> is <c>false</c>, displays the stack trace.
		/// </summary>
		/// <param name="cond">Condition to assert</param>
		/// <param name="msg">Message</param>
		/// <param name="args">Formatting elements</param>
		[AssertionMethod]
		[ContractAnnotation(COND_FALSE_HALT)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[StringFormatMethod(STRING_FORMAT_PARAM)]
		internal static void Assert([AsrtCnd(AsrtCndType.IS_TRUE)] bool cond, string msg, params object[] args)
		{
			Contract.Assert(cond, String.Format(msg, args));
		}

		/// <summary>
		///     Checks for a condition; if <paramref name="cond" /> is <c>false</c>, displays the stack trace.
		/// </summary>
		/// <param name="cond">Condition to assert</param>
		[AssertionMethod]
		[ContractAnnotation(COND_FALSE_HALT)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Assert([AsrtCnd(AsrtCndType.IS_TRUE)] bool cond)
		{
			Contract.Assert(cond);
		}


		internal static void AssertAllEqual<TSource, TResult>(Func<TSource, TResult> selector,
		                                                      IEnumerable<TSource>   values)
		{
			AssertAllEqual(values.Select(selector).ToArray());
		}


		internal static void AssertAllEqual<T>(T def, T[] values)
		{
			Assert(values.Any(o => o.Equals(def)));
		}

		internal static void AssertAllEqual<T>(T[] values)
		{
			AssertAllEqual(values[0], values);
		}

		internal static void AssertAll([AsrtCnd(AsrtCndType.IS_TRUE)] params bool[] conds)
		{
			foreach (bool b in conds) {
				Assert(b);
			}
		}

		#endregion

		#region Requires (precondition)

		internal static void RequiresDotNet()
		{
			bool isRunningOnMono = Type.GetType("Mono.Runtime") != null;
			Assert(!isRunningOnMono);
		}

		internal static void RequiresOS(OSPlatform os)
		{
			Assert(RuntimeInformation.IsOSPlatform(os), "OS type");
		}

		internal static void Requires64Bit()
		{
			Assert(IntPtr.Size == 8 && Environment.Is64BitProcess, "64-bit");
		}

		#region Not null

		private static void AssertNull(bool b)
		{
			Assert(b, "Null");
		}

		/// <summary>
		///     Specifies a precondition: checks to see if <paramref name="value" /> is <c>null</c>
		/// </summary>
		/// <param name="value">Pointer to check</param>
		[AssertionMethod]
		[ContractAnnotation(VALUE_NULL_HALT)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe void RequiresNotNull([AsrtCnd(AsrtCndType.IS_NOT_NULL)] void* value)
		{
			AssertNull(value != null);
		}

		/// <summary>
		///     Specifies a precondition: checks to see if <paramref name="value" /> is <see cref="IntPtr.Zero" />
		/// </summary>
		/// <param name="value"><see cref="IntPtr" /> to check</param>
		[AssertionMethod]
		[ContractAnnotation(VALUE_NULL_HALT)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RequiresNotNull(IntPtr value)
		{
			AssertNull(value != IntPtr.Zero);
		}

		/// <summary>
		///     Specifies a precondition: checks to see if <paramref name="value" /> is <c>null</c>
		///     <remarks>
		///         May cause a boxing operation.
		///     </remarks>
		/// </summary>
		/// <param name="value">Value to check</param>
		/// <typeparam name="T"></typeparam>
		[AssertionMethod]
		[ContractAnnotation(VALUE_NULL_HALT)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RequiresNotNull<T>([AsrtCnd(AsrtCndType.IS_NOT_NULL)] T value)
		{
			AssertNull(value != null);
		}

		/// <summary>
		///     Specifies a precondition: checks to see if <paramref name="value" /> is <c>null</c>
		/// </summary>
		/// <param name="value">Value to check</param>
		/// <typeparam name="T"></typeparam>
		[AssertionMethod]
		[ContractAnnotation(VALUE_NULL_HALT)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RequiresNotNull<T>([AsrtCnd(AsrtCndType.IS_NOT_NULL)] in T value) where T : class
		{
			AssertNull(value != null);
		}

		#endregion


		/// <summary>
		///     <para>
		///         Specifies a precondition: checks to see if <typeparamref name="TExpected" />  is
		///         <typeparamref name="TActual" />.
		///     </para>
		/// </summary>
		/// <typeparam name="TExpected">Expected type</typeparam>
		/// <typeparam name="TActual">Supplied type</typeparam>
		/// <exception cref="TypeException">
		///     If <typeparamref name="TExpected" /> is not <typeparamref name="TActual" />
		/// </exception>
		internal static void RequiresType<TExpected, TActual>()
		{
			ResolveTypeAction<TExpected, TActual>(TypeException.Throw<Array, TActual>,
			                                      TypeException.Throw<TExpected, TActual>);
		}

		/// <summary>
		///     Specifies a precondition: checks to see if <typeparamref name="T" /> is a reference type.
		/// </summary>
		/// <typeparam name="T">Type to verify</typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RequiresClassType<T>()
		{
			Assert(!typeof(T).IsValueType, "Type parameter <{0}> must be a reference type", typeof(T).Name);
		}

		/// <summary>
		///     Specifies a precondition: checks to see if <typeparamref name="T" /> is a value type.
		/// </summary>
		/// <typeparam name="T">Type to verify</typeparam>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void RequiresValueType<T>()
		{
			Assert(typeof(T).IsValueType, "Type parameter <{0}> must be a value type", typeof(T).Name);
		}

		#endregion
	}
}