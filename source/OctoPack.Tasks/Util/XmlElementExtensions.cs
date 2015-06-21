using System.Collections.Generic;
using System.Xml;

namespace OctoPack.Tasks.Util
{
	public static class XmlElementExtensions
	{
		public static XmlNode ElementAnyNamespace(XmlNode root, string localName)
		{
			var result = root.SelectSingleNode(string.Format("//*[local-name()='{0}']", localName));

			return result;
		}



		public static XmlElement AddChildElement(XmlDocument document, string nodeName, string value = null)
		{
			return AddChildElement(document, nodeName, null, value);
		}

		public static XmlElement AddChildElement(XmlDocument document, string nodeName, IList<XmlNodeAttribute> attributes,
		                                         string value = null)
		{
			var child = document.CreateElement(nodeName);
			if (attributes != null)
			{
				foreach (var attribute in attributes)
				{
					var att = document.CreateAttribute(attribute.Name);
					att.Value = attribute.Value;
					child.Attributes.Append(att);
				}
			}
			if (value != null)
				child.AppendChild(document.CreateTextNode(value));
			document.AppendChild(child);

			return child;
		}

		public static XmlElement AddChildElement(XmlDocument document, XmlNode package, string nodeName, string value = null)
		{
			return AddChildElement(document, package, nodeName, null, value);
		}

		public static XmlElement AddChildElement(XmlDocument document, XmlNode package, string nodeName,
		                                         IList<XmlNodeAttribute> attributes, string value = null)
		{
			var childElement = document.CreateElement(nodeName);
			if (attributes != null)
			{
				foreach (var attribute in attributes)
				{
					var att = document.CreateAttribute(attribute.Name);
					att.Value = attribute.Value;
					childElement.Attributes.Append(att);
				}
			}
			if (value != null)
				childElement.AppendChild(document.CreateTextNode(value));

			package.AppendChild(childElement);

			return childElement;
		}
	}
}