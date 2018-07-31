//
// Copyright ©2018 Christopher Boyd
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenNETCF.Web.Core;

namespace OpenNETCF.Web
{
    /// <summary>
    /// <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods">HTTP request methods</a>
    /// </summary>
    public class HttpMethod
    {
        /// <summary>
        /// The GET method requests a representation of the specified resource. Requests using GET should only retrieve data.
        /// </summary>
        public const string Get = "GET";

        /// <summary>
        /// The HEAD method asks for a response identical to that of a GET request, but without the response body.
        /// </summary>
        public const string Head = "HEAD";

        /// <summary>
        /// The POST method is used to submit an entity to the specified resource, often causing a change in state or side effects on the server.
        /// </summary>
        public const string Post = "POST";

        /// <summary>
        /// The PUT method replaces all current representations of the target resource with the request payload.
        /// </summary>
        public const string Put = "PUT";

        /// <summary>
        /// The DELETE method deletes the specified resource.
        /// </summary>
        public const string Delete = "DELETE";

        /// <summary>
        /// The CONNECT method establishes a tunnel to the server identified by the target resource.
        /// </summary>
        public const string Connect = "CONNECT";

        /// <summary>
        /// The OPTIONS method is used to describe the communication options for the target resource.
        /// </summary>
        public const string Options = "OPTIONS";

        /// <summary>
        /// The TRACE method performs a message loop-back test along the path to the target resource.
        /// </summary>
        public const string Trace = "TRACE";

        /// <summary>
        /// The PATCH method is used to apply partial modifications to a resource.
        /// </summary>
        public const string Patch = "PATCH";

        /// <summary>
        /// When the client tries to automatically attach the debugger in an ASP.NET 2.0 application, the client sends a HTTP request that contains the DEBUG verb.
        /// </summary>
        public const string Debug = "DEBUG";

        /// <summary>
        /// Parses comma-delimited HTTP methods specified on a HttpHandler's verb attribute into HttpMethodFlags.
        /// </summary>
        /// <param name="methods">Comma-delimited HTTP methods specified on a HttpHandler's verb attribute.</param>
        /// <returns>HttpMethodFlags corresponding to all specified HTTP methods.</returns>
        public static HttpMethodFlags GetFlags(string methods)
        {
            if (methods == "*")
            {
                return HttpMethodFlags.Any;
            }

            return methods.Split(',')
                .Aggregate(HttpMethodFlags.Unknown, (current, method) => current | ParseFlag(method.Trim()));
        }

        public static HttpMethodFlags ParseFlag(string method)
        {
            switch (method)
            {
                case Get:
                    return HttpMethodFlags.Get;
                case Head:
                    return HttpMethodFlags.Head;
                case Post:
                    return HttpMethodFlags.Post;
                case Put:
                    return HttpMethodFlags.Put;
                case Delete:
                    return HttpMethodFlags.Delete;
                case Connect:
                    return HttpMethodFlags.Connect;
                case Options:
                    return HttpMethodFlags.Options;
                case Trace:
                    return HttpMethodFlags.Trace;
                case Patch:
                    return HttpMethodFlags.Patch;
                case Debug:
                    return HttpMethodFlags.Debug;
                default:
                    return HttpMethodFlags.Unknown;
            }
        }

        public static string GetVerbName(HttpMethodFlags method)
        {
            switch (method)
            {
                case HttpMethodFlags.Get:
                    return Get;
                case HttpMethodFlags.Head:
                    return Head;
                case HttpMethodFlags.Post:
                    return Post;
                case HttpMethodFlags.Put:
                    return Put;
                case HttpMethodFlags.Delete:
                    return Delete;
                case HttpMethodFlags.Connect:
                    return Connect;
                case HttpMethodFlags.Options:
                    return Options;
                case HttpMethodFlags.Trace:
                    return Trace;
                case HttpMethodFlags.Patch:
                    return Patch;
                case HttpMethodFlags.Debug:
                    return Debug;
                default:
                    return "Unknown";
            }
        }
    }
    public static class HttpMethodExtensions
    {
        public static string GetVerbName(this HttpMethodFlags method)
        {
            return HttpMethod.GetVerbName(method);
        }
    }
}
