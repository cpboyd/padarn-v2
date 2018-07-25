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

namespace OpenNETCF.Web
{
    internal class MimeMapping
    {
        public const string DefaultType = "text/plain";
        private static readonly Hashtable extensionToMimeMappingTable;

        static MimeMapping()
        {
            extensionToMimeMappingTable = new Hashtable(190, StringComparer.CurrentCultureIgnoreCase)
            {
                {".*", DefaultType},
                // Images:
                {".bmp", "image/bmp"},
                {".gif", "image/gif"},
                {".ico", "image/x-icon"},
                {".jpg", "image/jpeg"},
                {".tif", "image/tiff"},
                {".tiff", "image/tiff"},
                {".png", "image/png"},
                {".svg", "image/svg+xml"},
                // Text:
                {".html", "text/html"},
                {".htm", "text/html"},
                {".js", "application/javascript"},
                {".json", "application/json"},
                {".vb", "text/vbscript"},
                {".txt", "text/plain"},
                {".xml", "text/xml"},
                {".aspx", "text/html"},
                {".css", "text/css"},
                // Binary:
                {".pdf", "application/pdf"},
                {".gz", "application/gzip"},
                {".zip", "application/zip"}
            };
        }

        private MimeMapping()
        {
        }

        internal static string GetMimeMapping(string fileName)
        {
            if (fileName == null)
            {
                return DefaultType;
            }

            string text = null;
            int startIndex = fileName.LastIndexOf('.');
            if ((0 < startIndex) && (startIndex > fileName.LastIndexOf(System.IO.Path.DirectorySeparatorChar)))
            {
                text = (string) extensionToMimeMappingTable[fileName.Substring(startIndex)];
            }
            return text ?? DefaultType;
        }
    }
}
