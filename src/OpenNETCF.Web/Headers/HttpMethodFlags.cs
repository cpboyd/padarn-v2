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
using System.Collections.Generic;
using System.Text;

namespace OpenNETCF.Web
{
    /// <summary>
    /// <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods">HTTP request methods</a>
    /// </summary>
    [Flags]
    public enum HttpMethodFlags
    {
        /// <summary>
        /// Unknown HTTP method
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// The GET method requests a representation of the specified resource. Requests using GET should only retrieve data.
        /// </summary>
        Get = 1,
        /// <summary>
        /// The PUT method replaces all current representations of the target resource with the request payload.
        /// </summary>
        Put = Get * 2,
        /// <summary>
        /// The HEAD method asks for a response identical to that of a GET request, but without the response body.
        /// </summary>
        Head = Put * 2,
        /// <summary>
        /// The POST method is used to submit an entity to the specified resource, often causing a change in state or side effects on the server.
        /// </summary>
        Post = Head * 2,
        /// <summary>
        /// When the client tries to automatically attach the debugger in an ASP.NET 2.0 application, the client sends a HTTP request that contains the DEBUG verb.
        /// </summary>
        Debug = Post * 2,
        /// <summary>
        /// The DELETE method deletes the specified resource.
        /// </summary>
        Delete = Debug * 2,
        /// <summary>
        /// The CONNECT method establishes a tunnel to the server identified by the target resource.
        /// </summary>
        Connect = Delete * 2,
        /// <summary>
        /// The OPTIONS method is used to describe the communication options for the target resource.
        /// </summary>
        Options = Connect * 2,
        /// <summary>
        /// The TRACE method performs a message loop-back test along the path to the target resource.
        /// </summary>
        Trace = Options * 2,
        /// <summary>
        /// The PATCH method is used to apply partial modifications to a resource.
        /// </summary>
        Patch = Trace * 2,
        /// <summary>
        /// Matches any HTTP method
        /// </summary>
        Any = Get + Put + Head + Post + Debug + Delete + Connect + Options + Trace + Patch
    }
}
