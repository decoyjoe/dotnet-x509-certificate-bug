
# Title

Unexpected deletion of persisted private key on disposing X509Certificate2 object

# Description

Install a certificate and its private key from a PFX file to the `LocalMachine` store with the `MachineKeySet | PersistKeySet` key storage flags. Close the process. The private key is persistently stored on disk under `C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\`.

Start a process as the local `NT AUTHORITY\SYSTEM` user and read the certificate PFX file again, creating an `X509Certificate2` object.

The bug manifests when the `.Dispose()` method is called on the `X509Certificate2` object. The private key container is unexpectedly deleted from disk, from the `C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\` directory, even though it was installed with the `PersistKeySet` flag.

Any subsequent attempts to access or read the private key, even in new processes, result in errors stating "Keyset does not exist." To resolve this issue, the certificate and its private key must be re-installed.

# Reproduction Steps

Run the following program as the local `NT AUTHORITY\SYSTEM` user. A base64 encoded certificate has been included for convenience.

```csharp
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DotnetX509CertificateBug
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine();
            Console.WriteLine("Reproducing dotnet/runtime #97121");
            Console.WriteLine();

            var currentUser = WindowsIdentity.GetCurrent().Name;

            if (!currentUser.Equals(@"NT AUTHORITY\SYSTEM", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Only reproducible when running as the SYSTEM user but current user is {currentUser}. Please re-run as SYSTEM.");
            }

            Console.WriteLine($"Running as: {currentUser}");
            Console.WriteLine($"{RuntimeInformation.OSDescription}");
            Console.WriteLine($"{RuntimeInformation.FrameworkDescription}");
            Console.WriteLine();

            string pfxBase64 = """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""";
            string password = "password";

            var pfxBytes = Convert.FromBase64String(pfxBase64);
            var keyFlags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;
            var certToInstall = new X509Certificate2(pfxBytes, password, keyFlags);

            var thumbprint = certToInstall.Thumbprint;
            var privateKey = RSACertificateExtensions.GetRSAPrivateKey(certToInstall);
            var keyContainer = ((RSACng)privateKey).Key.UniqueName;

            Console.WriteLine($"Install certificate to LocalMachine/My store with X509KeyStorageFlags: {keyFlags}");
            Console.WriteLine($"  Subject: {certToInstall.Subject}");
            Console.WriteLine($"  Subject: {certToInstall.Thumbprint}");
            Console.WriteLine();

            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(certToInstall);
            store.Close();
            store.Dispose();
            certToInstall.Dispose();

            var privateKeyFilePath = $"C:\\ProgramData\\Microsoft\\Crypto\\RSA\\MachineKeys\\{keyContainer}";

            Console.WriteLine("Check certificate private key:");
            Console.WriteLine($"  Path: {privateKeyFilePath}");
            Console.WriteLine($"  Exists: {Path.Exists(privateKeyFilePath)}");
            Console.WriteLine();

            Console.WriteLine("Now read certificate again from PFX and then dispose it:");
            var certFromPfx = new X509Certificate2(pfxBytes, password);
            certFromPfx.Dispose();
            Console.WriteLine("  Done.");
            Console.WriteLine();

            Console.WriteLine("Check certificate private key again:");
            Console.WriteLine($"  Path: {privateKeyFilePath}");
            Console.WriteLine($"  Exists: {Path.Exists(privateKeyFilePath)}");
            Console.WriteLine();

            store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var installedCert = store.Certificates.Where(cert => cert.Thumbprint == thumbprint).First();
            Console.WriteLine("Try to get the private key with GetRSAPrivateKey():");
            Console.WriteLine(RSACertificateExtensions.GetRSAPrivateKey(installedCert));
        }
    }
}
```

For a fully automated reproduction in a fresh virtual machine, [see this repository here](https://github.com/decoyjoe/dotnet-x509-certificate-bug).

# Expected Behavior

The private key container under `C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\` remains after disposing the certificate object.

# Actual Behavior

The private key container no longer exists under `C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\`.

Here's the output from the reproduction program:

```
Reproducing dotnet/runtime #97121

Running as: NT AUTHORITY\SYSTEM
Microsoft Windows 10.0.22631
.NET 8.0.1

Install certificate to LocalMachine/My store with X509KeyStorageFlags: MachineKeySet, PersistKeySet
  Subject: CN="dotnet-runtime-#97121"
  Subject: C6CDE0BCFC3F49C319CDA872AC1602D9D5FE621B

Check certificate private key:
  Path: C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\3783dffe86057ae296d14ef8601e9142_eead0bb6-08ac-432d-82ec-f0b5657e3223
  Exists: True

Now read certificate again from PFX and then dispose it:
  Done.

Check certificate private key again:
  Path: C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\3783dffe86057ae296d14ef8601e9142_eead0bb6-08ac-432d-82ec-f0b5657e3223
  Exists: False

Try to get the private key with GetRSAPrivateKey():
Unhandled exception. System.Security.Cryptography.CryptographicException: Keyset does not exist
   at System.Security.Cryptography.CapiHelper.CreateProvHandle(CspParameters parameters, Boolean randomKeyContainer)
   at System.Security.Cryptography.RSACryptoServiceProvider.get_SafeProvHandle()
   at System.Security.Cryptography.RSACryptoServiceProvider.get_SafeKeyHandle()
   at System.Security.Cryptography.RSACryptoServiceProvider..ctor(Int32 keySize, CspParameters parameters, Boolean useDefaultKeySize)
   at System.Security.Cryptography.X509Certificates.CertificatePal.<>c.<GetRSAPrivateKey>b__68_0(CspParameters csp)
   at System.Security.Cryptography.X509Certificates.CertificatePal.GetPrivateKey[T](Func`2 createCsp, Func`2 createCng)
   at System.Security.Cryptography.X509Certificates.CertificateExtensionsCommon.GetPrivateKey[T](X509Certificate2 certificate, Predicate`1 matchesConstraints)
   at DotnetX509CertificateBug.Program.Main() in C:\build\dotnet-x509-certificate-bug\Program.cs:line 74
dotnet.exe exited on DESKTOP-KGNKFC0 with error code -532462766.
```

# Known Workarounds

Do not read installed certificates with private keys as the local `NT AUTHORITY\SYSTEM` user.

# Configuration

.NET 8.0.1
Microsoft Windows 10.0.22631
