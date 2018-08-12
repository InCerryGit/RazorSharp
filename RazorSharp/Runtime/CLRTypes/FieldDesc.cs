#region

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using RazorCommon;
using RazorSharp.Pointers;
using RazorSharp.Utilities;

#endregion

// ReSharper disable InconsistentNaming

namespace RazorSharp.Runtime.CLRTypes
{

	#region

	using unsigned = UInt32;

	#endregion


	/// <summary>
	/// Source: https://github.com/dotnet/coreclr/blob/59714b683f40fac869050ca08acc5503e84dc776/src/vm/field.cpp<para></para>
	/// Source 2: https://github.com/dotnet/coreclr/blob/59714b683f40fac869050ca08acc5503e84dc776/src/vm/field.h#L43<para></para>
	/// DO NOT DEREFERENCE <para></para>
	/// Internal representation: FieldHandle.Value
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public unsafe struct FieldDesc
	{
		private const int FieldOffsetMax    = (1 << 27) - 1;
		private const int FieldOffsetNewEnC = FieldOffsetMax - 4;

		#region Fields

		[FieldOffset(0)] private readonly MethodTable* m_pMTOfEnclosingClass;

		// unsigned m_mb                  	: 24;
		// unsigned m_isStatic            	: 1;
		// unsigned m_isThreadLocal       	: 1;
		// unsigned m_isRVA               	: 1;
		// unsigned m_prot                	: 3;
		// unsigned m_requiresFullMbValue 	: 1;
		[FieldOffset(8)] private readonly unsigned m_dword1;

		// unsigned m_dwOffset         		: 27;
		// unsigned m_type             		: 5;
		[FieldOffset(12)] private readonly unsigned m_dword2;

		#endregion

		#region Accessors

		/// <summary>
		/// MemberDef
		/// </summary>
		private int MB => (int) (m_dword1 & 0xFFFFFF);

		public int MemberDef {
			get {
				if (RequiresFullMBValue) {
					return TokenFromRid(MB & (int) MbMask.PackedMbLayoutMbMask, CorTokenType.mdtFieldDef);
				}

				return TokenFromRid(MB, CorTokenType.mdtFieldDef);
			}
		}


		/// <summary>
		/// Offset in heap memory
		/// </summary>
		public int Offset => (int) (m_dword2 & 0x7FFFFFF);

		/// <summary>
		/// Field type
		/// </summary>
		private int Type => (int) ((m_dword2 >> 27) & 0x7FFFFFF);

		public CorElementType CorType {
			get => (CorElementType) Type;
		}

		/// <summary>
		/// Whether the field is static
		/// </summary>
		public bool IsStatic => Memory.Memory.ReadBit(m_dword1, 24);

		/// <summary>
		/// Whether the field is decorated with a ThreadStatic attribute
		/// </summary>
		public bool IsThreadLocal => Memory.Memory.ReadBit(m_dword1, 25);

		/// <summary>
		/// Unknown
		/// </summary>
		public bool IsRVA => Memory.Memory.ReadBit(m_dword1, 26);

		/// <summary>
		/// Access level
		/// </summary>
		private int ProtectionInt => (int) ((m_dword1 >> 26) & 0x3FFFFFF);

		public Constants.ProtectionLevel Protection => (Constants.ProtectionLevel) ProtectionInt;

		/// <summary>
		/// Address-sensitive
		/// </summary>
		public int Size {
			get {
				int s = Constants.SizeOfCorElementType(CorType);

				if (s == -1) {
					fixed (FieldDesc* __this = &this) {
						return CLRFunctions.FieldDescFunctions.LoadSize(__this);
					}
				}

				return s;
			}
		}

		public object GetValue<TInstance>(TInstance t)
		{
			return FieldInfo.GetValue(t);
		}

		public FieldInfo FieldInfo => Runtime.FieldMap[this];

		/// <summary>
		/// Returns the address of the field in the specified type.<para></para>
		///
		/// Source 1: https://github.com/dotnet/coreclr/blob/59714b683f40fac869050ca08acc5503e84dc776/src/vm/field.cpp#L516 <para></para>
		/// Source 2: https://github.com/dotnet/coreclr/blob/59714b683f40fac869050ca08acc5503e84dc776/src/vm/field.cpp#L489 <para></para>
		/// Source 3: https://github.com/dotnet/coreclr/blob/59714b683f40fac869050ca08acc5503e84dc776/src/vm/field.cpp#L467 <para></para>
		///
		/// <exception cref="RuntimeException">If the field is static</exception>
		/// </summary>
		public IntPtr GetAddress<TInstance>(ref TInstance t)
		{
			if (IsStatic)
				throw new RuntimeException("You cannot get the address of a static field (yet)");

			Debug.Assert(Runtime.ReadMethodTable(ref t) == MethodTableOfEnclosingClass);
			Debug.Assert(Offset != FieldOffsetNewEnC);

			var data = Unsafe.AddressOf(ref t);
			if (typeof(TInstance).IsValueType) {
				return data + Offset;
			}
			else {
				data =  Marshal.ReadIntPtr(data);
				data += IntPtr.Size + Offset;

				return data;
			}
		}


		/// <summary>
		/// Slower than using Reflection
		///
		/// Address-sensitive
		/// </summary>
		public string Name {
			get {
#if SIGSCAN
				fixed (FieldDesc* __this = &this) {
					byte* lpcutf8 = CLRFunctions.FieldDescFunctions.GetName(__this);
					return CLRFunctions.StringFunctions.NewString(lpcutf8);
				}
#endif
				return FieldInfo.Name;


//				return Assertion.WIPString;
			}
		}

		/// <summary>
		/// Address-sensitive
		/// </summary>
		public MethodTable* MethodTableOfEnclosingClass {
			get {
				return (MethodTable*) PointerUtils.Add(Unsafe.AddressOf(ref this).ToPointer(), m_pMTOfEnclosingClass);
			}
		}

		public bool RequiresFullMBValue => Memory.Memory.ReadBit(m_dword1, 31);

		#endregion

		//https://github.com/dotnet/coreclr/blob/7b169b9a7ed2e0e1eeb668e9f1c2a049ec34ca66/src/inc/corhdr.h#L1512
		private int TokenFromRid(int rid, CorTokenType tktype)
		{
			return rid | ((int) tktype);
		}




		public override string ToString()
		{
			var table = new ConsoleTable("Field", "Value");

			// !NOTE NOTE NOTE!
			// this->ToString() must be used to view this

			table.AddRow("Name", Name);
			table.AddRow("Enclosing MethodTable", Hex.ToHex(MethodTableOfEnclosingClass));


			// Unsigned 1
			table.AddRow("MB", MB);
			table.AddRow("MemberDef", MemberDef);


			table.AddRow("Offset", Offset);
			table.AddRow("CorType", CorType);
			table.AddRow("Size", Size);

			table.AddRow("Static", IsStatic);
			table.AddRow("ThreadLocal", IsThreadLocal);
			table.AddRow("RVA", IsRVA);

			table.AddRow("Protection", Protection);
			table.AddRow("Requires full MB value", RequiresFullMBValue);


			return table.ToMarkDownString();
		}

	}


	internal enum MbMask
	{
		PackedMbLayoutMbMask       = 0x01FFFF,
		PackedMbLayoutNameHashMask = 0xFE0000
	}

}