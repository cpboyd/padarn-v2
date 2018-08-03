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
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using OpenNETCF.Web.Configuration;
using OpenNETCF.Web.Helpers;

namespace OpenNETCF.Web.Hosting
{
    /// <summary>
    /// Provides application-management functions and application services to a managed application within its application domain. This class cannot be inherited.
    /// </summary>
    public sealed class HostingEnvironment
    {
        private static AssemblyName m_assemblyName;
        internal static VirtualPathProvider VirtualPathProvider { get; private set; }

        /// <summary>
        /// Gets the physical path on disk to the application's directory.
        /// </summary>
        public static string ApplicationPhysicalPath
        {
            get
            {
                if (m_assemblyName == null)
                {
                    m_assemblyName = Assembly.GetExecutingAssembly().GetName();
                }

                return Path.GetDirectoryName(m_assemblyName.CodeBase);
            }
        }

        /// <summary>
        /// Gets the version of the Padarn server assembly
        /// </summary>
        public static Version ApplicationVersion
        {
            get
            {
                if (m_assemblyName == null)
                {
                    m_assemblyName = Assembly.GetExecutingAssembly().GetName();
                }

                return m_assemblyName.Version;
            }
        }

        /// <summary>
        /// Registers a new VirtualPathProvider instance with the Padarn system.
        /// </summary>
        /// <param name="virtualPathProvider"></param>
        public static void RegisterVirtualPathProvider(VirtualPathProvider virtualPathProvider)
        {
            VirtualPathProvider previous = VirtualPathProvider;
            VirtualPathProvider = virtualPathProvider;
            virtualPathProvider.Initialize(previous);
        }

        /// <summary>
        /// Maps a virtual path to a physical path on the server.
        /// </summary>
        /// <param name="virtualPath">The virtual path (absolute or relative). </param>
        /// <returns>The physical path on the server specified by virtualPath.</returns>
        public static string MapPath(string virtualPath)
        {
            char separator = Path.DirectorySeparatorChar;

            // Normalize the path
            string path = Regex.Replace(virtualPath, @"(/+)", "/");

            // Get the index of the end of the last directory name
            string resourcePath;
            string resourceIdentifier;

            int endIndex = path.LastIndexOf('/');
            if (endIndex < 0)
            {
                resourcePath = "/";
                resourceIdentifier = path;
            }
            else
            {
                resourcePath = (endIndex == 0) ? string.Empty : path.Substring(virtualPath.StartsWith("/") ? 1 : 0, endIndex - 1);
                resourceIdentifier = path.Substring(endIndex + 1);
            }

            ServerConfig webServerConfig = ServerConfig.GetConfig();

            // make sure all slashes on all OSes use the proper directory separator
            string rootPath = webServerConfig.DocumentRoot.Replace('/', separator).Replace('\\', separator);
            rootPath = (rootPath.EndsWith(separator) ? rootPath : string.Format("{0}{1}", rootPath, separator));
            var physicalPath = new StringBuilder(rootPath);

            string[] directories = resourcePath.Split('/');
            foreach (string directory in directories)
            {
                if (string.IsNullOrEmpty(directory))
                {
                    break;
                }

                if (UrlPath.IsVirtualDirectory(directory))
                {
                    physicalPath = new StringBuilder(string.Format("{0}{1}", ServerConfig.GetConfig().VirtualDirectories.GetVirtualDirectory(directory).PhysicalDirectory, separator));
                }
                else
                {
                    physicalPath.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", directory, separator);
                }
            }

            physicalPath.Append(resourceIdentifier);

            return physicalPath.ToString().Replace('/', separator).Replace('\\', separator);
        }
    }
}
