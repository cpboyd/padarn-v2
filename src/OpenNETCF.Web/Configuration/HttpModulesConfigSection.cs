//
// Copyright ©2018 Christopher Boyd
//

using System;
using System.Collections.Generic;
using System.Xml;
using OpenNETCF.Configuration;

namespace OpenNETCF.Web.Configuration
{
    internal class HttpModulesConfigSection : List<HttpModuleElement>
    {
        private static readonly Dictionary<string, Type> ModuleTypeCache = new Dictionary<string, Type>();

        internal HttpModulesConfigSection() { }

        internal HttpModulesConfigSection(XmlNode section, XmlNamespaceManager nsMgr)
        {
            AddRange(section, nsMgr);
        }

        internal void AddRange(XmlNode section, XmlNamespaceManager nsMgr)
        {
            XmlNodeList list = section.GetNodes("add", nsMgr);
            foreach (XmlNode node in list)
            {
                Add(new HttpModuleElement(node.Attributes));
            }
        }

        internal void AppendTo(ServerConfig config)
        {
            foreach (HttpModuleElement element in this)
            {
                var module = (IHttpModule)Activator.CreateInstance(CheckType(element.Type, config));
                config.HttpModules.Add(element.Name, module);
            }
        }

        private Type CheckType(string typeName, ServerConfig config)
        {
            Type t;

            lock (ModuleTypeCache)
            {
                if (ModuleTypeCache.ContainsKey(typeName))
                {
                    t = ModuleTypeCache[typeName];
                }
                else
                {
                    t = config.GetType(typeName);

                    if (t == null)
                    {
                        throw new ConfigurationException(
                            string.Format("Unable to load type '{0}'", typeName));
                    }

                    ModuleTypeCache.Add(typeName, t);
                }
            }
            return t;
        }
    }

    internal class HttpModuleElement
    {
        readonly string _name;
        readonly string _type;

        internal HttpModuleElement(XmlAttributeCollection attributes)
            : this(attributes["name"].Value, attributes["type"].Value)
        { }

        internal HttpModuleElement(string name, string type)
        {

            _name = name;
            _type = type;
        }

        public string Name
        {
            get { return _name; }
        }

        public string Type
        {
            get { return _type; }
        }
    }
}
