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
using System.Collections.Generic;
using System.Net;
using System.Xml;
using OpenNETCF.Configuration;

#if !WindowsCE
using System.Configuration;
#endif

// disable warnings about obsoletes
#pragma warning disable 612, 618

namespace OpenNETCF.Web.Configuration
{

    /// <summary>
    /// Represents the WebServer section in the app.config file
    /// </summary>
    public sealed class ServerConfigurationHandler
#if WindowsCE
 : IConfigurationSectionHandler
#else
     : System.Configuration.IConfigurationSectionHandler
#endif
    {
        /// <summary>
        /// Creates an instance of ServerConfig from the information in the app.config file
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="configContext"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public object Create(object parent, object configContext, XmlNode section)
        {
            var cfg = new ServerConfig();

            foreach (XmlAttribute attribute in section.Attributes)
            {
                if (attribute.NodeType != XmlNodeType.Attribute)
                    continue;

                switch (attribute.Name)
                {
                    case "LocalIP":
                        try
                        {
                            cfg.LocalIP = IPAddress.Parse(attribute.Value);
                        }
                        catch
                        {
                            // TODO: log this
                            cfg.LocalIP = IPAddress.Any;
                        }
                        break;
                    case "DefaultPort":
                        cfg.Port = Int32.Parse(attribute.Value);
                        break;
                    case "DocumentRoot":
                        cfg.DocumentRoot = attribute.Value;
                        break;
                    case "TempRoot":
                        cfg.TempRoot = attribute.Value;
                        break;
                    case "MaxConnections":
                        cfg.MaxConnections = Int32.Parse(attribute.Value);
                        break;
                    case "Logging":
                        cfg.LoggingEnabled = bool.Parse(attribute.Value);
                        break;
                    case "LogFolder":
                        cfg.LogFileFolder = attribute.Value;
                        break;
                    case "LogExtensions":
                        cfg.SetLogExtensions(attribute.Value);
                        break;
                    case "LogProvider":
                        cfg.LogProvider = attribute.Value;
                        break;
                    case "BrowserDefinitions":
                        cfg.BrowserDefinitions = attribute.Value;
                        break;
                    case "CertificateName":
                        cfg.CertificateName = attribute.Value;
                        break;
                    case "CertificatePassword":
                        cfg.CertificatePassword = attribute.Value;
                        break;
                    case "UseSsl":
                        cfg.UseSsl = bool.Parse(attribute.Value);
                        break;
                    case "SSLLicenseKey":
                        cfg.SSLLicenseKey = attribute.Value;
                        break;
                    case "CustomErrorFolder":
                        cfg.CustomErrorFolder = attribute.Value;
                        break;
                    case "LicensePath":
                        cfg.LicensePath = attribute.Value;
                        break;
                    default:
                        HandleInvalidAttributes();
                        break;
                }
            }

            //Do the child nodes
            XmlNamespaceManager nsMgr = section.GetNsManager();
            foreach (XmlNode node in section.ChildNodes)
            {
                try
                {
                    switch (node.Name.FirstCharToLower())   // for backward compatibility
                    {
                        case "defaultDocuments":            // for backward compatibility
                            foreach (XmlNode docNode in node.ChildNodes)
                            {
                                cfg.DefaultDocuments.Add(docNode.InnerText);
                            }
                            break;

                        case "defaultDocument":
                            IEnumerable<string> docs = node.GetAttributeList("files/add", "value", nsMgr);
                            cfg.DefaultDocuments.AddRange(docs);
                            break;

                        case "assemblies":
                            IEnumerable<string> assemblyNames = node.GetAttributeList("add", "assembly", nsMgr);
                            cfg.AssembliesToLoad.AddRange(assemblyNames);
                            break;

                        case "authentication":
                            cfg.Authentication = new AuthenticationConfiguration(node);
                            break;

                        case "authorization":
                            ParseFormsAuthorization(node);
                            break;

                        case "virtualDirectories":
                            cfg.VirtualDirectories.AddRange(new VirtualDirectoryMappingCollection(node));
                            break;

                        case "cookies":
                            XmlAttribute domainAttrib = node.Attributes["Domain"];
                            if (domainAttrib == null)
                                break;

                            cfg.Cookies = new CookiesConfiguration(node);
                            break;

                        case "caching":
                            cfg.Caching.AddRange(new CachingConfig(node, nsMgr));
                            break;

                        case "virtualPathProviders":
                            IEnumerable<string> providers = node.GetAttributeList("add", "type", nsMgr);
                            cfg.VirtualPathProviders.AddRange(providers);
                            break;

                        case "httpHandlers":
                            var h = new HttpHandlersConfigSection(node, nsMgr);
                            cfg.AssembliesToLoad.AddRange(h.AssemblyNames); // for backward compatibility
                            cfg.HttpHandlers.AddRange(h);
                            break;

                        case "session":
                            cfg.Session = new SessionConfiguration(node);
                            break;

                        case "security":
                            cfg.Security = new SecurityConfig(node);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException(string.Format("Error parsing '{0}' configuration node in '{1}' section", node.Name, section.Name), ex);
                }
            }

            if (cfg.DefaultDocuments.Count == 0)
            {
                cfg.DefaultDocuments.Add("default.aspx");
            }

            return cfg;
        }

        // TODO:
        private static void ParseFormsAuthorization(XmlNode node)
        {
            // see http://support.microsoft.com/kb/316871
            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    switch (child.Name.FirstCharToLower())  // for backward compatibility
                    {
                        case "allow":
                            break;
                        case "deny":
                            break;
                    }
                }
            }
        }

        private static void HandleInvalidAttributes()
        {
            // Just ignore these attributes
        }
    }
}

#pragma warning restore 612, 618
