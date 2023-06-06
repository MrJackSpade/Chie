using Ai.Utils.Extensions;

namespace Ai.Utils.Tests
{
	[TestClass]
	public class ExtensionTests
	{
		[TestMethod]
		public void SplitLengthDot()
		{
			string source = "This is a test. This is a test.";

			List<string> parts = source.SplitLength(19).Trim().ToList();

			Assert.AreEqual(2, parts.Count);

			Assert.AreEqual(parts[0], parts[1]);

			Assert.AreEqual(parts[0], "This is a test.");
		}

		[TestMethod]
		public void SplitLengthSpace()
		{
			string source = "This is a test This is a test";

			List<string> parts = source.SplitLength(19).Trim().ToList();

			Assert.AreEqual(2, parts.Count);

			Assert.AreEqual(parts[0], parts[1]);

			Assert.AreEqual(parts[0], "This is a test");
		}
	}
}