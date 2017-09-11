﻿using System.IO;
using WebSockets.Common.Common;

namespace WebSockets.Server.Http
{
    public class BadRequestService : IService
    {
        private readonly Stream _stream;
        private readonly string _header;
        private readonly IWebSocketLogger _logger;

        public BadRequestService(Stream stream, string header, IWebSocketLogger logger)
        {
            _stream = stream;
            _header = header;
            _logger = logger;
        }

        public void Respond()
        {
            HttpHelper.WriteHttpHeader("HTTP/1.1 400 Bad Request", _stream);

            // limit what we log. Headers can be up to 16K in size
            var header = _header.Length > 255 ? _header.Substring(0,255) + "..." : _header;
            _logger.Warning(GetType(), "Bad request: '{0}'", header);
        }

        public void Dispose()
        {
            // do nothing
        }
    }
}