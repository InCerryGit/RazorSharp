#region

using System;
using System.Runtime.InteropServices;
using RazorCommon;

// ReSharper disable ConvertToAutoPropertyWhenPossible

#endregion

// ReSharper disable InconsistentNaming
// ReSharper disable BuiltInTypeReferenceStyle

namespace RazorSharp.CLR.Structures
{

	#region

	using UINT32 = UInt32;
	using BYTE = Byte;
	using UINT = UInt32;

	#endregion


	[StructLayout(LayoutKind.Explicit)]
	internal unsafe struct EEClassLayoutInfo
	{
		// why is there an m_cbNativeSize in EEClassLayoutInfo and EEClass?
		[FieldOffset(0)] private readonly UINT32 m_cbNativeSize;
		[FieldOffset(4)] private readonly UINT32 m_cbManagedSize;

		// 1,2,4 or 8: this is equal to the largest of the alignment requirements
		// of each of the EEClass's members. If the NStruct extends another NStruct,
		// the base NStruct is treated as the first member for the purpose of
		// this calculation.
		[FieldOffset(8)] private readonly BYTE m_LargestAlignmentRequirementOfAllMembers;

		// Post V1.0 addition: This is the equivalent of m_LargestAlignmentRequirementOfAllMember
		// for the managed layout.
		[FieldOffset(9)] private readonly BYTE m_ManagedLargestAlignmentRequirementOfAllMembers;

		[FieldOffset(10)] private readonly BYTE m_bFlags;

		// Packing size in bytes (1, 2, 4, 8 etc.)
		[FieldOffset(11)] private readonly BYTE m_cbPackingSize;

		// # of fields that are of the calltime-marshal variety.
		[FieldOffset(12)] private readonly UINT m_numCTMFields;


		[FieldOffset(16)] private readonly void* m_pFieldMarshalers;

		/// <summary>
		///     <para>Size (in bytes) of fixed portion of NStruct.</para>
		///     <remarks>
		///         <para>
		///             Equal to <see cref="Marshal.SizeOf(Type)" /> and (<see cref="EEClass" />)
		///             <see cref="EEClass.NativeSize" />
		///         </para>
		///     </remarks>
		/// </summary>
		internal uint NativeSize => m_cbNativeSize;

		/// <summary>
		///     <remarks>
		///         <para>Equal to <see cref="Unsafe.SizeOf{T}" /> </para>
		///     </remarks>
		/// </summary>
		internal uint ManagedSize => m_cbManagedSize;

		internal LayoutFlags Flags => (LayoutFlags) m_bFlags;


		internal bool ZeroSized => Flags.HasFlag(LayoutFlags.ZeroSized);

		internal bool IsBlittable => Flags.HasFlag(LayoutFlags.Blittable);

		public override string ToString()
		{
			ConsoleTable table = new ConsoleTable("Field", "Value");
			table.AddRow("Native size", m_cbNativeSize);
			table.AddRow("Managed size", m_cbManagedSize);
			table.AddRow("Largest alignment req of all", m_LargestAlignmentRequirementOfAllMembers);
			table.AddRow("Flags", String.Format("{0} ({1})", m_bFlags, String.Join(", ", Flags.GetFlags())));

			table.AddRow("Packing size", m_cbPackingSize);
			table.AddRow("CTM fields", m_numCTMFields);
			table.AddRow("Field marshalers", Hex.ToHex(m_pFieldMarshalers));
			table.AddRow("Blittable", IsBlittable);
			table.AddRow("Zero sized", ZeroSized);

			return table.ToMarkDownString();
		}
	}

}