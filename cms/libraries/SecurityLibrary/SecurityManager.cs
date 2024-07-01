using System.Collections.Specialized;
using System.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using System.Xml;
using System.IO;


namespace SecurityLibrary
{
    public static class SecurityManager
    {
        private static RsaCryptoServiceProvider GetCertificate(string? thumbPrint)
        {
                var userStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                userStore.Open(OpenFlags.OpenExistingOnly);
                foreach (var c in userStore.Certificates)
                {
                    if (c.Thumbprint != thumbPrint) continue;
                    return RsaCryptoServiceProvider.CreateInstance(c);
                }

                return null!;
        }
        private static string? GetCertificateThumb(Configuration cfg)
        {
            return cfg.AppSettings.Settings["X509CertificateThumb"].Value;
        }
        private static Configuration GetConfiguration()
        {
            var fileMap = new ExeConfigurationFileMap
            {
                ExeConfigFilename = Environment.CurrentDirectory + @"\SecurityLibrary.dll.config"
            };
            var cfg = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            return cfg;
        }
        public static string CertificateThumbValue()
        {
            var cfg = GetConfiguration();
            foreach (string key in cfg.AppSettings.Settings.AllKeys)
            {
                if (key.StartsWith("X509"))
                {
                    return cfg.AppSettings.Settings[key].Value;
                }

            }

            return "Certificate not found";
        }
            public static string[] GetAllKeys()
            {
                var cfg = GetConfiguration();
                List<string> values = new List<string>();
                foreach (string key in cfg.AppSettings.Settings.AllKeys)
                {
                    if (!key.StartsWith("X509"))
                    {
                        values.Add(key);
                    }

                }

                string[] repositoryUrls = values.ToArray();

            return repositoryUrls;
        }
        public static string GetCredential(string KeyName)
        {
            var cfg = GetConfiguration();
            foreach (string key in cfg.AppSettings.Settings.AllKeys)
            {
                if (key.Equals(KeyName))
                {
                    var rsaCryptoServiceProvider = GetCertificate(GetCertificateThumb(cfg));
                    var keyValue = cfg.AppSettings.Settings[KeyName].Value;
                    return rsaCryptoServiceProvider.Decrypt(keyValue);
                }

            }

            return "No Key Value Found";
        }
        public static bool DeleteKey(string keyName)
        {
            var cfg = GetConfiguration();
            cfg.AppSettings.Settings.Remove(keyName);
            cfg.Save(ConfigurationSaveMode.Full);
            ConfigurationManager.RefreshSection("appSettings");
            System.IO.File.Copy(Environment.CurrentDirectory + @"\SecurityLibrary.dll.config", Environment.CurrentDirectory + @"\app.config", true);
            return true;
        }
        public static bool AddKey(string keyName, string keyValue)
        {
            var cfg = GetConfiguration();
            cfg.AppSettings.Settings.Remove(keyName);
            cfg.AppSettings.Settings.Add(keyName, keyValue);
            cfg.Save(ConfigurationSaveMode.Full);
            ConfigurationManager.RefreshSection("appSettings");
            System.IO.File.Copy(Environment.CurrentDirectory + @"\SecurityLibrary.dll.config", Environment.CurrentDirectory + @"\app.config", true);
            return true;
        }
        public static string GetEncryption(string PhraseName)
        {
            var cfg = GetConfiguration();
            var rsaCryptoServiceProvider = GetCertificate(GetCertificateThumb(cfg));
            return rsaCryptoServiceProvider.Encrypt(PhraseName);
        }

    }

    internal class RsaCryptoServiceProvider
    {
        private readonly X509Certificate2 _certificate;
        private RSA? _rsa;
        private RSAParameters _rsaParameters;

        public static RsaCryptoServiceProvider CreateInstance(X509Certificate2 theCertificate)
        {
            return new RsaCryptoServiceProvider(theCertificate);
        }
        private RsaCryptoServiceProvider(X509Certificate2 theCertificate)
        {
            _certificate = theCertificate;
            InitializeCryptoProvider();
        }
        public bool InitializeCryptoProvider()
        {
            try
            {
                //if (_certificate.HasPrivateKey)
                //{
                //    _rsa = _certificate.PrivateKey as RSACryptoServiceProvider;
                //    return true;
                //}
                if (_certificate.HasPrivateKey)
                {
                    //_rsa = _certificate.GetRSAPrivateKey();
                    //_rsa = _certificate.PrivateKey as RSACryptoServiceProvider;

                    RSACng rsa = (RSACng)_certificate.GetRSAPrivateKey();
#pragma warning disable CA1416 // Validate platform compatibility
                    rsa?.Key.SetProperty(new CngProperty("Export Policy", BitConverter.GetBytes((int)CngExportPolicies.AllowPlaintextExport), CngPropertyOptions.Persist));
#pragma warning restore CA1416 // Validate platform compatibility

#pragma warning disable CA1416 // Validate platform compatibility
                    if (rsa != null) _rsaParameters = rsa.ExportParameters(true);
#pragma warning restore CA1416 // Validate platform compatibility

                    return true;
                }

            }
            catch (Exception e)
            {

            }
            return false;
        }
        public byte[] Encrypt(byte[] plainBytes)
        {
            byte[] b;
            using RSACryptoServiceProvider rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            //var p = _rsa.ExportParameters(true);
            rsaCryptoServiceProvider.ImportParameters(_rsaParameters);
            b = rsaCryptoServiceProvider.Encrypt(plainBytes, true);
            // b = _rsa.Encrypt(plainBytes,RSAEncryptionPadding.OaepSHA256);
            return b;
        }
        public string Encrypt(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var b = Encrypt(plainBytes);
            var cipherText = Convert.ToBase64String(b);

            return cipherText;
        }
        public byte[] EncryptToBytes(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var b = Encrypt(plainBytes);
            return b;
        }
        public char[] EncryptToChars(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var b = Encrypt(plainBytes);
            return Encoding.Unicode.GetChars(b);

        }
        public byte[] Decrypt(byte[] cipherBytes)
        {
            byte[] b;

            using RSACryptoServiceProvider rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            //var p = _rsa.ExportParameters(true);
            rsaCryptoServiceProvider.ImportParameters(_rsaParameters);
            b = rsaCryptoServiceProvider.Decrypt(cipherBytes, true);
            return b;
        }
        public string Decrypt(string? cipherText)
        {
            var cipherBytes = Convert.FromBase64String(cipherText);
            var b = Decrypt(cipherBytes);
            var plainText = Encoding.UTF8.GetString(b);
            return plainText;
        }
        public string DecryptFromChars(string cipherText)
        {
            var cipherBytes = Encoding.Unicode.GetBytes(cipherText);
            var b = Decrypt(cipherBytes);
            var plainText = Encoding.UTF8.GetString(b);
            return plainText;
        }
        public byte[] SignMessage(byte[] plainBytes)
        {
            byte[] b;
            // Please note that this is using SHA1, as the key that I'm using does not support 
            // SHA256. If you have a key that does, you can switch the provider to SHA256Managed.
            // See:  http://hintdesk.com/c-how-to-fix-invalid-algorithm-specified-when-signing-with-sha256/
            var hashAlg = new SHA1Managed();
            if (_certificate.HasPrivateKey)
            {
                _certificate.GetRSAPrivateKey();
                b = _rsa.SignData(plainBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return b;
            }
            return null;
        }
        public string SignMessage(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var b = SignMessage(plainBytes);
            var signature = Convert.ToBase64String(b);
            return signature;
        }
    }
}