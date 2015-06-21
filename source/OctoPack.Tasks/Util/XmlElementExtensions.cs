using System.Xml;

namespace OctoPack.Tasks.Util
{
	public static class XmlElementExtensions
	{
		 public static XmlNode ElementAnyNamespace(XmlNode root, string localName)
		 {
			 return root.SelectSingleNode(string.Format("//{0}", localName));
		 }
	}
}