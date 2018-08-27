using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace OpenNETCF.Web.Configuration
{
    internal static class ConfigExtensions
    {
        public static XmlNamespaceManager GetNsManager(this XmlNode section)
        {
            if (string.IsNullOrEmpty(section.NamespaceURI))
            {
                return null;
            }

            var nsmgr = new XmlNamespaceManager(section.OwnerDocument.NameTable);
            nsmgr.AddNamespace("padarn", section.NamespaceURI);
            return nsmgr;
        }

        public static IEnumerable<string> GetAssemblies(this XmlNode section, XmlNamespaceManager nsmgr)
        {
            XmlNodeList list = section.GetNodes(nsmgr, "assembly");
            return (from XmlNode node in list select node.InnerText);
        }

        public static XmlNodeList GetNodes(this XmlNode section, XmlNamespaceManager nsMgr, params string[] path)
        {
            string xpath = nsMgr.CombinePath(path);
            return (nsMgr == null)
                ? section.SelectNodes(xpath)
                : section.SelectNodes(xpath, nsMgr);
        }

        public static string CombinePath(this XmlNamespaceManager nsMgr, params string[] path)
        {
            return (nsMgr == null)
                ? string.Join("/", path)
                : "padarn:" + string.Join("/padarn:", path);
        }

        public static IEnumerable<string> GetNodeValues(this XmlNode section, XmlNamespaceManager nsmgr, params string[] path)
        {
            return section.GetNodes(nsmgr, path).GetValues();
        }

        public static IEnumerable<string> GetValues(this XmlNodeList list)
        {
            return (from XmlNode node in list
                    where node != null
                    select node.Value);
        }

        public static IEnumerable<string> GetAttributeList(this XmlNodeList list, string attribute)
        {
            return (from XmlNode node in list
                    where node.Attributes[attribute] != null
                    select node.Attributes[attribute].Value);
        }

        public static string AttributeOrEmpty(this XmlNode node, string attribute)
        {
            XmlAttribute attr = node.Attributes[attribute];
            return (attr == null) ? string.Empty : attr.Value;
        }

        public static string FirstCharToLower(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str, 0))
            {
                return str;
            }

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static string RemoveTrailingSlash(this string str)
        {
            int lastChar = str.Length - 1;
            return (str.Length > 1 && str[lastChar] == '\\')
                ? str.Substring(0, lastChar) : str;
        }
    }
}
