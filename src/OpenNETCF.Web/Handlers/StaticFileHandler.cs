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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using OpenNETCF.Web.Headers;

namespace OpenNETCF.Web
{
    /// <summary>
    /// Provides access to specific file types.
    /// </summary>
    internal class StaticFileHandler : IHttpHandler
    {
        // Focus on compressing text files:
        private static readonly string[] AllowCompress =
        {
            ".css",
            ".htm",
            ".html",
            ".js",
            ".json",
            ".txt",
            ".svg",
            ".xml",
        };

        private static DateTimeFormatInfo m_dtfi = new DateTimeFormatInfo();

        private readonly string m_localFile;
        private readonly string m_mime;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="mimeType"></param>
        internal StaticFileHandler(string filePath, string mimeType)
        {
            m_localFile = filePath;
            m_mime = mimeType;
        }

        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return false; }
        }

        /// <summary>
        /// Processes the incoming HTTP request
        /// </summary>
        /// <param name="context">The HttpContext for the request</param>
        public void ProcessRequest(HttpContext context)
        {
            int et = Environment.TickCount;
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string filename = GetFilename(context); // TODO: Re-instate request.PhysicalPath;

            FileInfo requestedFile;

            try
            {
                requestedFile = new FileInfo(filename);
            }
            catch (IOException)
            {
                throw new HttpException(HttpStatusCode.NotFound, Resources.HttpEnumeratorError);
            }
            catch (SecurityException)
            {
                throw new HttpException(HttpStatusCode.Unauthorized, Resources.HttpEnumeratorDenied);
            }

            DateTime lastModTime = requestedFile.LastWriteTime;//File.GetLastWriteTime(filename);

            string ims = request.Headers["HTTP_IF_MODIFIED_SINCE"];
            if (!string.IsNullOrEmpty(ims))
            {
                try
                {
                    DateTime t = DateTime.Parse(ims, DateTimeFormatInfo.CurrentInfo);
                    if (lastModTime.Subtract(t.ToUniversalTime()).TotalSeconds < 1)
                    {
                        response.SendStatus(304, "Not Modified: " + m_mime, true);
                        return;
                    }
                }
                catch
                {
                    Debug.WriteLine("Unable to parse header 'HTTP_IF_MODIFIED_SINCE' value: " + ims);
                    // ignore and continue, we just won't return a 304
                }
            }

            if ((requestedFile.Attributes & FileAttributes.Hidden) != 0)
            {
                throw new HttpException(HttpStatusCode.NotFound, Resources.HttpFileHidden);
            }

            // https://stackoverflow.com/questions/429963/the-resource-cannot-be-found-error-when-there-is-a-dot-at-the-end-of-the-ur
            if (filename.EndsWith('.'))
            {
                throw new HttpException(HttpStatusCode.NotFound, Resources.HttpFileNotFound);
            }

            if (lastModTime > DateTime.Now)
            {
                lastModTime = DateTime.Now;
            }

            string strETag = GenerateETag(context, lastModTime);

            long fileLength = requestedFile.Length;
            try
            {
                BuildFileItemResponse(context, filename, fileLength, lastModTime, strETag, lastModTime);
            }
            catch (Exception)
            {
                throw new HttpException(HttpStatusCode.Unauthorized, Resources.HttpAccessForbidden);
            }

            response.ForcedContentLength = fileLength;
            response.ContentType = m_mime;

#if DEBUG
            int et2 = Environment.TickCount - et;
#endif
            response.TransmitFile(filename);
#if DEBUG
            et = Environment.TickCount - et;
            WriteTrace(et2, filename, "PreTransmit");
            WriteTrace(et, filename, "ProcessRequest");
#endif
        }

#if DEBUG
        private void WriteTrace(int ticks, string filename, string func)
        {
            if (ticks > 10)
            {
                HttpRuntime.WriteTrace(string.Format("FileHandler::{0} took {1}ms: {2}", func, ticks, filename));
            }
            
        }
#endif

        #endregion

        // Get the local file to serve and set Content-Encoding on the Response if necessary.
        private string GetFilename(HttpContext context)
        {
            string filename = m_localFile;
            string extension = Path.GetExtension(filename).ToLowerInvariant();

            if (!AllowCompress.Contains(extension))
            {
                return filename;
            }

            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            bool acceptGzip = false;
            bool acceptBrotli = false;
            foreach (StringWithQualityHeaderValue encoding in request.AcceptEncoding.Where(x => x.Quality > 0))
            {
                switch (encoding.Value.ToLowerInvariant())
                {
                    case ContentEncoding.Brotli:
                        acceptBrotli = true;
                        break;
                    case ContentEncoding.Gzip:
                        acceptGzip = true;
                        break;
                }

                if (acceptBrotli && acceptGzip)
                    break;
            }

            // Prefer Brotli if available:
            if (acceptBrotli && File.Exists(filename + ".br"))
            {
                response.ContentEncoding = ContentEncoding.Brotli;
                return filename + ".br";
            }

            if (acceptGzip && File.Exists(filename + ".gz"))
            {
                response.ContentEncoding = ContentEncoding.Gzip;
                return filename + ".gz";
            }

            return filename;
        }

        private static void BuildFileItemResponse(HttpContext context, string fileName, long fileSize,
                                                  DateTime lastModifiedTime, string strETag, DateTime lastChange)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            response.AppendHeader(Resources.HeaderAcceptRanges, Resources.HeaderAcceptRangesBytes);
            response.AppendHeader("Last-modified", lastChange.ToUniversalTime().ToString(m_dtfi.RFC1123Pattern));
            //string rangeHeader = request.Headers["Range"];

            // TODO: Complete implementation of BuildFileItemResponse
        }

        internal static string GenerateETag(HttpContext context, DateTime lastModTime)
        {
            var builder = new StringBuilder();
            long num = DateTime.Now.ToFileTime();
            long num2 = lastModTime.ToFileTime();
            builder.Append("\"");
            builder.Append(num2.ToString("X8", CultureInfo.InvariantCulture));
            builder.Append(":");
            builder.Append(num.ToString("X8", CultureInfo.InvariantCulture));
            builder.Append("\"");
            if ((DateTime.Now.ToFileTime() - num2) <= 0x1c9c380)
            {
                return ("W/" + builder);
            }
            return builder.ToString();
        }

    }
}
