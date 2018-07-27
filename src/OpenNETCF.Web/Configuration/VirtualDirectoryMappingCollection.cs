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
using System.Xml;
using OpenNETCF.Configuration;

namespace OpenNETCF.Web.Configuration
{
    internal class VirtualDirectoryMappingCollection : List<VirtualDirectoryMapping>
    {
        internal VirtualDirectoryMappingCollection() { }

        internal VirtualDirectoryMappingCollection(XmlNode node)
        {
            ParseXml(node);
        }

        public VirtualDirectoryMapping GetVirtualDirectory(string virtualPath)
        {
            return Find(mapping => StringComparer.InvariantCultureIgnoreCase.Equals(mapping.VirtualDirectory, virtualPath));
        }

        public VirtualDirectoryMapping this[string virtualPath]
        {
            get { return GetVirtualDirectory(virtualPath); }
        }

        public string FindPhysicalDirectoryForVirtualUrlPath(string url)
        {
            return (from mapping in this
                    where url.IndexOf(mapping.VirtualDirectory, StringComparison.InvariantCultureIgnoreCase) > 0
                    select mapping.PhysicalDirectory)
                    .FirstOrDefault();
        }

        private void ParseXml(XmlNode node)
        {
            if (!node.HasChildNodes)
            {
                return;
            }

            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (HandlerBase.IsIgnorableAlsoCheckForNonElement(childNode))
                {
                    continue;
                }

                if (childNode.Name == "Directory")
                {
                    XmlNode directoryNode = childNode;

                    string virtualPath = HandlerBase.RemoveRequiredAttribute(
                        directoryNode, "VirtualPath", false);
                    string physicalPath = HandlerBase.RemoveRequiredAttribute(
                        directoryNode, "PhysicalPath", false).RemoveTrailingSlash();

                    string requiresAuthValue = HandlerBase.RemoveAttribute(
                        directoryNode, "RequireAuthentication");
                    var virtualDir = new VirtualDirectoryMapping(virtualPath, physicalPath)
                    {
                        RequiresAuthentication =
                            !string.IsNullOrEmpty(requiresAuthValue) && Convert.ToBoolean(requiresAuthValue)
                    };

                    HandlerBase.CheckForUnrecognizedAttributes(directoryNode);

                    this.Add(virtualDir);
                }
            }
        }
    }
}
