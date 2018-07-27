using NUnit.Framework;
using RazorSharp;
using RazorSharp.Pointers;

namespace Test.Testing.Tests
{
	[TestFixture]
	internal class PinningTests
	{
		/*[Test]
		public void TestPinHandle()
		{
			string s = "foo";
			PinHandle<string> p = PinHandle<string>.Pin(ref s);

			Assertion.AssertPinning(ref s);

			Dummy d = new Dummy();
			Debug.Assert(!Unsafe.IsBlittable<Dummy>());
			PinHandle<Dummy> p2 = PinHandle<Dummy>.Pin(ref d);
			Assertion.AssertPinning(ref d);
			p.Unpin();
			p2.Unpin();

			Assert.That(p.IsAllocated, Is.EqualTo(false));
			Assert.That(p.IsPinned, Is.EqualTo(false));
			Assert.That(p2.IsAllocated, Is.EqualTo(false));
			Assert.That(p2.IsPinned, Is.EqualTo(false));
		}*/

		[Test]
		public void TestILPinning()
		{
			string s = "foo";
			ArrayPointer<char> ptr = s;

			ObjectPinner.InvokeWhilePinned(s, delegate
			{
				TestingAssertion.AssertPinning(ref s);
				TestingAssertion.AssertElements(ptr, s);
			});
		}




	}

}