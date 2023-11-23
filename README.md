
# .NET X509 Certificate Bug

A project that reliably reproduces [dotnet/runtime #97121](https://github.com/dotnet/runtime/issues/97121).

To reproduce, run the `.\Reproduce-Bug.ps1` script on a Windows machine that has the .NET SDK installed.

If you have Vagrant and VirtualBox installed, you can run `vagrant up` to reproduce the issue in a fresh Windows 11 VM.

## The Bug

1. Start a process using the local `NT AUTHORITY\SYSTEM` user.

1. Install a certificate and its private key from a PFX to the `LocalMachine` store with the `MachineKeySet |
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
Reproducing dotnet/dotnet #97121

Running as: NT AUTHORITY\SYSTEM
Microsoft Windows 10.0.22631
.NET 8.0.1

Install certificate to LocalMachine/My store with X509KeyStorageFlags: MachineKeySet, PersistKeySet
  Subject: CN=dotnet-x509-certificate-bug
  Subject: 801C2A2802B5E2FCE807AF22DFAEF8DA592C95FE

Check certificate private key:
  Path: C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\bbae62f55a90fd01f60a59b9e474c2e6_318c8c07-5a29-4277-b3d2-81a69ee92153
  Exists: True

Now read certificate again from PFX and then dispose it:
  Done.

Check certificate private key again:
  Path: C:\ProgramData\Microsoft\Crypto\RSA\MachineKeys\bbae62f55a90fd01f60a59b9e474c2e6_318c8c07-5a29-4277-b3d2-81a69ee92153
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
