#region

using System;

#endregion

namespace RazorSharp.CLR.Meta
{

	public static class Meta
	{
		public static MetaType GetType<T>()
		{
			return GetType(typeof(T));
		}

		public static MetaType GetType(Type t)
		{
			return new MetaType(t.GetMethodTable());
		}
	}

}