#region

#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using RazorSharp.CoreClr.Meta.Interfaces;
using RazorSharp.CoreClr.Meta.Virtual;
using SimpleSharp;
using SimpleSharp.Diagnostics;
using SimpleSharp.Utilities;
using RazorSharp.CoreClr.Structures;
using RazorSharp.CoreClr.Structures.EE;
using RazorSharp.CoreClr.Structures.Enums;
using RazorSharp.Memory;
using RazorSharp.Memory.Pointers;
using RazorSharp.Utilities;

#endregion

// ReSharper disable InconsistentNaming

#endregion

namespace RazorSharp.CoreClr.Meta
{
	/// <summary>
	///     Exposes metadata from:
	///     <list type="bullet">
	///         <item>
	///             <description>
	///                 <see cref="Structures.MethodTable" />
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 <see cref="EEClass" />
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 <see cref="EEClassLayoutInfo" />
	///             </description>
	///         </item>
	///     </list>
	/// <remarks>Corresponds to <see cref="Type"/></remarks>
	/// </summary>
	public class MetaType : IMetadata, IFormattable
	{
		/// <summary>
		///     Exhaustive
		/// </summary>
		private const string FMT_E = "E";

		/// <summary>
		///     Basic
		/// </summary>
		private const string FMT_B = "B";

		public MetaType(Type t) : this(t.GetMethodTable()) { }

		internal MetaType(Pointer<MethodTable> p)
		{
			MethodTable = p;

			if (!p.Reference.Canon.IsNull && p.Reference.Canon.Address != p.Address)
				Canon = new MetaType(p.Reference.Canon);

			if (p.Reference.IsArray)
				ElementType = new MetaType(p.Reference.ElementTypeHandle);

			if (!p.Reference.Parent.IsNull) {
				Parent = new MetaType(p.Reference.Parent);
			}


			Fields    = new VirtualCollection<MetaField>(GetField, GetFields);
			Methods   = new VirtualCollection<MetaMethod>(GetMethod, GetMethods);
			AllFields = new VirtualCollection<MetaField>(GetAnyField, GetAllFields);
		}

		private Pointer<EEClassLayoutInfo> LayoutInfo => MethodTable.Reference.EEClass.Reference.LayoutInfo;

		public IEnumerable<MetaField> InstanceFields {
			get {
				if (IsArray) {
					return Array.Empty<MetaField>();
				}

				return Fields
				      .Where(f => !f.FieldInfo.IsStatic && !f.FieldInfo.IsLiteral)
				      .OrderBy(f => f.Offset);
			}
		}

		/// <summary>
		/// <see cref="InstanceFields"/> with transient fields
		/// </summary>
		public IEnumerable<IReadableStructure> MemoryFields {
			get {
				List<IReadableStructure> instanceFields = InstanceFields.Cast<IReadableStructure>().ToList();

				if (!IsStruct) {
					instanceFields.Insert(0, GetObjectHeaderField());
					instanceFields.Insert(1, GetMethodTableField());
				}

				return instanceFields;
			}
		}

		public IEnumerable<PaddingField> Padding {
			get {
				var nextOffsetOrSize = NumInstanceFieldBytes;
				var memFields        = InstanceFields.ToArray(); // todo: maybe use MemoryFields?

				for (int i = 0; i < memFields.Length; i++) {
					// start padding

					if (i != memFields.Length - 1) {
						nextOffsetOrSize = Fields[i + 1].Offset;
					}

					int nextSectOfsCandidate = Fields[i].Offset + Fields[i].Size;

					if (nextSectOfsCandidate < nextOffsetOrSize) {
						int padSize = nextOffsetOrSize - nextSectOfsCandidate;

						yield return new PaddingField(nextSectOfsCandidate, padSize);
					}

					// end padding
				}
			}
		}

		public IReadableStructure[] GetElementFields(object value)
		{
			if (!IsStringOrArray) {
				return Array.Empty<IReadableStructure>();
			}

			Conditions.Require(value.GetType() == RuntimeType, nameof(value));


			int lim;

			switch (value) {
				case string str:
					lim = str.Length - 1;
					break;
				case Array rg:
					lim = rg.Length;
					break;
				default:
					throw new InvalidOperationException();
			}

			var elementFields = new List<IReadableStructure>(lim);

			if (IsArray) {
				int d = 1;
				if (MemInfo.Is64Bit) {
					d++;
				}

				elementFields.Capacity = lim + d;

				elementFields.AddRange(ElementField.CreateArrayStructures());
			}

			for (int i = 0; i < lim; i++) {
				var element = ElementField.Create(this, i);

				elementFields.Add(element);
			}

			return elementFields.ToArray();
		}

		public bool IsStruct => RtInfo.IsStruct(RuntimeType);

