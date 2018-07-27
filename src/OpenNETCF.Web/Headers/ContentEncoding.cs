//
// Copyright ©2018 Christopher Boyd
//

namespace OpenNETCF.Web.Headers
{
    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Encoding
    class ContentEncoding
    {
        // A format using the Lempel-Ziv coding (LZ77), with a 32-bit CRC.
        // This is the original format of the UNIX gzip program.
        public const string Gzip = "gzip";

        // Using the zlib structure (defined in RFC 1950) with the deflate compression algorithm
        // (defined in RFC 1951).
        public const string Deflate = "deflate";

        // Indicates the identity function (i.e., no compression or modification).
        // This token, except if explicitly specified, is always deemed acceptable.
        public const string Identity = "identity";

        // A format using the Brotli algorithm.
        public const string Brotli = "br";
    }
}
