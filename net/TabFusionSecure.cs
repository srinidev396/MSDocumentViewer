using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace DocumentService.Security
{
    public class TabFusionSecure
    {
        private const string EncryptionKey = "MAKV2SPBNI99212";
        public static string DecryptURLParameters(string cipherText)
        {
            try
            {
                if (!cipherText.StartsWith(Conversions.ToString("á")))
                    return cipherText;
                cipherText = DecryptURLParameters(cipherText.Substring(1)).Replace(" ", "+");
                var cipherBytes = Convert.FromBase64String(cipherText);

                using (var encryptor = new AesCryptoServiceProvider())
                {
                    using (var pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6E, 0x20, 0x4D, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }))
                    {
                        encryptor.Key = pdb.GetBytes(32);
                        encryptor.IV = pdb.GetBytes(16);
                        using (var ms = new MemoryStream())
                        {
                            using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                            {
                                cs.Write(cipherBytes, 0, cipherBytes.Length);
                                cs.FlushFinalBlock();
                            }
                            cipherText = Encoding.Unicode.GetString(ms.ToArray());
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Return "whatever"
            }

            return cipherText;
        }
    }
}