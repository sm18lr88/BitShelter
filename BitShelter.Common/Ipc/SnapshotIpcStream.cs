using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace BitShelter.Ipc
{
  public static class SnapshotIpcStream
  {
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static void WriteJson<T>(Stream stream, T payload)
    {
      if (stream == null)
        throw new ArgumentNullException(nameof(stream));

      string json = JsonConvert.SerializeObject(payload);
      byte[] data = Utf8NoBom.GetBytes(json);
      if (data.Length > SnapshotIpcProtocol.MaxMessageBytes)
        throw new InvalidDataException($"IPC message of {data.Length} bytes exceeds the {SnapshotIpcProtocol.MaxMessageBytes} byte limit.");

      WriteInt32(stream, data.Length);
      stream.Write(data, 0, data.Length);
      stream.Flush();
    }

    public static T ReadJson<T>(Stream stream)
    {
      if (stream == null)
        throw new ArgumentNullException(nameof(stream));

      int length = ReadInt32(stream);
      if (length < 0 || length > SnapshotIpcProtocol.MaxMessageBytes)
        throw new InvalidDataException("Invalid message length.");

      byte[] data = ReadExact(stream, length);
      string json = Utf8NoBom.GetString(data);
      return JsonConvert.DeserializeObject<T>(json);
    }

    private static void WriteInt32(Stream stream, int value)
    {
      byte[] buf = BitConverter.GetBytes(value);
      stream.Write(buf, 0, buf.Length);
    }

    private static int ReadInt32(Stream stream)
    {
      byte[] buf = ReadExact(stream, 4);
      return BitConverter.ToInt32(buf, 0);
    }

    private static byte[] ReadExact(Stream stream, int length)
    {
      byte[] buffer = new byte[length];
      int offset = 0;
      while (offset < length)
      {
        int read = stream.Read(buffer, offset, length - offset);
        if (read <= 0)
          throw new EndOfStreamException();
        offset += read;
      }

      return buffer;
    }
  }
}

