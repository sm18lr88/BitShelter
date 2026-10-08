using System;
using System.IO;

namespace BitShelter.Backup
{
  // Write-only pass-through stream. It stops the backup when the output grows beyond the size limit.
  // It can seek when the underlying stream can (zip writers seek back to complete Zip64 headers).
  // The underlying stream stays open when this stream is disposed.
  public sealed class SizeLimitStream : Stream
  {
    private readonly Stream inner;
    private readonly long maxBytes;

    public SizeLimitStream(Stream inner, long maxBytes)
    {
      this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
      this.maxBytes = maxBytes;
    }

    public override bool CanRead => false;
    public override bool CanSeek => inner.CanSeek;
    public override bool CanWrite => true;
    public override long Length => inner.Length;
    public override long Position { get => inner.Position; set => inner.Position = value; }

    public override void Write(byte[] buffer, int offset, int count)
    {
      Write(new ReadOnlySpan<byte>(buffer, offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
      long end = (inner.CanSeek ? inner.Position : writtenWithoutSeek) + buffer.Length;

      if (maxBytes > 0 && end > maxBytes)
        throw new BackupSizeLimitException(maxBytes);

      inner.Write(buffer);
      writtenWithoutSeek += buffer.Length;
    }

    public override void WriteByte(byte value)
    {
      Write(new ReadOnlySpan<byte>(new[] { value }));
    }

    public override void Flush() => inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    private long writtenWithoutSeek;
  }
}
