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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using OpenNETCF.Web.UI.WebControls;

namespace OpenNETCF.Web.UI.Parsers
{
    internal class PageParser
    {
        private static PageParser m_instance;
        private readonly Dictionary<string, ControlBuilder> m_controlBuilders;
        private Page m_currentPage;
        private object m_syncRoot = new object();

        private PageParser()
        {
            // TODO: load up all of the control builders
            m_controlBuilders = new Dictionary<string, ControlBuilder>
            {
                {"button", new BasicControlBuilder<Button>("input")},
                {"label", new BasicControlBuilder<Label>("span")},
                {"textbox", new BasicControlBuilder<TextBox>("input")},
                {"linkbutton", new BasicControlBuilder<LinkButton>("a")}
            };
        }

        internal static PageParser GetParser()
        {
            if (m_instance == null)
            {
                m_instance = new PageParser();
            }

            return m_instance;
        }

        internal string PreParse(string content, out string docType, out AspxInfo info)
        {
            content = StripDocType(content, out docType);
            content = ParseAspTags(content, out info);

            return content;
        }

        internal string Parse(Page page, string content)
        {
            lock (m_syncRoot)
            {
                m_currentPage = page;

                if (string.IsNullOrEmpty(content))
                {
                    return string.Empty;
                }

                var doc = new XmlDocument();

                var nt = new NameTable();
                var nsmgr = new XmlNamespaceManager(nt);
                nsmgr.AddNamespace("asp", "http://www.w3.org/1999/xhtml");
                var context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);
                var xset = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
                XmlReader rd = XmlReader.Create(new StringReader(content), xset, context);
                doc.Load(rd);

                //Dump(doc.FirstChild, 0);

                content = ParseDocument(page, doc);

                //page = ParseAspControls(page);

                return string.Format("\r\n\r\n{0}\r\n\r\n{1}", page.DTDHeader, content);
            }
        }

        private string ParseDocument(Page page, XmlDocument doc)
        {
            XmlNode htmlNode = doc.FirstChild;
            // TODO: validate this is the "html" node?

            var sb = new StringBuilder();

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writer.WriteStartElement(htmlNode.LocalName, htmlNode.NamespaceURI);
                ParseNode(page, htmlNode, false, writer);
                writer.WriteFullEndElement();
                writer.Flush();
            }

