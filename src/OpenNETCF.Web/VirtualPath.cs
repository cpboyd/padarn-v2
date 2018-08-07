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
using OpenNETCF.Web.Helpers;

namespace OpenNETCF.Web
{
    [Flags]
    internal enum VirtualPathOptions
    {
        AllowAbsolutePath = 0x4,
        AllowAllPath = 0x1c,
        AllowAppRelativePath = 0x8,
        AllowNull = 0x1,
        AllowRelativePath = 0x10,
        EnsureTrailingSlash = 0x2,
        FailIfMalformed = 0x20
    }

    internal sealed class VirtualPath : IComparable
    {
        internal static VirtualPath RootVirtualPath;
        private readonly static char[] illegalVirtualPathChars;
        private string _appRelativeVirtualPath;
        private string _virtualPath;

        static VirtualPath()
        {
            illegalVirtualPathChars = new[] { ':', '?', '*', '\0' };
            RootVirtualPath = Create("/");
        }

        private VirtualPath() { }

        private VirtualPath(string virtualPath)
        {
            if (UrlPath.IsAppRelativePath(virtualPath))
            {
                this._appRelativeVirtualPath = virtualPath;
            }
            else
            {
                this._virtualPath = virtualPath;
            }
        }

        public string VirtualPathString
        {
            get
            {
                if (this._virtualPath == null)
                {
                    this._virtualPath = HttpContext.Current.Request.Path;
                }

                return this._virtualPath;
            }
        }

        internal string VirtualPathStringNoTrailingSlash
        {
            get { return UrlPath.RemoveSlashFromPathIfNeeded(this.VirtualPathString); }
        }

        internal string VirtualPathStringWhicheverAvailable
        {
            get
            {
                return this._virtualPath ?? this._appRelativeVirtualPath;
            }
        }

        public string Extension
        {
            get { return UrlPath.GetExtension(this.VirtualPathString); }
        }

        public string FileName
        {
            get { return UrlPath.GetFileName(this.VirtualPathStringNoTrailingSlash); }
        }

        public bool IsRelative
        {
            get { return ((this._virtualPath != null) && (this._virtualPath[0] != '/')); }
        }

        public bool IsRoot
        {
            get { return (this._virtualPath == "/"); }
        }

        public VirtualPath Parent
        {
            get
            {
                this.FailIfRelativePath();

                if (this.IsRoot)
                {
                    return null;
                }

                string virtualPathStringNoTrailingSlash = UrlPath.RemoveSlashFromPathIfNeeded(this.VirtualPathStringWhicheverAvailable);
                if (virtualPathStringNoTrailingSlash == "~")
                {
                    virtualPathStringNoTrailingSlash = this.VirtualPathStringNoTrailingSlash;
                }

                int num = virtualPathStringNoTrailingSlash.LastIndexOf('/');
                return (num == 0) ? RootVirtualPath
                    : new VirtualPath(virtualPathStringNoTrailingSlash.Substring(0, num + 1));
            }
        }

        public string AppRelativeVirtualPathString
        {
            get
            {
                return this.AppRelativeVirtualPathStringOrNull ?? this._virtualPath;
            }
        }

        internal string AppRelativeVirtualPathStringOrNull
        {
            get
            {
                //if (this._appRelativeVirtualPath == null)
                //{
                //    if (this.flags[0x4])
                //    {
                //        return null;
                //    }

                //    this._appRelativeVirtualPath = UrlPath.MakeVirtualPathAppRelativeOrNull(this._virtualPath);
                //    //this.flags[0x4] = true;
                //    if (this._appRelativeVirtualPath == null)
                //    {
                //        return null;
                //    }
                //}
                return this._appRelativeVirtualPath;
            }
        }

        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            var path = obj as VirtualPath;
            if (path == null)
            {
                throw new ArgumentException();
            }

            return (path == this) ? 0
                : StringComparer.InvariantCultureIgnoreCase.Compare(this.VirtualPathString, path.VirtualPathString);
        }

        #endregion

        private static bool ContainsIllegalVirtualPathChars(string virtualPath)
        {
            return (virtualPath.IndexOfAny(illegalVirtualPathChars) >= 0);
        }

        public static VirtualPath CreateTrailingSlash(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAllPath | VirtualPathOptions.EnsureTrailingSlash);
        }

        public static VirtualPath Create(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAllPath);
        }

        public static VirtualPath Create(string virtualPath, VirtualPathOptions options)
        {
            if (virtualPath != null)
            {
                virtualPath = virtualPath.Trim();
            }
            if (string.IsNullOrEmpty(virtualPath))
            {
                if ((options & VirtualPathOptions.AllowNull) == 0)
                {
                    throw new ArgumentNullException("virtualPath");
                }
                return null;
            }
            if (ContainsIllegalVirtualPathChars(virtualPath))
            {
                throw new HttpException(""); //Resources.GetString("Invalid_vpath", new object[] { _virtualPath }));
            }
            string normalizedVirtualPath = UrlPath.FixVirtualPathSlashes(virtualPath);
            if (((options & VirtualPathOptions.FailIfMalformed) != 0) && !ReferenceEquals(virtualPath, normalizedVirtualPath))
            {
                throw new HttpException(""); //"Invalid virtual path ", _virtualPath }));
            }
            virtualPath = normalizedVirtualPath;
            if ((options & VirtualPathOptions.EnsureTrailingSlash) != 0)
            {
                virtualPath = UrlPath.AppendSlashToPathIfNeeded(virtualPath);
            }
            var path = new VirtualPath();
            if (UrlPath.IsAppRelativePath(virtualPath))
            {
                virtualPath = UrlPath.ReduceVirtualPath(virtualPath);
                if (virtualPath[0] == '~')
                {
                    if ((options & VirtualPathOptions.AllowAppRelativePath) == 0)
                    {
                        throw new ArgumentException();//Resources.GetString("VirtualPath_AllowAppRelativePath", new object[] { _virtualPath }));
                    }
                    path._appRelativeVirtualPath = virtualPath;
                    return path;
                }
                if ((options & VirtualPathOptions.AllowAbsolutePath) == 0)
                {
                    throw new ArgumentException();//Resources.GetString("VirtualPath_AllowAbsolutePath", new object[] { _virtualPath }));
                }
                path._virtualPath = virtualPath;
                return path;
            }
            if (virtualPath[0] != '/')
            {
                if ((options & VirtualPathOptions.AllowRelativePath) == 0)
                {
                    throw new ArgumentException();//Resources.GetString("VirtualPath_AllowRelativePath", new object[] { _virtualPath }));
                }
                path._virtualPath = virtualPath;
                return path;
            }
            if ((options & VirtualPathOptions.AllowAbsolutePath) == 0)
            {
                throw new ArgumentException();//Resources.GetString("VirtualPath_AllowAbsolutePath", new object[] { _virtualPath }));
            }
            path._virtualPath = UrlPath.ReduceVirtualPath(virtualPath);
            return path;
        }

        public static VirtualPath CreateNonRelative(string virtualPath)
        {
            return Create(virtualPath, VirtualPathOptions.AllowAppRelativePath | VirtualPathOptions.AllowAbsolutePath);
        }

        internal void FailIfRelativePath()
        {
            if (this.IsRelative)
            {
                throw new ArgumentException(string.Format("The relative virtual path '{0}' is not allowed here.", this._virtualPath));
            }
        }
    }
}
