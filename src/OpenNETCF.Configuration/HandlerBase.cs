#region License
// Copyright ©2017 Tacke Consulting (dba OpenNETCF)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, 
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or 
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR 
// ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;
using System.Globalization;
using System.Xml;

namespace OpenNETCF.Configuration
{
	public class HandlerBase
	{
		private static XmlNode GetAndRemoveAttribute(XmlNode node, string attrib, bool fRequired)
		{
			XmlNode xmlNode = node.Attributes.RemoveNamedItem(attrib);
			// If attribute is required and not present, throw exception
			if (fRequired && xmlNode == null)
			{
				throw new ConfigurationException("Missing required attribute: " + node.Name, node);
			}
		
			return xmlNode;
		}

		private static XmlNode GetAndRemoveStringAttributeInternal(XmlNode node, string attrib, bool fRequired, ref string val)
		{
			XmlNode xmlNode = GetAndRemoveAttribute(node, attrib, fRequired);
			if (xmlNode != null)
			{
				val = xmlNode.Value;
			}
			
			return xmlNode;
		}

		internal static XmlNode GetAndRemoveStringAttribute(XmlNode node, string attrib, ref string val)
		{
			return GetAndRemoveStringAttributeInternal(node, attrib, false, ref val);
		}

		internal static XmlNode GetAndRemoveRequiredStringAttribute(XmlNode node, string attrib, ref string val)
		{
			return GetAndRemoveStringAttributeInternal(node, attrib, true, ref val);
		}

		private static XmlNode GetAndRemoveBooleanAttributeInternal(XmlNode node, string attrib, bool fRequired, ref bool val)
		{
			XmlNode xmlNode = GetAndRemoveAttribute(node, attrib, fRequired);
			if (xmlNode != null)
			{
				try
				{
					val = Boolean.Parse(xmlNode.Value);
					return xmlNode;
				}
				catch (Exception e)
				{
					throw new ConfigurationException("Invalid boolean attribute: " + xmlNode.Name, e, xmlNode);
				}
			}
			else
			{
				return xmlNode;
			}
		}

		internal static XmlNode GetAndRemoveBooleanAttribute(XmlNode node, string attrib, ref bool val)
		{
			return GetAndRemoveBooleanAttributeInternal(node, attrib, false, ref val);
		}

		internal static XmlNode GetAndRemoveRequiredBooleanAttribute(XmlNode node, string attrib, ref bool val)
		{
			return GetAndRemoveBooleanAttributeInternal(node, attrib, true, ref val);
		}

		private static XmlNode GetAndRemoveIntegerAttributeInternal(XmlNode node, string attrib, bool fRequired, ref int val)
		{
			XmlNode xmlNode = GetAndRemoveAttribute(node, attrib, fRequired);
			if (xmlNode != null)
			{
				try
				{
					val = Int32.Parse(xmlNode.Value, CultureInfo.InvariantCulture);
					return xmlNode;
				}
				catch (Exception e)
				{
					throw new ConfigurationException("Invalid integer attribute:" + xmlNode.Name, e, xmlNode);
				}
			}
			else
			{
				return xmlNode;
			}
		}

		internal static XmlNode GetAndRemoveIntegerAttribute(XmlNode node, string attrib, ref int val)
		{
			return GetAndRemoveIntegerAttributeInternal(node, attrib, false, ref val);
		}

		internal static XmlNode GetAndRemoveRequiredIntegerAttribute(XmlNode node, string attrib, ref int val)
		{
			return GetAndRemoveIntegerAttributeInternal(node, attrib, true, ref val);
		}

		private static XmlNode GetAndRemovePositiveIntegerAttributeInternal(XmlNode node, string attrib, bool fRequired, ref int val)
		{
			XmlNode xmlNode = GetAndRemoveIntegerAttributeInternal(node, attrib, fRequired, ref val);
			if (xmlNode != null && val < 0)
			{
				throw new ConfigurationException("Invalid positive integer attribute: "  + attrib, node);
			}
			else
			{
				return xmlNode;
			}
		}

		internal static XmlNode GetAndRemovePositiveIntegerAttribute(XmlNode node, string attrib, ref int val)
		{
			return GetAndRemovePositiveIntegerAttributeInternal(node, attrib, false, ref val);
		}

		internal static XmlNode GetAndRemoveRequiredPositiveIntegerAttribute(XmlNode node, string attrib, ref int val)
		{
			return GetAndRemovePositiveIntegerAttributeInternal(node, attrib, true, ref val);
		}

		private static XmlNode GetAndRemoveTypeAttributeInternal(XmlNode node, string attrib, bool fRequired, ref Type val)
		{
			XmlNode xmlNode = GetAndRemoveAttribute(node, attrib, fRequired);
			if (xmlNode != null)
			{
				try
				{
					val = Type.GetType(xmlNode.Value, true);
					return xmlNode;
				}
				catch (Exception e)
				{
					throw new ConfigurationException("Invalid type attribute: " + xmlNode.Name, e, xmlNode);
				}
			}
			else
			{
				return xmlNode;
			}
		}

		internal static XmlNode GetAndRemoveTypeAttribute(XmlNode node, string attrib, ref Type val)
		{
			return GetAndRemoveTypeAttributeInternal(node, attrib, false, ref val);
		}

		internal static XmlNode GetAndRemoveRequiredTypeAttribute(XmlNode node, string attrib, ref Type val)
		{
			return GetAndRemoveTypeAttributeInternal(node, attrib, true, ref val);
		}

		public static void CheckForUnrecognizedAttributes(XmlNode node)
		{
			if (node.Attributes.Count != 0)
			{
				throw new ConfigurationException("Config base unrecognized attribute: " + node.Attributes.Item(0).Name, node);
			}
			else
			{
				return;
			}
		}

		public static string RemoveAttribute(XmlNode node, string name)
		{
			XmlNode xmlNode = node.Attributes.RemoveNamedItem(name);
			if (xmlNode != null)
			{
				return xmlNode.Value;
			}
			else
			{
				return null;
			}
		}

        public static string RemoveRequiredAttribute(XmlNode node, string name)
		{
			return RemoveRequiredAttribute(node, name, false);
		}

		public static string RemoveRequiredAttribute(XmlNode node, string name, bool allowEmpty)
		{
			object[] locals;

			XmlNode xmlNode = node.Attributes.RemoveNamedItem(name);
			if (xmlNode == null)
			{
				locals = new object[]{name};
				throw new ConfigurationException("Required attribute missing: " + locals, node);
			}
			if (xmlNode.Value != String.Empty || allowEmpty)
			{
				return xmlNode.Value;
			}
			locals = new object[]{name};
			throw new ConfigurationException("Required attribute empty: " + locals, node);
		}

		internal static void CheckForNonElement(XmlNode node)
		{
			if (node.NodeType != XmlNodeType.Element)
			{
				throw new ConfigurationException("Non-element found", node);
			}
			else
			{
				return;
			}
		}

		public static bool IsIgnorableAlsoCheckForNonElement(XmlNode node)
		{
			if (node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.Whitespace)
			{
				return true;
			}
			CheckForNonElement(node);
			return false;
		}

		internal static void CheckForChildNodes(XmlNode node)
		{
			if (node.HasChildNodes)
			{
				throw new ConfigurationException("No child nodes", node.FirstChild);
			}
			else
			{
				return;
			}
		}

		internal static void ThrowUnrecognizedElement(XmlNode node)
		{
			throw new ConfigurationException("Unrecognized element", node);
		}
	}
}
