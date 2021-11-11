using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JimmikerNetwork
{
    public static class EncryptAndCompress
    {
        #region 加密

        #region AES
        /// <summary>
        /// Generate AES Key
        /// </summary>
        static public string GenerateAESKey()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        /// <summary>
        /// AES Encrypt
        /// </summary>
        /// <param name="inputByteArray">plaintext Binary</param>
        /// <param name="IV">Initialization Vector</param>
        /// <param name="strKey">key</param>
        /// <returns>return ciphertext Binary</returns>
        public static byte[] AESEncrypt(byte[] inputByteArray, byte[] IV, string strKey)
        {
            //分組加密演算法
            SymmetricAlgorithm des = Rijndael.Create();
            //設定金鑰及金鑰向量
            des.Key = Encoding.UTF8.GetBytes(strKey);
            des.IV = IV;
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            byte[] cipherBytes = ms.ToArray();//得到加密後的位元組陣列
            cs.Close();
            ms.Close();
            return cipherBytes;
        }

        /// <summary>
        /// AES Decrypt
        /// </summary>
        /// <param name="cipherText">ciphertext Binary</param>
        /// <param name="IV">Initialization Vector</param>
        /// <param name="strKey">key</param>
        /// <returns>return plaintext Binary</returns>
        public static byte[] AESDecrypt(byte[] cipherText, byte[] IV, string strKey)
        {
            SymmetricAlgorithm des = Rijndael.Create();
            des.Key = Encoding.UTF8.GetBytes(strKey);
            des.IV = IV;
            byte[] decryptBytes = new byte[cipherText.Length];
            MemoryStream ms = new MemoryStream(cipherText);
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read);
            cs.Read(decryptBytes, 0, decryptBytes.Length);
            cs.Close();
            ms.Close();
            return decryptBytes;
        }
        #endregion

        #region RSA
        /// <summary>
        /// A struct for RSA Key Pair
        /// </summary>
        public struct RSAKeyPair
        {
            public string PrivateKey { get; private set; }
            public string PublicKey { get; private set; }

            public byte[] PrivateKeyBytes
            {
                get
                {
                    return Convert.FromBase64String(PrivateKey);
                }
            }
            public byte[] PublicKeyBytes
            {
                get
                {
                    return Convert.FromBase64String(PublicKey);
                }
            }

            public RSAKeyPair(string PrivateKey, string PublicKey)
            {
                this.PrivateKey = PrivateKey;
                this.PublicKey = PublicKey;
            }

            public RSAKeyPair(string PublicKey)
            {
                this.PrivateKey = "";
                this.PublicKey = PublicKey;
            }

            public RSAKeyPair(byte[] PrivateKey, byte[] PublicKey)
            {
                this.PrivateKey = Convert.ToBase64String(PrivateKey);
                this.PublicKey = Convert.ToBase64String(PublicKey);
            }

            public RSAKeyPair(byte[] PublicKey)
            {
                this.PrivateKey = "";
                this.PublicKey = Convert.ToBase64String(PublicKey);
            }
        }

        static public int GetRSAKeySize(string Key)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(Key));
            return rsa.KeySize;
        }

        /// <summary>
        /// Generate RSA Key
        /// </summary>
        static public RSAKeyPair GenerateRSAKeys(int size)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(size);
            return new RSAKeyPair(rsa.ExportCspBlob(true), rsa.ExportCspBlob(false));
        }

        /// <summary>
        /// RSA Encrypt
        /// </summary>
        /// <param name="publicKey">public key</param>
        /// <param name="IV">Initialization Vector</param>
        /// <param name="content">plaintext Binary</param>
        /// <returns>return ciphertext Binary</returns>
        static public byte[] RSAEncrypt(string publicKey, byte[] IV, byte[] content)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(publicKey));

            int buffersize = (rsa.KeySize / 8) - 11;

            byte[] newIV = new byte[buffersize];
            Array.Copy(IV, 0, newIV, 0, Math.Min(buffersize, IV.Length));

            for (int i = IV.Length; i < buffersize; i++)
            {
                int now = (newIV[i - IV.Length] + newIV[i - IV.Length + 1]) % 256;
                newIV[i] = (byte)now;
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                for (int i = 0; i < content.Length; i += buffersize)
                {
                    int copyLength = Math.Min(content.Length - i, buffersize);
                    byte[] buffer = new byte[copyLength];
                    Array.Copy(content, i, buffer, 0, copyLength);

                    BitArray bitArray = new BitArray(buffer);
                    byte[] nowIV = new byte[copyLength];
                    Array.Copy(newIV, 0, nowIV, 0, copyLength);
                    BitArray bitIV = new BitArray(nowIV);
                    byte[] newbuffer = new byte[copyLength];
                    bitArray.Xor(bitIV).CopyTo(newbuffer, 0);

                    byte[] encryptdata = rsa.Encrypt(newbuffer, false);
                    writer.Write(encryptdata);

                    Array.Copy(encryptdata, 0, newIV, 0, newIV.Length);
                }
                writer.Close();
                stream.Close();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// RSA Decrypt
        /// </summary>
        /// <param name="privateKey">private Key</param>
        /// <param name="IV">Initialization Vector</param>
        /// <param name="encryptedContent">ciphertext Binary</param>
        /// <returns>return plaintext Binary</returns>
        static public byte[] RSADecrypt(string privateKey, byte[] IV, byte[] encryptedContent)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(privateKey));

            int buffersize = rsa.KeySize / 8;
            int Decryptsize = (rsa.KeySize / 8) - 11;

            byte[] newIV = new byte[Decryptsize];
            Array.Copy(IV, 0, newIV, 0, Math.Min(Decryptsize, IV.Length));

            for (int i = IV.Length; i < Decryptsize; i++)
            {
                int now = (newIV[i - IV.Length] + newIV[i - IV.Length + 1]) % 256;
                newIV[i] = (byte)now;
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                for (int i = 0; i < encryptedContent.Length; i += buffersize)
                {
                    int copyLength = Math.Min(encryptedContent.Length - i, buffersize);
                    byte[] buffer = new byte[copyLength];
                    Array.Copy(encryptedContent, i, buffer, 0, copyLength);

                    byte[] decryptString = rsa.Decrypt(buffer, false);

                    BitArray bitArray = new BitArray(decryptString);
                    byte[] nowIV = new byte[decryptString.Length];
                    Array.Copy(newIV, 0, nowIV, 0, decryptString.Length);
                    BitArray bitIV = new BitArray(nowIV);
                    byte[] newbuffer = new byte[decryptString.Length];
                    bitArray.Xor(bitIV).CopyTo(newbuffer, 0);

                    writer.Write(newbuffer);

                    Array.Copy(buffer, 0, newIV, 0, newIV.Length);
                }
                writer.Close();
                stream.Close();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// RSA Signature 
        /// </summary>
        /// <param name="privateKey">private key</param>
        /// <param name="content">plaintext Binary</param>
        /// <param name="halg">Hash Algorithm</param>
        /// <returns>return Signature Binary</returns>
        static public byte[] RSASignData(string privateKey, byte[] content, HashAlgorithm halg)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(privateKey));

            var encryptString = rsa.SignData(content, halg);

            return encryptString;
        }

        /// <summary>
        /// RSA Verify
        /// </summary>
        /// <param name="publicKey">private key</param>
        /// <param name="content">plaintext Binary</param>
        /// <param name="signature">signature Binary</param>
        /// <param name="halg">Hash Algorithm</param>
        /// <returns>is verify</returns>
        static public bool RSAVerifyData(string publicKey, byte[] content, byte[] signature, HashAlgorithm halg)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(publicKey));

            var Verify = rsa.VerifyData(content, halg, signature);

            return Verify;
        }

        #endregion

        public enum LockType
        {
            None,
            AES,
            RSA,
        }

        /// <summary>
        /// Encrypt Data with specified encryption algorithm
        /// </summary>
        /// <param name="bs">plaintext Binary</param>
        /// <param name="key">key</param>
        /// <param name="_Lock">encryption algorithm</param>
        /// <returns>return ciphertext Binary</returns>
        static public byte[] Lock(byte[] bs, string key, LockType _Lock)
        {
            byte[] encryptBytes = null;

            byte[] setdata(LockType Lock, byte[] IV, byte[] data)
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write((byte)Lock);
                    if (IV != null)
                    {
                        writer.Write(IV, 0, 16);
                    }
                    writer.Write(data);

                    writer.Close();
                    stream.Close();

                    return stream.ToArray();
                }
            }

            if (!string.IsNullOrEmpty(key))
            {
                switch (_Lock)
                {
                    case LockType.None:
                        {
                            encryptBytes = setdata(_Lock, null, bs);
                            break;
                        }
                    case LockType.AES:
                        {
                            byte[] IV = new byte[16];
                            new Random(Guid.NewGuid().GetHashCode()).NextBytes(IV);
                            encryptBytes = setdata(_Lock, IV, AESEncrypt(bs, IV, key));
                            break;
                        }
                    case LockType.RSA:
                        {
                            byte[] IV = new byte[16];
                            new Random(Guid.NewGuid().GetHashCode()).NextBytes(IV);
                            encryptBytes = setdata(_Lock, IV, RSAEncrypt(key, IV, bs));
                            break;
                        }
                }
            }
            else
            {
                encryptBytes = setdata(LockType.None, null, bs);
            }
            return Compress(encryptBytes);
        }

        /// <summary>
        /// Decrypt Data
        /// </summary>
        /// <param name="bs">ciphertext Binary</param>
        /// <param name="key">key</param>
        /// <returns>return plaintext Binary</returns>
        static public byte[] UnLock(byte[] bs, string key)
        {
            const int TypeLen = 1;
            const int IVLen = 16;

            byte[] _out = null;
            using (MemoryStream stream = new MemoryStream(bs))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                LockType _Lock = (LockType)reader.ReadByte();
                switch (_Lock)
                {
                    case LockType.None:
                        {
                            _out = reader.ReadBytes(bs.Length - TypeLen);
                            break;
                        }
                    case LockType.AES:
                        {
                            byte[] IV = reader.ReadBytes(IVLen);
                            byte[] data = reader.ReadBytes(bs.Length - TypeLen - IVLen);
                            _out = AESDecrypt(data, IV, key);
                            break;
                        }
                    case LockType.RSA:
                        {
                            byte[] IV = reader.ReadBytes(IVLen);
                            byte[] data = reader.ReadBytes(bs.Length - TypeLen - IVLen);
                            _out = RSADecrypt(key, IV, data);
                            break;
                        }
                }
                reader.Close();
                stream.Close();
                return _out;
            }
        }

        static public byte[] SignData(string privateKey, string AESKey, byte[] content)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                byte[] data = Lock(content, AESKey, LockType.AES);
                writer.Write(data.Length);
                writer.Write(data);
                writer.Write(RSASignData(privateKey, content, new SHA256CryptoServiceProvider()));
                writer.Close();
                stream.Close();
                return stream.ToArray();
            }
        }

        static public bool VerifyData(string publicKey, string AESKey, byte[] signature)
        {
            using (MemoryStream stream = new MemoryStream(signature))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int datalen = reader.ReadInt32();
                byte[] data = UnLock(reader.ReadBytes(datalen), AESKey);
                byte[] sign = reader.ReadBytes(signature.Length - datalen);
                return RSAVerifyData(publicKey, data, sign, new SHA256CryptoServiceProvider());
            }
        }

        #endregion

        #region 壓縮
        /// <summary>
        /// Compress Binary
        /// </summary>
        /// <param name="_bytes">Binary after compress</param>
        /// <param name="bytes">Binary need to compress</param>
        static public byte[] Compress(byte[] bytes)
        {
            int byteLength = bytes.Length;
            byte[] bytes2;
            using (MemoryStream stream = new MemoryStream())
            {
                using (GZipStream compressionStream = new GZipStream(stream, CompressionMode.Compress))
                {
                    compressionStream.Write(bytes, 0, bytes.Length);
                }
                stream.Close();

                bytes2 = stream.ToArray();
                stream.Dispose();
            }

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(byteLength > bytes2.Length);

                writer.Write(byteLength);
                if (byteLength > bytes2.Length)
                {
                    writer.Write(bytes2.Length);
                    writer.Write(bytes2);
                }
                else
                {
                    writer.Write(bytes);
                }
                writer.Close();
                stream.Close();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Decompress Binary
        /// </summary>
        /// <param name="_bytes">Binary need to decompress</param>
        /// <param name="index">start index</param>
        /// <param name="str">Binary after decompress</param>
        /// <param name="length">Binary length</param>
        static public byte[] Decompress(byte[] _bytes, int index, out int length)
        {
            bool compress;
            int q;
            byte[] bs;

            using (MemoryStream stream = new MemoryStream(_bytes))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                reader.ReadBytes(index);
                compress = reader.ReadBoolean();
                q = reader.ReadInt32();
                bs = reader.ReadBytes(compress ? reader.ReadInt32() : q);

                reader.Close();
                reader.Dispose();
                stream.Close();
                stream.Dispose();
            }

            byte[] str;

            if (compress)
            {
                using (MemoryStream stream = new MemoryStream(bs))
                {
                    str = new byte[q];

                    using (GZipStream decompressionStream = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        decompressionStream.Read(str, 0, q);
                    }
                }
                length = bs.Length + 9;
            }
            else
            {
                str = bs;
                length = bs.Length + 5;
            }
            return str;
        }
        #endregion
    }
}
