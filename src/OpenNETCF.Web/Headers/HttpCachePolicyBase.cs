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

namespace OpenNETCF.Web
{
    /// <summary>
    /// Serves as the base class for classes that contain methods for setting cache-specific HTTP headers and for controlling the ASP.NET page output cache.
    /// </summary>
    public abstract class HttpCachePolicyBase
    {
        /// <summary>
        /// When overridden in a derived class, sets the Cache-Control: s-maxage HTTP header to the specified time span.
        /// </summary>
        /// <param name="delta"></param>
        public virtual void SetProxyMaxAge(TimeSpan delta)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// When overridden in a derived class, registers a validation callback for the current response.
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="data"></param>
        public virtual void AddValidationCallback(HttpCacheValidateHandler handler, Object data)
        {
            throw new NotImplementedException();
        }
    }
}
