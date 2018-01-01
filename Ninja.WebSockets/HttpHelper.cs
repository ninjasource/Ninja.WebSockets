// ---------------------------------------------------------------------
// Copyright 2017 David Haig
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
// ---------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ninja.WebSockets.Exceptions;

namespace Ninja.WebSockets
{
    using System.Buffers;

    public class HttpHelper
    {
        private static readonly Regex _HTTP_GET_HEADER_REGEX = 
            new Regex(@"^GET(.*)HTTP\/1\.1", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        private static readonly Regex _GET_REGEX = 
            new Regex(@"HTTP\/1\.1 (.*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _UPGRADE_REGEX = 
            new Regex("Upgrade: websocket", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly ThreadLocal<SHA1> _HASHER = new ThreadLocal<SHA1>(SHA1.Create);
        private static readonly ThreadLocal<Random> _RANDOM = new ThreadLocal<Random>(() => new Random((int)DateTime.Now.Ticks));

        private static readonly ArrayPool<byte> _POOL = ArrayPool<byte>.Shared;

        /// <summary>
        /// Calculates a random WebSocket key that can be used to initiate a WebSocket handshake
        /// </summary>
        /// <returns>A random websocket key</returns>
        public static string CalculateWebSocketKey()
        {
            // this is not used for cryptography so doing something simple like the code below is op
            byte[] keyAsBytes = new byte[16];
            _RANDOM.Value.NextBytes(keyAsBytes);
            return Convert.ToBase64String(keyAsBytes);
        }        

        /// <summary>
        /// Computes a WebSocket accept string from a given key
        /// </summary>
        /// <param name="secWebSocketKey">The web socket key to base the accept string on</param>
        /// <returns>A web socket accept string</returns>
        public static string ComputeSocketAcceptString(string secWebSocketKey)
        {
            // this is a guid as per the web socket spec
            const string webSocketGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string concatenated = secWebSocketKey + webSocketGuid;
            byte[] concatenatedAsBytes = Encoding.UTF8.GetBytes(concatenated);

            // note an instance of SHA1 is not threadsafe so we have to create a new one every time here
            byte[] sha1Hash = _HASHER.Value.ComputeHash(concatenatedAsBytes);
            string secWebSocketAccept = Convert.ToBase64String(sha1Hash);
            return secWebSocketAccept;
        }

        /// <summary>
        /// Reads an http header as per the HTTP spec
        /// </summary>
        /// <param name="stream">The stream to read UTF8 text from</param>
        /// <param name="token">The cancellation token</param>
        /// <returns>The HTTP header</returns>
        public static async Task<string> ReadHttpHeaderAsync(Stream stream, CancellationToken token)
        {
            const int Length = 1024*16; // 16KB buffer more than enough for http header
            
            byte[] buffer = _POOL.Rent(Length);
            int offset = 0;

            try
            {
                int bytesRead;
                do
                {
                    if (offset >= Length)
                    {
                        throw new EntityTooLargeException("Http header message too large to fit in buffer (16KB)");
                    }

                    bytesRead = await stream.ReadAsync(buffer, offset, Length - offset, token).ConfigureAwait(false);
                    offset += bytesRead;
                    string header = Encoding.UTF8.GetString(buffer, 0, offset);

                    // as per http specification, all headers should end this
                    if (header.Contains("\r\n\r\n"))
                    {
                        return header;
                    }

                } while (bytesRead > 0);
            } finally
            {
                _POOL.Return(buffer);
            }

            return string.Empty;
        }

        /// <summary>
        /// Decodes the header to detect is this is a web socket upgrade response
        /// </summary>
        /// <param name="header">The HTTP header</param>
        /// <returns>True if this is an http WebSocket upgrade response</returns>
        public static bool IsWebSocketUpgradeRequest(String header)
        {
            Match getRegexMatch = _HTTP_GET_HEADER_REGEX.Match(header);

            if (getRegexMatch.Success)
            {
                // check if this is a web socket upgrade request
                Match webSocketUpgradeRegexMatch = _UPGRADE_REGEX.Match(header);
                return webSocketUpgradeRegexMatch.Success;
            }

            return false;
        }

        /// <summary>
        /// Gets the path from the HTTP header
        /// </summary>
        /// <param name="httpHeader">The HTTP header to read</param>
        /// <returns>The path</returns>
        public static string GetPathFromHeader(string httpHeader)
        {
            Match getRegexMatch = _HTTP_GET_HEADER_REGEX.Match(httpHeader);

            if (getRegexMatch.Success)
            {
                // extract the path attribute from the first line of the header
                return getRegexMatch.Groups[1].Value.Trim(); // [ToDo] - Improve REGEX to avoid Trim()
            }

            return null;
        }

        /// <summary>
        /// Reads the HTTP response code from the http response string
        /// </summary>
        /// <param name="response">The response string</param>
        /// <returns>the response code</returns>
        public static string ReadHttpResponseCode(string response)
        {
            Match getRegexMatch = _GET_REGEX.Match(response);

            if (getRegexMatch.Success)
            {
                // extract the path attribute from the first line of the header
                return getRegexMatch.Groups[1].Value.Trim(); // [ToDo] - Improve REGEX to avoid Trim()
            }

            return null;
        }

        /// <summary>
        /// Writes an HTTP response string to the stream
        /// </summary>
        /// <param name="response">The response (without the new line characters)</param>
        /// <param name="stream">The stream to write to</param>
        /// <param name="token">The cancellation token</param>
        public static Task WriteHttpHeaderAsync(string response, Stream stream, CancellationToken token)
        {
            response = response.Trim() + "\r\n\r\n"; // [ToDo] - Consider avoiding Trim()
            Byte[] bytes = Encoding.UTF8.GetBytes(response);
            return stream.WriteAsync(bytes, 0, bytes.Length, token);
        }
    }
}
