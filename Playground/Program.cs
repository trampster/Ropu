// See https://aka.ms/new-console-template for more information
using System.Security.Cryptography;

Console.WriteLine("Hello, World!");


// CngKey key = CngKey.Create(CngAlgorithm.ECDiffieHellmanP256, null, new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextExport });
// var privateKey = key.Export(CngKeyBlobFormat.EccPrivateBlob);
// var publicKey = key.Export(CngKeyBlobFormat.EccPublicBlob);

// var importedKey = CngKey.Import(privateKey, CngKeyBlobFormat.EccFullPrivateBlob);
// var privateKeyImported = key.Export(CngKeyBlobFormat.EccPrivateBlob);
// var publicKeyImported = key.Export(CngKeyBlobFormat.EccPublicBlob);

//https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.ecdiffiehellmancng?view=net-6.0#examples
using (ECDiffieHellmanOpenSsl alice = new ECDiffieHellmanOpenSsl())
using (ECDiffieHellmanOpenSsl aliceImported = new ECDiffieHellmanOpenSsl())
{
    string privateKey = alice.ExportECPrivateKeyPem();
    aliceImported.ImportFromPem(privateKey);

    var publicKey = alice.ExportSubjectPublicKeyInfoPem();
    var importedPublicKey = aliceImported.ExportSubjectPublicKeyInfoPem();

    bool areEqual = publicKey == importedPublicKey;
    Console.WriteLine("asdf");

    // alicePublicKey = alice.PublicKey.ToByteArray();
    // Bob bob = new Bob();
    // CngKey bobKey = CngKey.Import(bob.bobPublicKey, CngKeyBlobFormat.EccPublicBlob);
    // byte[] aliceKey = alice.DeriveKeyMaterial(bobKey);
    // byte[] encryptedMessage = null;
    // byte[] iv = null;
    // Send(aliceKey, "Secret message", out encryptedMessage, out iv);
    // bob.Receive(encryptedMessage, iv);
}