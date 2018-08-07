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

namespace OpenNETCF.Web.Configuration
{
    /// <summary>
    /// Mappings for Padarn virtual directories
    /// </summary>
    public sealed class VirtualDirectoryMapping
    {
        private readonly string physicalDirectory;
        private readonly string virtualDirectory;

        /// <summary>
        /// VirtualDirectoryMapping constructor
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="physicalPath"></param>
        public VirtualDirectoryMapping(string virtualPath, string physicalPath)
        {
            this.virtualDirectory = virtualPath;
            this.physicalDirectory = physicalPath;

            this.RequiresAuthentication = false;
        }

        /// <summary>
        /// Physical directory path
        /// </summary>
        public string PhysicalDirectory
        {
            get { return this.physicalDirectory; }
        }

        /// <summary>
        /// Virtual directory name
        /// </summary>
        public string VirtualDirectory
        {
            get { return this.virtualDirectory; }
        }

        /// <summary>
        /// True if the virtual directory requires authentication, otherwise false
        /// </summary>
        public bool RequiresAuthentication { get; internal set; }
    }
}
