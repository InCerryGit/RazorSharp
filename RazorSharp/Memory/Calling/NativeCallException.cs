#region

using System;

#endregion

namespace RazorSharp.Memory.Calling
{
	public class NativeCallException : NotImplementedException
	{
		public NativeCallException(string name) : base($"Native method \"{name}\" error") { }

		public NativeCallException() : base("Native method error")
		{
			
		}
	}
}