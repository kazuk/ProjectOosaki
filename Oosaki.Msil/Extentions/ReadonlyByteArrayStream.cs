using System;
using System.IO;

namespace Oosaki.Msil.Extentions
{
    public class ReadonlyByteArrayStream : Stream
    {
        private readonly byte[] _bytes;
        private long _position;

        public ReadonlyByteArrayStream(byte[] bytes)
        {
            _bytes = bytes;
            _position = 0;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = _bytes.LongLength + offset;
                    break;
            }
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException( "ReadonlyByteArrayStream.SetLength not supported");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readable = _bytes.LongLength - _position;
            if (readable < count)
            {
                count = (int)readable;
            }
            Buffer.BlockCopy(_bytes,(int)_position, buffer, offset, count);
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("ReadonlyByteArrayStream.Write not supported");
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _bytes.LongLength; }
        }

        public override long Position
        {
            get { return _position; }
            set { _position = value; }
        }
    }
}