		internal static IReadableStructure GetMethodTableField() => new MethodTableField();

		internal static IReadableStructure GetObjectHeaderField() => new ObjectHeaderField();

		public IEnumerable<MetaField> MethodTableFields {
			get {
				var mtFields = RuntimeType.GetCorrespondingMethodTableFields();
				foreach (var info in mtFields) {
					yield return new MetaField(info);
				}
			}
		}

		public string ToString(string format, IFormatProvider formatProvider)
		{
			if (String.IsNullOrEmpty(format))
				format = FMT_B;

			if (formatProvider == null)
				formatProvider = CultureInfo.CurrentCulture;


			switch (format.ToUpperInvariant()) {
				case FMT_B:
					return String.Format(
						"{0} (token: {1}) (base size: {2}) (component size: {3}) (base fields size: {4})",
						Name, Token, BaseSize, ComponentSize, NumInstanceFieldBytes);
				case FMT_E:
					return ToTable().ToString();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private MetaField GetAnyField(string name)
		{
			var field = RuntimeType.GetAnyField(name);
			Conditions.Require(!field.IsLiteral, "Field cannot be literal", nameof(field));
			return new MetaField(field.GetFieldDesc());
		}

		private MetaField[] GetAllFields()
		{
			FieldInfo[] fields     = RuntimeType.GetAllFields().Where(x => !x.IsLiteral).ToArray();
			var         metaFields = new MetaField[fields.Length];

			for (int i = 0; i < fields.Length; i++) {
				metaFields[i] = new MetaField(fields[i]);
			}

			return metaFields;
		}

		private MetaField GetField(string name)
		{
			return new MetaField(RuntimeType.GetFieldDesc(name));
		}

		private MetaField[] GetFields()
		{
			Pointer<FieldDesc>[] fields = RuntimeType.GetFieldDescs();
			var                  meta   = new MetaField[fields.Length];

			for (int i = 0; i < fields.Length; i++)
				meta[i] = new MetaField(fields[i]);

			return meta;
		}

		private MetaMethod GetMethod(string name)
		{
			return new MetaMethod(RuntimeType.GetMethodDesc(name));
		}

		private MetaMethod[] GetMethods()
		{
			Pointer<MethodDesc>[] methods = RuntimeType.GetMethodDescs();
			var                   meta    = new MetaMethod[methods.Length];

			for (int i = 0; i < meta.Length; i++)
				meta[i] = new MetaMethod(methods[i]);

			return meta;
		}

		private ConsoleTable ToTable()
		{
			var table = new ConsoleTable("Info", "Value");
			table.AddRow("Name", Name);
			table.AddRow("Token", Token);

			/* -- Sizes -- */
			table.AddRow("Base size", BaseSize);
			table.AddRow("Component size", ComponentSize);
			table.AddRow("Base fields size", NumInstanceFieldBytes);

			/* -- Flags -- */
			table.AddRow("Flags", EnumUtil.CreateString(Flags));
			table.AddRow("Flags 2", EnumUtil.CreateString(Flags2));
			table.AddRow("Low flags", EnumUtil.CreateString(FlagsLow));
			table.AddRow("Attributes", EnumUtil.CreateString(TypeAttributes));
			table.AddRow("Layout flags", HasLayout ? EnumUtil.CreateString(LayoutFlags) : "-");
			table.AddRow("VM Flags", EnumUtil.CreateString(VMFlags));

			/* -- Aux types -- */
			table.AddRow("Canon type", Canon?.Name);
			table.AddRow("Element type", ElementType?.Name);
			table.AddRow("Parent type", Parent.Name);

			/* -- Numbers -- */
			table.AddRow("Number instance fields", NumInstanceFields);
			table.AddRow("Number static fields", NumStaticFields);
			table.AddRow("Number non virtual slots", NumNonVirtualSlots);
			table.AddRow("Number methods", NumMethods);
			table.AddRow("Number instance field bytes", NumInstanceFieldBytes);
			table.AddRow("Number virtuals", NumVirtuals);
			table.AddRow("Number interfaces", NumInterfaces);

			table.AddRow("Blittable", IsBlittable);


			table.AddRow("Value", MethodTable.ToString("P"));

			return table;
		}

		public override string ToString()
		{
			return ToString(FMT_B);
		}

		public string ToString(string format)
		{
			return ToString(format, CultureInfo.CurrentCulture);
		}

		public static implicit operator MetaType(Type type) => new MetaType(type);

		#region Accessors

		public MetaField this[string name] => AllFields[name];

		public VirtualCollection<MetaField> Fields { get; }

		public VirtualCollection<MetaField> AllFields { get; }

		public VirtualCollection<MetaMethod> Methods { get; }

		public MetaType Canon { get; }

		public MetaType ElementType { get; }

		public MetaType Parent { get; }


		public CorElementType NormalType => MethodTable.Reference.EEClass.Reference.NormalType;

		public MemberInfo Info => RuntimeType;

		public string Name => MethodTable.Reference.Name;

		/// <summary>
		///     Metadata token
		///     <remarks>
		///         <para>Equal to WinDbg's <c>!DumpMT /d</c> <c>"mdToken"</c> value in hexadecimal format.</para>
		///         <para>Equals <see cref="Type.MetadataToken" /></para>
		///     </remarks>
		/// </summary>
		public int Token => MethodTable.Reference.Token;

		/// <summary>
		///     <para>Corresponding <see cref="Type" /> of this <see cref="Structures.MethodTable" /></para>
		/// </summary>
		public Type RuntimeType => MethodTable.Reference.RuntimeType;

		#region bool

		public bool IsZeroSized => LayoutInfo.Reference.ZeroSized;

		public bool HasLayout => MethodTable.Reference.EEClass.Reference.HasLayout;

		public bool HasComponentSize => MethodTable.Reference.HasComponentSize;

		public bool IsArray => MethodTable.Reference.IsArray;

		public bool IsStringOrArray => MethodTable.Reference.IsStringOrArray;

		public bool IsBlittable => MethodTable.Reference.IsBlittable;

		public bool IsString => MethodTable.Reference.IsString;

		public bool ContainsPointers => MethodTable.Reference.ContainsPointers;

		#endregion

		#region Flags

		public LayoutFlags LayoutFlags => LayoutInfo.Reference.Flags;

		public TypeAttributes TypeAttributes => MethodTable.Reference.EEClass.Reference.TypeAttributes;

		public VMFlags VMFlags => MethodTable.Reference.EEClass.Reference.VMFlags;

		public MethodTableFlags Flags => MethodTable.Reference.Flags;

		public MethodTableFlags2 Flags2 => MethodTable.Reference.Flags2;

		public MethodTableFlagsLow FlagsLow => MethodTable.Reference.FlagsLow;

		#endregion

		#region Size

		/// <summary>
		///     The base size of this class when allocated on the heap. Note that for value types
		///     <see cref="BaseSize" /> returns the size of instance fields for a boxed value, and
		///     <see cref="NumInstanceFieldBytes" /> for an unboxed value.
		/// </summary>
		public int BaseSize => MethodTable.Reference.BaseSize;

		/// <summary>
		///     <para>The size of an individual element when this type is an array or string.</para>
		///     <example>
		///         If this type is a <c>string</c>, the component size will be <c>2</c>. (<c>sizeof(char)</c>)
		///     </example>
		///     <returns>
		///         <c>0</c> if <c>!</c><see cref="HasComponentSize" />, component size otherwise
		///     </returns>
		/// </summary>
		public int ComponentSize => MethodTable.Reference.ComponentSize;

		public int ManagedSize => (int) LayoutInfo.Reference.ManagedSize;

		public int NativeSize => MethodTable.Reference.EEClass.Reference.NativeSize;

		public int BaseSizePadding => MethodTable.Reference.EEClass.Reference.BaseSizePadding;

		#endregion

		#region Num

		/// <summary>
		///     The number of instance fields in this type.
		/// </summary>
		public int NumInstanceFields => MethodTable.Reference.NumInstanceFields;

		/// <summary>
		///     The number of <c>static</c> fields in this type.
		/// </summary>
		public int NumStaticFields => MethodTable.Reference.NumStaticFields;

		public int NumNonVirtualSlots => MethodTable.Reference.NumNonVirtualSlots;

		/// <summary>
		///     Number of methods in this type.
		/// </summary>
		public int NumMethods => MethodTable.Reference.NumMethods;

		/// <summary>
		///     The size of the instance fields in this type. This is the unboxed size of the type if the object is boxed.
		///     (Minus padding and overhead of the base size.)
		/// </summary>
		public int NumInstanceFieldBytes => MethodTable.Reference.NumInstanceFieldBytes;

		/// <summary>
		///     The number of virtual methods in this type (<c>4</c> by default; from <see cref="Object" />)
		/// </summary>
		public int NumVirtuals => MethodTable.Reference.NumVirtuals;

		/// <summary>
		///     The number of interfaces this type implements
		///     <remarks>
		///         <para>Equal to WinDbg's <c>!DumpMT /d</c> <c>Number of IFaces in IFaceMap</c> value.</para>
		///     </remarks>
		/// </summary>
		public int NumInterfaces => MethodTable.Reference.NumInterfaces;

		#endregion

		private Pointer<MethodTable> MethodTable { get; }

		/// <summary>
		/// Points to <see cref="MethodTable"/>
		/// </summary>
		public Pointer<byte> Value => MethodTable.Cast();

		#endregion
	}
}