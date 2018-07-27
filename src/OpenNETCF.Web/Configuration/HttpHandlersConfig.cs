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
using System.Linq;
using System.Text;
using System.Xml;

namespace OpenNETCF.Web.Configuration
{
    internal class HttpHandlersConfigSection : List<HttpHandler>
    {
        public IEnumerable<string> AssemblyNames { get; private set; }

        internal HttpHandlersConfigSection(XmlNode section, XmlNamespaceManager nsMgr)
        {
            ParseSection(section, nsMgr);
        }

        private void ParseSection(XmlNode section, XmlNamespaceManager nsMgr)
        {
            AssemblyNames = section.GetAssemblies(nsMgr);

            XmlNodeList list = section.GetNodes("add", nsMgr);
            foreach (XmlNode node in list)
            {
                string verb = node.Attributes["verb"].Value;
                HttpMethod method;
                if (verb != "*")
                {
                    method = (HttpMethod) Enum.Parse(typeof(HttpMethod), verb, true);
                }
                else
                {
                    method = HttpMethod.ANY;
                }

                this.Add(new HttpHandler(method, node.Attributes["path"].Value, node.Attributes["type"].Value));
            }
        }
    }


    internal class HttpHandler
    {
        readonly HttpMethod _verb;
        readonly string _path;
        readonly string _type;

        internal HttpHandler(HttpMethod verb, string path, string typeName)
        {

            _verb = verb;
            _path = path;
            _type = typeName;
        }

        public string TypeName
        {
            get { return _type; }
        }
        public HttpMethod Verb
        {
            get { return _verb; }
        }
        public string Path
        {
            get { return _path; }
        }
    }
}