            return sb.ToString();
        }

        private void InsertServerSideFormInfrastructure(XmlWriter writer)
        {
            writer.WriteStartElement("div");
            writer.WriteRaw("<input type=\"hidden\" name=\"__EVENTTARGET\" id=\"__EVENTTARGET\" value=\"\" />");
            writer.WriteRaw("<input type=\"hidden\" name=\"__EVENTARGUMENT\" id=\"__EVENTARGUMENT\" value=\"\" />");
            writer.WriteEndElement();

            writer.WriteStartElement("script");
            writer.WriteAttributeString("type", "text/javascript");
            writer.WriteRaw("\r\n//<![CDATA[\r\n");
            writer.WriteRaw("var theForm = document.forms['form1'];\r\n");
            writer.WriteRaw("if (!theForm) {\r\n");
            writer.WriteRaw("    theForm = document.form1;\r\n");
            writer.WriteRaw("}\r\n");
            writer.WriteRaw("function __doPostBack(eventTarget, eventArgument) {\r\n");
            writer.WriteRaw("    if (!theForm.onsubmit || (theForm.onsubmit() != false)) {\r\n");
            writer.WriteRaw("        theForm.__EVENTTARGET.value = eventTarget;\r\n");
            writer.WriteRaw("        theForm.__EVENTARGUMENT.value = eventArgument;\r\n");
            writer.WriteRaw("        theForm.submit();\r\n");
            writer.WriteRaw("    }\r\n");
            writer.WriteRaw("}\r\n");
            writer.WriteRaw("//]]>\r\n");
            writer.WriteEndElement();
        }

        private void ParseNode(Page page, XmlNode parent, bool runatServer, XmlWriter writer)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                Debug.WriteLine(node.Name);

                bool currentNodeIsServer = (node.Attributes != null) && (node.Attributes["runat"] != null);
                bool server = runatServer || currentNodeIsServer;


                if (server && node.Prefix == "asp")
                {
                    ParseAspControl(page, node, writer);
                }
                else if (currentNodeIsServer && node.LocalName == "form")
                {
                    writer.WriteStartElement("form");
                    string id = node.Attributes["id"].Value;
                    writer.WriteAttributeString("name", id);
                    writer.WriteAttributeString("id", id);
                    writer.WriteAttributeString("method", "post");
                    writer.WriteAttributeString("action", m_currentPage.Request.Path);

                    InsertServerSideFormInfrastructure(writer);

                    if (node.HasChildNodes)
                    {
                        ParseNode(page, node, server, writer);
                    }

                    writer.WriteFullEndElement();
                }
                else
                {
                    if (node.NodeType == XmlNodeType.Text)
                    {
                        writer.WriteRaw(node.InnerText);
                        continue;
                    }

                    writer.WriteStartElement(node.Prefix, node.LocalName, null);

                    foreach (XmlAttribute attr in node.Attributes)
                    {
                        if (server && (attr.Name == "runat"))
                        {
                            continue;
                        }
                        writer.WriteAttributeString(attr.Name, attr.Value);
                    }

                    if (node.HasChildNodes)
                    {
                        ParseNode(page, node, server, writer);
                    }

                    writer.WriteFullEndElement();
                }
            }
        }

        private void ParseAspControl(Page page, XmlNode node, XmlWriter writer)
        {
            string name = node.LocalName.ToLowerInvariant();
            if (!m_controlBuilders.ContainsKey(name))
            {
                throw new NotSupportedException(string.Format("Unsupported server control: '{0}'", name));
            }

            var parms = new Hashtable();

            foreach (XmlAttribute attrib in node.Attributes)
            {
                parms.Add(attrib.Name.ToLowerInvariant(), attrib.Value);
            }

            ControlBuilder builder = m_controlBuilders[name];

            var control = Activator.CreateInstance(builder.ControlType) as WebControl;
            control.SetParameters(parms);

            control.Content = node.InnerText;

            page.Controls.Add(control);

            control.Render(writer);
        }

        private void Dump(XmlNode parent, int level)
        {
            var spacer = new string(' ', level * 4);

            foreach (XmlNode node in parent.ChildNodes)
            {
                Debug.WriteLine(spacer + node.Name);

                if (node.Attributes["runat"] != null)
                {
                    Debug.WriteLine("server");
                }

                if (node.HasChildNodes)
                {
                    Dump(node, level + 1);
                }
            }
        }

        internal string StripDocType(string source, out string docType)
        {
            const string regex = "<!DOCTYPE(.*?)>";
            MatchCollection matches = Regex.Matches(source, regex, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            docType = string.Empty;

            foreach (Match match in matches)
            {
                docType = match.Value;
                source = source.Replace(docType, string.Empty);
            }

            return source;
        }

        private IDictionary GetControlParameters(string controlText)
        {
            return null;
        }

        internal string ParseAspTags(string source, out AspxInfo info)
        {
            info = new AspxInfo();

            const string regex = "<%(.*?)%>";
            MatchCollection matches = Regex.Matches(source, regex, RegexOptions.IgnoreCase | RegexOptions.Multiline);

            foreach (Match match in matches)
            {
                if (match.Value.StartsWith("<%@ Page"))
                {
                    info = new AspxInfo(match.Value);
                    source = source.Replace(match.Value, string.Empty);
                }
                else
                {
                    source = source.Replace(match.Value, ParseASP(match.Groups[1].Value));
                }
            }

            return source;
        }

        private string ParseASP(string tag)
        {
            // TODO:
            return "<pre>[translated asp]</pre>";
        }
    }
}
