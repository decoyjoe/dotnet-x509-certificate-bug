
# .NET X509 Certificate Bug

A project that reliably reproduces .NET bug [#0000]()...

To reproduce, run the `.\Reproduce-Bug.ps1` script on a Windows machine that has the .NET SDK installed.

## The Bug

1. Start a process using the local `NT AUTHORITY\SYSTEM` user.

1. Install a certificate and its private key from a PFX to the LocalMachine store with the `MachineKeySet |
   PersistKeySet` key storage flags. The private key is persistently stored on disk at
   `C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\`.

1. Once the certificate is installed with the persisted private key, re-read the certificate from the PFX file into
   memory as an `X509Certificate2` object and then call the `.Dispose()` method on it.

1. Disposing of the certificate, which was initially read from the PFX file into memory, results in .NET deleting the
   private key file from disk (located in the `MachineKeys` directory). Attempts to read the private key, even in new
   processes, will result in `Keyset does not exist` errors. The certificate and private key must be re-installed.

## Example

Running `Reproduce-Bug.ps1` script will result in the following output:

```console
Running as: NT AUTHORITY\SYSTEM
Microsoft Windows 10.0.19045
.NET 8.0.0

Install certificate to LocalMachine/My store with X509KeyStorageFlags: MachineKeySet, PersistKeySet
  Subject: CN=dotnet-x509-certificate-bug
  Subject: 74B83BCA87D78A964F019B3FF41F2F37CB77395A

Check certificate private key:
  Path: C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\d43273855d65ba52669550a30c068619_185ec405-45c2-4d00-ae59-ef3ce14bc897
  Exists: True

Now read certificate again from PFX and then dispose it:
  Done.

Check certificate private key again:
  Path: C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\d43273855d65ba52669550a30c068619_185ec405-45c2-4d00-ae59-ef3ce14bc897
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
   at DotnetX509CertificateBug.Program.Main() in C:\build\dotnet-x509-certificate-bug\Program.cs:line 65
dotnet.exe exited on test-machine with error code -532462766.
```
