using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace DotnetX509CertificateBug
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine();

            Console.WriteLine($"Running as: {WindowsIdentity.GetCurrent().Name}");
            Console.WriteLine($"{RuntimeInformation.OSDescription}");
            Console.WriteLine($"{RuntimeInformation.FrameworkDescription}");
            Console.WriteLine();

            string pfxPath = Path.Join(Directory.GetCurrentDirectory(), "dotnet-x509-certificate-bug.pfx");
            string password = "hunter2";

            var pfxBytes = File.ReadAllBytes(pfxPath);
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