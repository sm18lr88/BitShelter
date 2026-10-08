using BitShelter.Models;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BitShelter.Encryption
{
  // Writes standard OpenPGP messages: an encrypted data packet with an integrity check (MDC)
  // that contains one literal data packet. "gpg --decrypt" and Kleopatra can read them.
  public static class OpenPgpEncryption
  {
    private const int BufferSize = 1 << 16;

    public const EncryptionAlgorithm DefaultAlgorithm = EncryptionAlgorithm.AES256_CFB;

    // The symmetric ciphers that OpenPGP supports. The other EncryptionAlgorithm values stay only so that old rule files load.
    public static readonly IReadOnlyList<EncryptionAlgorithm> Ciphers = new[]
    {
      EncryptionAlgorithm.AES128_CFB,
      EncryptionAlgorithm.AES256_CFB,
      EncryptionAlgorithm.Blowfish_CFB,
      EncryptionAlgorithm.Camellia128_CFB,
      EncryptionAlgorithm.Camellia256_CFB,
      EncryptionAlgorithm.Cast5_CFB,
      EncryptionAlgorithm.TripleDes_CFB,
      EncryptionAlgorithm.Twofish_CFB,
    };

    public static Stream OpenForPublicKey(Stream output, string armoredPublicKey, EncryptionAlgorithm algorithm, string fileName)
    {
      var generator = CreateGenerator(algorithm);
      generator.AddMethod(FindEncryptionKey(armoredPublicKey));

      return Open(generator, output, fileName);
    }

    public static Stream OpenForPassphrase(Stream output, string passphrase, EncryptionAlgorithm algorithm, string fileName)
    {
      if (string.IsNullOrEmpty(passphrase))
        throw new ArgumentException("The passphrase is empty.", nameof(passphrase));

      var generator = CreateGenerator(algorithm);
      generator.AddMethodUtf8(passphrase.ToCharArray(), HashAlgorithmTag.Sha256);

      return Open(generator, output, fileName);
    }

    // Returns the first key in the key ring that can encrypt and is not revoked.
    public static PgpPublicKey FindEncryptionKey(string armoredPublicKey)
    {
      if (string.IsNullOrWhiteSpace(armoredPublicKey))
        throw new ArgumentException("The OpenPGP public key is empty.", nameof(armoredPublicKey));

      using (var keyStream = new MemoryStream(Encoding.ASCII.GetBytes(armoredPublicKey)))
      {
        var bundle = new PgpPublicKeyRingBundle(PgpUtilities.GetDecoderStream(keyStream));

        return bundle.GetKeyRings()
                     .SelectMany(ring => ring.GetPublicKeys())
                     .FirstOrDefault(k => k.IsEncryptionKey && !k.HasRevocation())
          ?? throw new ArgumentException("The OpenPGP key ring has no encryption key.", nameof(armoredPublicKey));
      }
    }

    public static bool IsSupported(EncryptionAlgorithm algorithm)
    {
      return Ciphers.Contains(algorithm);
    }

    internal static SymmetricKeyAlgorithmTag GetCipher(EncryptionAlgorithm algorithm)
    {
      switch (algorithm)
      {
        case EncryptionAlgorithm.AES128_CFB:
          return SymmetricKeyAlgorithmTag.Aes128;

        case EncryptionAlgorithm.AES256_CFB:
          return SymmetricKeyAlgorithmTag.Aes256;

        case EncryptionAlgorithm.Blowfish_CFB:
          return SymmetricKeyAlgorithmTag.Blowfish;

        case EncryptionAlgorithm.Camellia128_CFB:
          return SymmetricKeyAlgorithmTag.Camellia128;

        case EncryptionAlgorithm.Camellia256_CFB:
          return SymmetricKeyAlgorithmTag.Camellia256;

        case EncryptionAlgorithm.Cast5_CFB:
          return SymmetricKeyAlgorithmTag.Cast5;

        case EncryptionAlgorithm.TripleDes_CFB:
          return SymmetricKeyAlgorithmTag.TripleDes;

        case EncryptionAlgorithm.Twofish_CFB:
          return SymmetricKeyAlgorithmTag.Twofish;

        default:
          throw new ArgumentException(String.Format("Unsupported EncryptionAlgorithm {0}", algorithm), "algorithm");
      }
    }

    private static PgpEncryptedDataGenerator CreateGenerator(EncryptionAlgorithm algorithm)
    {
      if (algorithm == EncryptionAlgorithm.None)
        algorithm = DefaultAlgorithm;

      return new PgpEncryptedDataGenerator(GetCipher(algorithm), withIntegrityPacket: true, new SecureRandom());
    }

    private static Stream Open(PgpEncryptedDataGenerator generator, Stream output, string fileName)
    {
      Stream encrypted = generator.Open(output, new byte[BufferSize]);
      Stream literal = new PgpLiteralDataGenerator().Open(encrypted, PgpLiteralData.Binary, fileName, DateTime.UtcNow, new byte[BufferSize]);

      return new MessageStream(literal, encrypted);
    }

    // Disposing this stream finishes the literal packet and then the encrypted packet. The output stream stays open.
    private sealed class MessageStream : Stream
    {
      private readonly Stream literal;
      private readonly Stream encrypted;
      private bool disposed;

      public MessageStream(Stream literal, Stream encrypted)
      {
        this.literal = literal;
        this.encrypted = encrypted;
      }

      public override bool CanRead => false;
      public override bool CanSeek => false;
      public override bool CanWrite => true;
      public override long Length => throw new NotSupportedException();
      public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

      public override void Write(byte[] buffer, int offset, int count) => literal.Write(buffer, offset, count);
      public override void Write(ReadOnlySpan<byte> buffer) => literal.Write(buffer);
      public override void Flush() => literal.Flush();
      public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
      public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
      public override void SetLength(long value) => throw new NotSupportedException();

      protected override void Dispose(bool disposing)
      {
        if (disposing && !disposed)
        {
          disposed = true;
          literal.Dispose();
          encrypted.Dispose();
        }

        base.Dispose(disposing);
      }
    }
  }
}
