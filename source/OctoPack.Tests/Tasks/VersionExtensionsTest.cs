using NUnit.Framework;

namespace OctoPack.Tests.Tasks
{
    [TestFixture]
    public class VersionExtensionsTest
    {
        [Test]
        [TestCase("1", true)]
        [TestCase("1.0", true)]
        [TestCase("1.0.1", true)]
        [TestCase("1.0.1.5", true)]
        [TestCase("1.0.0-alpha", true)]
        [TestCase("test", false)]
        [TestCase("1.a", false)]
        public void ShouldParseSemanticVersions(string versionString, bool expected)
        {
            var actual = versionString.IsSemanticVersion();
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}