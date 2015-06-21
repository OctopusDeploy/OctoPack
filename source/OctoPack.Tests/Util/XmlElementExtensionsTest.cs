using System.Xml;
using NUnit.Framework;
using OctoPack.Tasks.Util;

namespace OctoPack.Tests.Util
{
	[TestFixture]
	public class XmlElementExtensionsTest
	{
		const string xml = "<?xml version=\"1.0\"?>"+
						"<package>"+
						  "<metadata>"+
							"<id>$id$</id>"+
							"<version>$version$</version>"+
							"<title>$title$</title>"+
							"<authors>AM</authors>"+
							"<owners>AM</owners>"+
							"<requireLicenseAcceptance>false</requireLicenseAcceptance>"+
							"<copyright>Copyright 2015</copyright>"+
						  "</metadata>"+
						"</package>";
		[Test]
		public void ElementAtAnyNamespaceShouldReturnFirstNodeByName()
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);

			var result = XmlElementExtensions.ElementAnyNamespace(doc, "owners");

			Assert.AreEqual("owners", result.Name);
		}
	}
}
