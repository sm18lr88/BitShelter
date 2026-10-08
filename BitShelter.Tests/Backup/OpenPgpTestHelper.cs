using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace BitShelter.Tests.Backup
{
  // Creates a throwaway OpenPGP key and decrypts messages, as GnuPG would.
  internal static class OpenPgpTestHelper
  {
    public static (string ArmoredPublicKey, PgpSecretKeyRing SecretRing, char[] KeyPassphrase) CreateKey()
    {
      var random = new SecureRandom();
      var generator = new RsaKeyPairGenerator();
      generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), random, 2048, 12));

      var keyPair = new PgpKeyPair(PublicKeyAlgorithmTag.RsaGeneral, generator.GenerateKeyPair(), DateTime.UtcNow);
      char[] passphrase = "key-passphrase".ToCharArray();
      var ringGenerator = new PgpKeyRingGenerator(PgpSignature.PositiveCertification, keyPair, "BitShelter test <test@example.org>",
        SymmetricKeyAlgorithmTag.Aes256, passphrase, true, null, null, random);

      using var armored = new MemoryStream();
      using (var armor = new ArmoredOutputStream(armored))
        ringGenerator.GeneratePublicKeyRing().Encode(armor);

      return (System.Text.Encoding.ASCII.GetString(armored.ToArray()), ringGenerator.GenerateSecretKeyRing(), passphrase);
    }

    public static byte[] DecryptWithPassphrase(byte[] message, string passphrase, out string literalName)
    {
      PgpPbeEncryptedData data = ReadEncryptedData(message).OfType<PgpPbeEncryptedData>().Single();

      using Stream clear = data.GetDataStreamUtf8(passphrase.ToCharArray());
      byte[] result = ReadLiteral(clear, out literalName);

      Assert.True(data.IsIntegrityProtected());
      Assert.True(data.Verify());
      return result;
    }

    public static byte[] DecryptWithKey(byte[] message, PgpSecretKeyRing secretRing, char[] keyPassphrase, out string literalName)
    {
      PgpPublicKeyEncryptedData data = ReadEncryptedData(message).OfType<PgpPublicKeyEncryptedData>().Single();
      PgpPrivateKey privateKey = secretRing.GetSecretKey(data.KeyId).ExtractPrivateKey(keyPassphrase);

      using Stream clear = data.GetDataStream(privateKey);
      byte[] result = ReadLiteral(clear, out literalName);

      Assert.True(data.IsIntegrityProtected());
      Assert.True(data.Verify());
      return result;
    }

    private static IEnumerable<PgpEncryptedData> ReadEncryptedData(byte[] message)
    {
      var factory = new PgpObjectFactory(PgpUtilities.GetDecoderStream(new MemoryStream(message)));
      PgpObject first = factory.NextPgpObject();
      var list = first as PgpEncryptedDataList ?? (PgpEncryptedDataList)factory.NextPgpObject();

      return list.GetEncryptedDataObjects();
    }

    private static byte[] ReadLiteral(Stream clear, out string literalName)
    {
      var literal = (PgpLiteralData)new PgpObjectFactory(clear).NextPgpObject();
      literalName = literal.FileName;

      using var content = new MemoryStream();
      literal.GetInputStream().CopyTo(content);
      return content.ToArray();
    }
  }
}
