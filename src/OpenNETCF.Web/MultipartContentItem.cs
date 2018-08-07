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

using System.Text;

namespace OpenNETCF.Web
{
    internal sealed class MultipartContentItem
    {
        // Fields
        private string m_contentType;
        private HttpRawRequestContent m_data;
        private string m_filename;
        private int m_length;
        private string m_name;
        private int m_offset;

        // Methods
        internal MultipartContentItem(string name, string filename, string contentType, HttpRawRequestContent data, int offset, int length)
        {
            m_name = name;
            m_filename = filename;
            m_contentType = contentType;
            m_data = data;
            m_offset = offset;
            m_length = length;
        }

        // Properties
        internal bool IsFile
        {
            get { return (m_filename != null); }
        }

        internal bool IsFormItem
        {
            get { return (m_filename == null); }
        }

        internal string Name
        {
            get { return m_name; }
        }

        internal HttpPostedFile GetAsPostedFile()
        {
            return new HttpPostedFile(m_filename, m_contentType, new HttpInputStream(m_data, m_offset, m_length));
        }

        internal string GetAsString(Encoding encoding)
        {
            if (m_length > 0)
            {
                byte[] data = m_data.GetAsByteArray(m_offset, m_length);
                return encoding.GetString(data, 0, data.Length);
            }
            return string.Empty;
        }
    }
}
