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
using System.IO;
using System.Linq;
using System.Text;
using OpenNETCF.Web.Configuration;
using OpenNETCF.Web.Hosting;

// disable warnings about obsoletes
#pragma warning disable 612, 618

namespace OpenNETCF.Web.Helpers
{
    internal static class UrlPath
    {
        private readonly static char[] trims = { '/' };
        private readonly static char[] slashChars;

        static UrlPath()
        {
            slashChars = new[] { '\\', '/' };
        }

        public static bool IsVirtualDirectory(string urlDirectory)
        {
            if (string.IsNullOrEmpty(urlDirectory) || (ServerConfig.GetConfig().VirtualDirectories == null))
            {
                return false;
            }

            urlDirectory = urlDirectory.Trim(trims);

            if (urlDirectory.IndexOf('/') > 0)
            {
                return urlDirectory.Split('/').Any(IsVirtualDirectory);
            }

            return (ServerConfig.GetConfig().VirtualDirectories[urlDirectory] != null);
        }

        internal static string GetFileName(string virtualPath)
        {
            return Path.GetFileName(virtualPath);
        }

        internal static string RemoveSlashFromPathIfNeeded(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            int pathLength = path.Length;
            if ((pathLength > 1) && (path[pathLength - 1] == '/'))
            {
                return path.Substring(0, pathLength - 1);
            }

            return path;
        }

        internal static string AppendSlashToPathIfNeeded(string path)
        {
            if (path == null)
            {
                return null;
            }
            int length = path.Length;
            if ((length != 0x0) && (path[length - 0x1] != '/'))
            {
                path = path + '/';
            }
            return path;
        }

        internal static bool IsRooted(string basepath)
        {
            return string.IsNullOrEmpty(basepath) || (basepath[0] == '/') || (basepath[0x0] == '\\');
        }

        internal static string MakeVirtualPathAppAbsolute(string virtualPath)
        {
            return MakeVirtualPathAppAbsolute(virtualPath, HostingEnvironment.ApplicationPhysicalPath); //.AppDomainAppVirtualPathString);
        }

        internal static string MakeVirtualPathAppAbsolute(string virtualPath, string applicationPath)
        {
            if ((virtualPath.Length == 1) && (virtualPath[0] == '~'))
            {
                return applicationPath;
            }
            if (((virtualPath.Length >= 2) && (virtualPath[0] == '~')) && ((virtualPath[1] == '/') || (virtualPath[1] == '\\')))
            {
                if (applicationPath.Length > 1)
                {
                    return (applicationPath + virtualPath.Substring(2));
                }
                return ("/" + virtualPath.Substring(0x2));
            }
            if (!IsRooted(virtualPath))
            {
                throw new ArgumentOutOfRangeException("virtualPath");
            }
            return virtualPath;
        }

        internal static string MakeRelative(string from, string to)
        {
            string relativePath;
            from = MakeVirtualPathAppAbsolute(from);
            to = MakeVirtualPathAppAbsolute(to);
            if (!IsRooted(from))
            {
                throw new ArgumentException(); // Resources.GetString("Path_must_be_rooted", new object[] { from }));
            }
            if (!IsRooted(to))
            {
                throw new ArgumentException(); // Resources.GetString("Path_must_be_rooted", new object[] { to }));
            }
            string queryString = null;
            if (to != null)
            {
                int index = to.IndexOf('?');
                if (index >= 0)
                {
                    queryString = to.Substring(index);
                    to = to.Substring(0, index);
                }
            }
            var fromUri = new Uri("file://foo" + from);
            var toUri = new Uri("file://foo" + to);
            if (fromUri.Equals(toUri))
            {
                int finalSlashIndex = to.LastIndexOfAny(slashChars);
                if (finalSlashIndex >= 0)
                {
                    if (finalSlashIndex == (to.Length - 1))
                    {
                        relativePath = "./";
                    }
                    else
                    {
                        relativePath = to.Substring(finalSlashIndex + 0x1);
                    }
                }
                else
                {
                    relativePath = to;
                }
            }
            else
            {
                relativePath = fromUri.MakeRelative(toUri);
            }
            return (relativePath + queryString + toUri.Fragment);
        }

        internal static bool IsAppRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path) || (path[0] != '~'))
            {
                return false;
            }

            int length = path.Length;
            return (length == 1) || (path[1] == Path.DirectorySeparatorChar) || (path[1] == '/');
        }

        internal static string ReduceVirtualPath(string path)
        {
            int length = path.Length;
            int startIndex = 0x0;
            while (true)
            {
                startIndex = path.IndexOf('.', startIndex);
                if (startIndex < 0x0)
                {
                    return path;
                }
                if (((startIndex == 0x0) || (path[startIndex - 0x1] == '/')) && ((((startIndex + 0x1) == length) || (path[startIndex + 0x1] == '/')) || ((path[startIndex + 0x1] == '.') && (((startIndex + 0x2) == length) || (path[startIndex + 0x2] == '/')))))
                {
                    break;
                }
                startIndex++;
            }
            var list = new ArrayList();
            var builder = new StringBuilder();
            startIndex = 0x0;
            do
            {
                int num3 = startIndex;
                startIndex = path.IndexOf('/', num3 + 0x1);
                if (startIndex < 0x0)
                {
                    startIndex = length;
                }
                if ((((startIndex - num3) <= 0x3) && ((startIndex < 0x1) || (path[startIndex - 0x1] == '.'))) && (((num3 + 0x1) >= length) || (path[num3 + 0x1] == '.')))
                {
                    if ((startIndex - num3) == 0x3)
                    {
                        if (list.Count == 0x0)
                        {
                            throw new HttpException("");// Resources.GetString("Cannot_exit_up_top_directory"));
                        }
                        if ((list.Count == 0x1) && IsAppRelativePath(path))
                        {
                            return ReduceVirtualPath(MakeVirtualPathAppAbsolute(path));
                        }
                        builder.Length = (int)list[list.Count - 0x1];
                        list.RemoveRange(list.Count - 0x1, 0x1);
                    }
                }
                else
                {
                    list.Add(builder.Length);
                    builder.Append(path, num3, startIndex - num3);
                }
            }
            while (startIndex != length);
            string str = builder.ToString();
            if (str.Length != 0x0)
            {
                return str;
            }
            if ((length > 0x0) && (path[0x0] == '/'))
            {
                return "/";
            }
            return ".";
        }

        internal static string FixVirtualPathSlashes(string virtualPath)
        {
            virtualPath = virtualPath.Replace('\\', '/');
            while (true)
            {
                string str = virtualPath.Replace("//", "/");
                if (str == virtualPath)
                {
                    return virtualPath;
                }
                virtualPath = str;
            }
        }

        internal static string GetExtension(string virtualPath)
        {
            return Path.GetExtension(virtualPath);
        }
    }
}

#pragma warning restore 612, 618
