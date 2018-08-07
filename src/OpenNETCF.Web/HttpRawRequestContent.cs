﻿#region License
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
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using OpenNETCF.Web.Configuration;

namespace OpenNETCF.Web
{
    internal class HttpRawRequestContent : IDisposable
    {
        private static readonly char[] s_ColonOrNL = { ':'/*, '\n' */};
        private int m_chunkLength;
        private int m_chunkOffset;
        private bool m_completed;
        private byte[] m_data;
        private int m_endofLine;
        private int m_expectedLength;
        private TempFile m_file;
        private int m_fileThreshold;
        private NameValueCollection m_headers;
        private string m_httpMethod;
        private string m_httpVersion;
        private int m_length;
        private int m_lengthOfHeaders = -1;
        private string m_path;
        private string m_rawQueryString;

        private IPAddress m_remoteClient;

        /// <summary>
        /// The offset in which to start reading the line data
        /// </summary>
        private int m_startofLine;

        internal HttpRawRequestContent(int fileThreshold, int expectedLength, IPAddress remoteClient, HttpRawRequestContent rawRequest)
            : this(fileThreshold, expectedLength, remoteClient)
        {
            Path = rawRequest.Path;
            m_rawQueryString = rawRequest.RawQueryString;
            m_headers = rawRequest.Headers;
            m_httpMethod = rawRequest.HttpMethod;
            m_httpVersion = rawRequest.HttpVersion;
            AddBytes(rawRequest.GetAsByteArray(), 0, rawRequest.Length);
            rawRequest.Dispose();
        }

        internal HttpRawRequestContent(int fileThreshold, int expectedLength, IPAddress remoteClient)
        {
            m_remoteClient = remoteClient;

            this.m_fileThreshold = fileThreshold;
            this.m_expectedLength = expectedLength;
            if ((this.m_expectedLength >= 0) && (this.m_expectedLength < this.m_fileThreshold))
            {
                this.m_data = new byte[this.m_expectedLength];
            }
            else
            {
                this.m_data = new byte[this.m_fileThreshold];
            }
        }

        internal int CurrentReadIndex { get { return m_endofLine; } }

        internal NameValueCollection Headers
        {
            get
            {
                if (m_headers == null)
                {
                    m_headers = GetHeaders();
                }
                return m_headers;
            }
        }

        internal int LengthOfHeaders
        {
            get
            {
                if (m_lengthOfHeaders == -1)
                {
                    for (int x = 0; x < this.Length - 3; x++)
                    {
                        if ((this[x] == '\r') && (this[++x] == '\n') &&
                            (this[++x] == '\r') && (this[++x] == '\n'))
                        {
                            //we found the end of the headers so return the length
                            m_lengthOfHeaders = ++x;
                            break;
                        }
                    }
                }
                return m_lengthOfHeaders;
            }
        }

        // Properties
        internal byte this[int index]
        {
            get
            {
                if (!this.m_completed)
                {
                    throw new InvalidOperationException();
                }
                if (this.m_file == null)
                {
                    return this.m_data[index];
                }
                if ((index >= this.m_chunkOffset) && (index < (this.m_chunkOffset + this.m_chunkLength)))
                {
                    return this.m_data[index - this.m_chunkOffset];
                }
                if ((index < 0) || (index >= this.m_length))
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                this.m_chunkLength = this.m_file.GetBytes(index, this.m_data.Length, this.m_data, 0);
                this.m_chunkOffset = index;
                return this.m_data[0];
            }
        }

        internal bool Completed
        {
            get { return m_completed; }
        }

        internal int Length
        {
            get { return this.m_length; }
        }

        public string HttpMethod
        {
            get
            {
                if (m_httpMethod == null)
                    ReadContentInfo();
                return m_httpMethod;
            }
        }

        public string Path
        {
            get
            {
                if (m_path == null)
                    ReadContentInfo();
                return m_path;
            }
            set
            {
                m_path = value;
            }
        }

        public string HttpVersion
        {
            get
            {
                if (m_httpVersion == null)
                    ReadContentInfo();
                return m_httpVersion;
            }
        }

