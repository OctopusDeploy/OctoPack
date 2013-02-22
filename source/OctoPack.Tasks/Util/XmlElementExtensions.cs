using System.Linq;
using System.Xml.Linq;

namespace OctoPack.Tasks.Util
{
    public static class XmlElementExtensions
    {
         public static XElement ElementAnyNamespace(this XContainer root, string localName)
         {
             return root.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
         }
    }
}