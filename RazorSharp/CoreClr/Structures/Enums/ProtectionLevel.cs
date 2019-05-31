namespace RazorSharp.CoreClr.Structures.Enums
{
	/// <summary>
	///     <remarks>
	///         Use with <see cref="FieldDesc.ProtectionInt" />
	///     </remarks>
	/// </summary>
	public enum ProtectionLevel
	{
		Private           = 4,
		PrivateProtected  = 8,
		Internal          = 12,
		Protected         = 16,
		ProtectedInternal = 20,
		Public            = 24
	}
}