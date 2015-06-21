using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Framework;
using OctoPack.Tasks;
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

		private const string xml2 =
			"<?xml version=\"1.0\"?><package xmlns=\"http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd\"><metadata><id>Sample.WebAppWithSpecAndCustomContent</id><title>Sample application</title><version>1.0.0</version><authors>OctopusDeploy</authors><owners>OctopusDeploy</owners><licenseUrl>http://octopusdeploy.com</licenseUrl><projectUrl>http://octopusdeploy.com</projectUrl><requireLicenseAcceptance>false</requireLicenseAcceptance><description>This is a sample ASP.NET MVC package designed to work with Octopus.</description></metadata><files><file src=\"bin\\*.dll\" target=\"bin\" /><file src=\"SomeFiles\\Foo.css\" target=\"SomeFiles\" /></files></package>";
		[Test]
		public void ElementAtAnyNamespaceShouldReturnFirstNodeByName()
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml);

			var result = XmlElementExtensions.ElementAnyNamespace(doc, "owners");

			Assert.AreEqual("owners", result.Name);
		}

		[Test]
		public void ElementAtAnyNamespaceShouldReturnFirstNodeByName2()
		{
			var doc = new XmlDocument();
			doc.LoadXml(xml2);

			var result = XmlElementExtensions.ElementAnyNamespace(doc, "package");

			Assert.AreEqual("package", result.Name);
		}



		[Test]
		public void AddChildElementTest()
		{
			var manifest = new XmlDocument();
			XmlNode docNode = manifest.CreateXmlDeclaration("1.0", "UTF-8", null);
			manifest.AppendChild(docNode);

			var element = XmlElementExtensions.AddChildElement(manifest, "package", new List<XmlNodeAttribute>{ new XmlNodeAttribute("xmlns", @"http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd")});
			var metadata = XmlElementExtensions.AddChildElement(manifest, element, "metadata");

			XmlElementExtensions.AddChildElement(manifest, metadata, "id", "123");
			XmlElementExtensions.AddChildElement(manifest, metadata, "version", "0.1");
			XmlElementExtensions.AddChildElement(manifest, metadata, "authors", "AM");
			XmlElementExtensions.AddChildElement(manifest, metadata, "owners", "AM");
			XmlElementExtensions.AddChildElement(manifest, metadata, "licenseUrl", "http://example.com");
			XmlElementExtensions.AddChildElement(manifest, metadata, "projectUrl", "http://example.com");
			XmlElementExtensions.AddChildElement(manifest, metadata, "requireLicenseAcceptance", "false");
			XmlElementExtensions.AddChildElement(manifest, metadata, "description", "The test deployment package, built on " + DateTime.Now.ToShortDateString());
			XmlElementExtensions.AddChildElement(manifest, metadata, "releaseNotes", "");

			manifest.Save("test.nuspec");

			Assert.IsTrue(File.Exists("test.nuspec"));
			Assert.IsNotNull(XmlElementExtensions.ElementAnyNamespace(manifest, "package"));
		}
	}
}