        public string RawQueryString
        {
            get
            {
                if (m_rawQueryString == null)
                    ReadContentInfo();
                return m_rawQueryString;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (this.m_file != null)
            {
                this.m_file.Dispose();
            }
            m_data = null;
        }

        #endregion

        internal void AddBytes(byte[] data, int offset, int length)
        {
            if (this.m_completed)
            {
                throw new InvalidOperationException();
            }
            if (length < 1)
            {
                return;
            }

            if (this.m_file == null)
            {
                if ((this.m_length + length) <= this.m_data.Length)
                {
                    Array.Copy(data, offset, this.m_data, this.m_length, length);
                    this.m_length += length;
                    return;
                }
                if ((this.m_length + length) <= this.m_fileThreshold)
                {
                    var destinationArray = new byte[this.m_fileThreshold];
                    if (this.m_length > 0)
                    {
                        Array.Copy(this.m_data, 0, destinationArray, 0, this.m_length);
                    }
                    Array.Copy(data, offset, destinationArray, this.m_length, length);
                    this.m_data = destinationArray;
                    this.m_length += length;
                    return;
                }
                this.m_file = new TempFile();
                this.m_file.AddBytes(this.m_data, 0, this.m_length);
            }
            this.m_file.AddBytes(data, offset, length);
            this.m_length += length;
        }

        internal void CopyBytes(int offset, byte[] buffer, int bufferOffset, int length)
        {
            if (!this.m_completed)
            {
                throw new InvalidOperationException();
            }

            if (this.m_file == null)
            {
                Array.Copy(this.m_data, offset, buffer, bufferOffset, length);
                return;
            }

            if ((offset >= this.m_chunkOffset) && ((offset + length) < (this.m_chunkOffset + this.m_chunkLength)))
            {
                Array.Copy(this.m_data, offset - this.m_chunkOffset, buffer, bufferOffset, length);
                return;
            }

            if (length <= this.m_data.Length)
            {
                this.m_chunkLength = this.m_file.GetBytes(offset, this.m_data.Length, this.m_data, 0);
                this.m_chunkOffset = offset;
                Array.Copy(this.m_data, offset - this.m_chunkOffset, buffer, bufferOffset, length);
                return;
            }

            this.m_file.GetBytes(offset, length, buffer, bufferOffset);
        }

        internal void DoneAddingBytes()
        {
            if (this.m_data == null)
            {
                this.m_data = new byte[0];
            }
            if (this.m_file != null)
            {
                this.m_file.DoneAddingBytes();
            }
            this.m_completed = true;
        }

        internal byte[] GetAsByteArray()
        {
            if ((this.m_file == null) && (this.m_length == this.m_data.Length))
            {
                return this.m_data;
            }
            return this.GetAsByteArray(0, this.m_length);
        }

        internal byte[] GetAsByteArray(int offset, int length)
        {
            if (!this.m_completed)
            {
                throw new InvalidOperationException();
            }
            if (length == 0)
            {
                return new byte[0];
            }
            var buffer = new byte[length];
            this.CopyBytes(offset, buffer, 0, length);
            return buffer;
        }

        internal void WriteBytes(int offset, int length, Stream stream)
        {
            if (!this.m_completed)
            {
                throw new InvalidOperationException();
            }
            if (this.m_file == null)
            {
                stream.Write(this.m_data, offset, length);
                return;
            }

            int num = offset;
            int num2 = length;
            var buffer = new byte[(num2 > this.m_fileThreshold) ? this.m_fileThreshold : num2];
            while (num2 > 0)
            {
                int num3 = (num2 > this.m_fileThreshold) ? this.m_fileThreshold : num2;
                int count = this.m_file.GetBytes(num, num3, buffer, 0);
                if (count == 0)
                {
                    return;
                }
                stream.Write(buffer, 0, count);
                num += count;
                num2 -= count;
            }
        }

        internal string ReadLine(Encoding encoding)
        {
            return ReadLine(encoding, false);
        }

        internal string ReadLine(Encoding encoding, bool reset)
        {
            if (reset)
                m_startofLine = m_endofLine = 0;

            try
            {
                //Set the start of the line to the previous end of the line
                m_startofLine = m_endofLine;

                //find the \r\n
                for (int x = m_endofLine; x < this.Length; x++)
                {
                    if (this[x] == '\r')
                    {
                        if ((x + 1 < this.Length) && (this[x + 1] == '\n'))
                        {
                            //we found the end of the line so extract it
                            x += 2; //add two bytes so it goes to the next line
                            m_endofLine = x;

                            //Return the string
                            byte[] data = GetAsByteArray(m_startofLine, m_endofLine - 2 - m_startofLine); //remove two bytes for the \r\n
                            return encoding.GetString(data, 0, data.Length);
                        }
                    }
                    //Increase the offset
                    m_endofLine++;
                }

                return null;
            }
            finally { }
        }

        internal string ReadContentInfo()
        {
            string ret = null;
            try
            {
                ret = this.ReadLine(Encoding.ASCII, true);
                if (ret == null)
                {
                    Debug.WriteLine("ret is null in ReadContentInfo()");
                    return null;
                }

                string requestLine = ret.Trim();
                string[] s = Regex.Split(requestLine, @"\s+");
                int length = s.Length;
                if (length < 1)
                {
                    return ret;
                }
                m_httpMethod = s[0];

                switch (length)
                {
                    case 2:
                        Path = s[1];
                        break;
                    case 3:
                        Path = s[1];
                        m_httpVersion = s[2];
                        break;
                    default:
                        return ret;
                }

                int q = m_path.IndexOf('?');
                if (q != -1)
                {
                    m_rawQueryString = m_path.Substring(q + 1);
                    Path = Path.Substring(0, q);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                if (Debugger.IsAttached)
                    Debugger.Break();
            }

            return ret;
        }

        private NameValueCollection GetHeaders()
        {
            var headers = new NameValueCollection();

            //the first line is the content information so just read that line
            string line = ReadLine(Encoding.ASCII, true);

            while ((line = ReadLine(Encoding.ASCII)) != null && line.Length > 0)
            {
                int sep = line.IndexOfAny(s_ColonOrNL);
                if (sep == -1 || line.Length < sep + 2)
                {
                    return headers;
                }

                // the key must be "massaged" to standard server variable notation
                string key = "HTTP_" + line.Substring(0, sep).ToUpper().Replace('-', '_');
                string value = line.Substring(sep + 1).Trim();

                headers[key] = value;
            }

            // add the HTTP_REMOTE_ADDR header
            headers["HTTP_REMOTE_ADDR"] = m_remoteClient.ToString();
            headers["REQUEST_URI"] = this.Path;

            return headers;
        }

        // Nested Types

        internal void ResetRead()
        {
            m_endofLine = m_startofLine = 0;
        }

        private class TempFile : IDisposable
        {
            // Fields
            private string m_filename;
            private Stream m_filestream;

            // Methods
            internal TempFile()
            {
                string path = ServerConfig.GetConfig().TempRoot;
                if (!Directory.Exists(path))
                {
                    try
                    {
                        Directory.CreateDirectory(path);
                    }
                    catch
                    {
                        // TODO: don't swallow this!! - log it
                        // Logging
                    }
                }
                this.m_filename = System.IO.Path.Combine(path, Guid.NewGuid().ToString() + ".post");
                this.m_filestream = new FileStream(this.m_filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            }

            #region IDisposable Members

            public void Dispose()
            {
                try
                {
                    //Close the stream
                    if (this.m_filestream != null)
                        this.m_filestream.Close();
                    //Delete the file
                    if (File.Exists(m_filename))
                        File.Delete(m_filename);
                }
                catch
                {
                }
            }

            #endregion

            internal void AddBytes(byte[] data, int offset, int length)
            {
                if (this.m_filestream == null)
                {
                    throw new InvalidOperationException();
                }
                this.m_filestream.Write(data, offset, length);
            }

            internal void DoneAddingBytes()
            {
                if (this.m_filestream == null)
                {
                    throw new InvalidOperationException();
                }
                this.m_filestream.Flush();
                this.m_filestream.Seek(0L, SeekOrigin.Begin);
            }

            internal int GetBytes(int offset, int length, byte[] buffer, int bufferOffset)
            {
                if (this.m_filestream == null)
                {
                    throw new InvalidOperationException();
                }
                this.m_filestream.Seek((long)offset, SeekOrigin.Begin);
                return this.m_filestream.Read(buffer, bufferOffset, length);
            }
        }
    }
}
