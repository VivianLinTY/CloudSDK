using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cloud.Client
{
    internal class ZHttpContent : HttpContent
    {
        private const int defaultBufferSize = 1024 * 1024;

        private readonly HttpContent content;
        private readonly int bufferSize;
        private readonly Action<long, long> progress;

        public ZHttpContent(HttpContent content, Action<long, long> progress) :
            this(content, defaultBufferSize, progress)
        { }

        public ZHttpContent(HttpContent content, int bufferSize,
            Action<long, long> progress)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

            this.content = content ?? throw new ArgumentNullException("content");
            this.bufferSize = bufferSize;
            this.progress = progress;

            foreach (var header in content.Headers)
            {
                this.Headers.Add(header.Key, header.Value);
            }
        }

        protected override Task SerializeToStreamAsync(
            Stream stream, TransportContext context)
        {
            return Task.Run(async () =>
            {
                var buffer = new byte[bufferSize];
                TryComputeLength(out long size);
                var uploaded = 0;

                using (var sinput = await content.ReadAsStreamAsync())
                {
                    while (true)
                    {
                        var length = sinput.Read(buffer, 0, buffer.Length);
                        if (length <= 0) break;

                        uploaded += length;
                        progress?.Invoke(uploaded, size);

                        stream.Write(buffer, 0, length);
                        stream.Flush();
                    }
                }
                stream.Flush();
            });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.Headers.ContentLength.GetValueOrDefault();
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                content.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}