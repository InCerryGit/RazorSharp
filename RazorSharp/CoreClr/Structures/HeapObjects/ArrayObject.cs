#region

using System;
using System.Runtime.InteropServices;
using RazorCommon;
using RazorCommon.Strings;

// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible

#endregion

namespace RazorSharp.CoreClr.Structures.HeapObjects
{
	/// <summary>
	///     <para>Represents the layout of an array in heap memory.</para>
	///     <para>Corresponding files:</para>
	///     <list type="bullet">
	///         <item>
	///             <description>/src/vm/object.h</description>
	///         </item>
	///         <item>
	///             <description>/src/vm/object.cpp</description>
	///         </item>
	///         <item>
	///             <description>/src/vm/object.inl</description>
	///         </item>
	///     </list>
	///     <para>Lines of interest:</para>
	///     <list type="bullet">
	///         <item>
	///             <description>/src/vm/object.h: 743</description>
	///         </item>
	///     </list>
	///     <remarks>
	///         Should be used with <see cref="Runtime.GetArrayObject{T}" /> and double indirection.
	///     </remarks>
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	internal unsafe struct ArrayObject : IHeapObject
	{
// 		[FieldOffset(-8) public ObjHeader _header
		[FieldOffset(0)]
		private readonly MethodTable* m_methodTablePtr;
		
		[FieldOffset(Offsets.PTR_SIZE)]
		private readonly uint m_numComponents;

		[FieldOffset(Offsets.PTR_SIZE+sizeof(uint))]
		private readonly uint m_pad;


		public uint Length => m_numComponents;

		/// <summary>
		///     <remarks>
		///         Address-sensitive
		///     </remarks>
		/// </summary>
		public ObjHeader* Header => (ObjHeader*) (Unsafe.AddressOf(ref this) - IntPtr.Size);

		public MethodTable* MethodTable => m_methodTablePtr;


		public override string ToString()
		{
			var table = new ConsoleTable("Field", "Value");
			table.AddRow("Header*", Hex.ToHex(Header));
			table.AddRow("MethodTable*", Hex.ToHex(m_methodTablePtr));
			table.AddRow("Length", Length);


			return table.ToMarkDownString();
		}
	}
}