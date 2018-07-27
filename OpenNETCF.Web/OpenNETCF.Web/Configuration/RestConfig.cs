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
using System.Text;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.IO;

namespace OpenNETCF.Web.Configuration
{
    internal class RestConfig : List<Assembly>
    {
        internal RestConfig(XmlNode section)
        {
            ParseSection(section);
        }

        private void ParseSection(XmlNode section)
        {
            XmlNode assemblies = section.SelectSingleNode("Assemblies");
            if (assemblies != null)
            {
                ParseAssemblies(assemblies);
            }
        }

        private void ParseAssemblies(XmlNode assemblies)
        {
            XmlNodeList adds = assemblies.SelectNodes("add");
            Assembly currentAssembly = Assembly.GetExecutingAssembly();

            foreach (XmlNode n in adds)
            {
                string fileName = n.Attributes["name"].Value;
                if (!File.Exists(fileName))
                {
                    // local path?
                    fileName = Path.Combine(Path.GetDirectoryName(currentAssembly.GetName().CodeBase),
                        fileName);

                    if(!File.Exists(fileName))
                    {
                        throw new FileNotFoundException(string.Format("Cannot find REST service assembly '{0}'", n.Attributes["name"].Value));
                    }
                }

                Assembly asm;
                try
                {
                    asm = StringComparer.InvariantCultureIgnoreCase.Equals(AppDomain.CurrentDomain.FriendlyName, Path.GetFileName(fileName))
                        ? currentAssembly : Assembly.Load(fileName);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Cannot load REST service assembly '{0}'", n.Attributes["name"].Value), ex);
                }
                this.Add(asm);
            }
        }
    }
}
