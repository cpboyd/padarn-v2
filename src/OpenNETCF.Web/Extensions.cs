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
using System.Linq;
using OpenNETCF.Web.UI;

#if NET_20
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public sealed class ExtensionAttribute : Attribute { }
}
#endif

namespace OpenNETCF.Web
{
    internal static class Extensions
    {
        public static bool Implements<T>(this Type t) where T : class
        {
            return t.GetInterfaces().Any(type => type == typeof(T));
        }

        public static bool EndsWith(this string str, char c)
        {
            return (str[str.Length - 1] == c);
        }

        public static string ToUpperInvariant(this string str)
        {
            return str.ToUpper(CultureInfo.InvariantCulture);
        }

        public static string ToLowerInvariant(this string str)
        {
            return str.ToLower(CultureInfo.InvariantCulture);
        }

        public static string AsText(this HtmlTextWriterTag tag)
        {
            return tag.ToString().ToLowerInvariant();
        }

        public static string AsText(this HtmlTextWriterAttribute attrib)
        {
            return attrib.ToString().ToLowerInvariant();
        }
    }
}
