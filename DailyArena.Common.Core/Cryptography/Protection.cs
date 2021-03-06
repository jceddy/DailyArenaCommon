﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Security.Cryptography;

namespace DailyArena.Common.Core.Cryptography
{
	/// <summary>
	/// Static class with roll-your-own protection methods (to use when we're not running on Windows).
	/// </summary>
	public static class Protection
	{
		/// <summary>
		/// Encrypt salted data using keys linked to the user account.
		/// </summary>
		/// <param name="userData">The data to encrypt.</param>
		/// <param name="optionalEntropy">The salt to apply to the data before encryption.</param>
		/// <returns>The encrypted data.</returns>
		public static byte[] Protect(byte[] userData, byte[] optionalEntropy)
		{
			if(userData == null)
			{
				throw new ArgumentNullException("userData");
			}

			Rijndael aes = Rijndael.Create();
			aes.KeySize = 128;

			byte[] encdata = null;
			using (MemoryStream ms = new MemoryStream())
			{
				ICryptoTransform t = aes.CreateEncryptor();
				using (CryptoStream cs = new CryptoStream(ms, t, CryptoStreamMode.Write))
				{
					cs.Write(userData, 0, userData.Length);
					cs.Close();
					encdata = ms.ToArray();
				}
			}

			byte[] key = null;
			byte[] iv = null;
			byte[] secret = null;
			byte[] header = null;
			SHA256 hash = SHA256.Create();

			try
			{
				key = aes.Key;
				iv = aes.IV;
				secret = new byte[1 + 1 + 16 + 1 + 16 + 1 + 32];

				byte[] digest = hash.ComputeHash(userData);
				if((optionalEntropy != null) && (optionalEntropy.Length > 0))
				{
					byte[] mask = hash.ComputeHash(optionalEntropy);
					for(int i = 0; i < 16; i++)
					{
						key[i] ^= mask[i];
						iv[i] ^= mask[i + 16];
					}
					secret[0] = 2;
				}
				else
				{
					secret[0] = 1;
				}

				secret[1] = 16;
				Buffer.BlockCopy(key, 0, secret, 2, 16);
				secret[18] = 16;
				Buffer.BlockCopy(iv, 0, secret, 19, 16);
				secret[35] = 32;
				Buffer.BlockCopy(digest, 0, secret, 36, 32);

				RSAOAEPKeyExchangeFormatter formatter = new RSAOAEPKeyExchangeFormatter(GetKey());
				header = formatter.CreateKeyExchange(secret);
			}
			finally
			{
				if(key != null)
				{
					Array.Clear(key, 0, key.Length);
					key = null;
				}
				if(secret != null)
				{
					Array.Clear(secret, 0, secret.Length);
					secret = null;
				}
				if(iv != null)
				{
					Array.Clear(iv, 0, iv.Length);
					iv = null;
				}
				aes.Clear();
				hash.Clear();
			}

			byte[] result = new byte[header.Length + encdata.Length];
			Buffer.BlockCopy(header, 0, result, 0, header.Length);
			Buffer.BlockCopy(encdata, 0, result, header.Length, encdata.Length);
			return result;
		}

		/// <summary>
		/// Decrypt salted data using keys linked to the user account.
		/// </summary>
		/// <param name="encryptedData">The data to decrypt.</param>
		/// <param name="optionalEntropy">The salt to apply to the data after decryption.</param>
		/// <returns>The decrypted data.</returns>
		public static byte[] Unprotect(byte[] encryptedData, byte[] optionalEntropy)
		{
			if(encryptedData == null)
			{
				throw new ArgumentNullException("encryptedData");
			}

			byte[] decdata = null;

			Rijndael aes = Rijndael.Create();
			RSA rsa = GetKey();
			int headerSize = rsa.KeySize >> 3;
			bool valid1 = encryptedData.Length >= headerSize;
			if(!valid1)
			{
				headerSize = encryptedData.Length;
			}

			byte[] header = new byte[headerSize];
			Buffer.BlockCopy(encryptedData, 0, header, 0, headerSize);

			byte[] secret = null;
			byte[] key = null;
			byte[] iv = null;
			bool valid2 = false;
			bool valid3 = false;
			bool valid4 = false;
			SHA256 hash = SHA256.Create();

			try
			{
				try
				{
					RSAOAEPKeyExchangeDeformatter deformatter = new RSAOAEPKeyExchangeDeformatter(rsa);
					secret = deformatter.DecryptKeyExchange(header);
					valid2 = secret.Length == 68;
				}
				catch
				{
					valid2 = false;
				}

				if(!valid2)
				{
					secret = new byte[68];
				}

				valid3 = (secret[1] == 16) && (secret[18] == 16) && (secret[35] == 32);

				key = new byte[16];
				Buffer.BlockCopy(secret, 2, key, 0, 16);
				iv = new byte[16];
				Buffer.BlockCopy(secret, 19, iv, 0, 16);

				if((optionalEntropy != null) && (optionalEntropy.Length > 0))
				{
					byte[] mask = hash.ComputeHash(optionalEntropy);
					for(int i = 0; i < 16; i++)
					{
						key[i] ^= mask[i];
						iv[i] ^= mask[i + 16];
					}
					valid3 &= secret[0] == 2;
				}
				else
				{
					valid3 &= secret[0] == 1;
				}

				using (MemoryStream ms = new MemoryStream())
				{
					ICryptoTransform t = aes.CreateDecryptor(key, iv);
					using (CryptoStream cs = new CryptoStream(ms, t, CryptoStreamMode.Write))
					{
						try
						{
							cs.Write(encryptedData, headerSize, encryptedData.Length - headerSize);
							cs.Close();
						}
						catch { }
					}
					decdata = ms.ToArray();
				}

				byte[] digest = hash.ComputeHash(decdata);
				valid4 = true;
				for(int i = 0; i < 32; i++)
				{
					if(digest[i] != secret[36 + i])
					{
						valid4 = false;
					}
				}
			}
			finally
			{
				if(key != null)
				{
					Array.Clear(key, 0, key.Length);
					key = null;
				}
				if(secret != null)
				{
					Array.Clear(secret, 0, secret.Length);
					secret = null;
				}
				if(iv != null)
				{
					Array.Clear(iv, 0, iv.Length);
					iv = null;
				}
				aes.Clear();
				hash.Clear();
			}

			if(!valid1 || !valid2 || !valid3 || !valid4)
			{
				if(decdata != null)
				{
					Array.Clear(decdata, 0, decdata.Length);
					decdata = null;
				}
				throw new CryptographicException("Invalid data.");
			}
			return decdata;
		}

		/// <summary>
		/// The user's key value;
		/// </summary>
		private static RSA _key;

		/// <summary>
		/// Gets the user's key.
		/// </summary>
		/// <returns>The user's key.</returns>
		private static RSA GetKey()
		{
			if(_key == null)
			{
				try
				{
					CspParameters csp = new CspParameters
					{
						KeyContainerName = "DailyArena.Common.Core.Cryptography.Protection"
					};
					_key = new RSACryptoServiceProvider(1536, csp);
				}
				catch(PlatformNotSupportedException)
				{
					// we're not running on Windows, need to do non-platform-specific stuff
					string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".protection");
					if(!Directory.Exists(path))
					{
						Directory.CreateDirectory(path);
					}
					string filename = Path.Combine(path, "DailyArena.Common.Core.Cryptography.Protection");
					if (File.Exists(filename))
					{
						_key = RSA.Create();
						FromJsonString(_key, File.ReadAllText(filename));
					}
					else
					{
						_key = new RSACryptoServiceProvider(1536);
						File.WriteAllText(filename, ToJsonString(_key, true));
					}
				}
			}

			return _key;
		}

		private static void FromJsonString(RSA rsa, string jsonString)
		{
			try
			{
				dynamic paramsJson = JToken.Parse(jsonString);

				RSAParameters parameters = new RSAParameters
				{
					Modulus = paramsJson.Modulus != null ? Convert.FromBase64String((string)paramsJson.Modulus) : null,
					Exponent = paramsJson.Exponent != null ? Convert.FromBase64String((string)paramsJson.Exponent) : null,
					P = paramsJson.P != null ? Convert.FromBase64String((string)paramsJson.P) : null,
					Q = paramsJson.Q != null ? Convert.FromBase64String((string)paramsJson.Q) : null,
					DP = paramsJson.DP != null ? Convert.FromBase64String((string)paramsJson.DP) : null,
					DQ = paramsJson.DQ != null ? Convert.FromBase64String((string)paramsJson.DQ) : null,
					InverseQ = paramsJson.InverseQ != null ? Convert.FromBase64String((string)paramsJson.InverseQ) : null,
					D = paramsJson.D != null ? Convert.FromBase64String((string)paramsJson.D) : null
				};
				rsa.ImportParameters(parameters);
			}
			catch
			{
				throw new Exception("Invalid JSON RSA key.");
			}
		}

		private static string ToJsonString(RSA rsa, bool includePrivateParameters)
		{
			RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

			var parasJson = new
			{
				Modulus = parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null,
				Exponent = parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null,
				P = parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
				Q = parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
				DP = parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null,
				DQ = parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null,
				InverseQ = parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null,
				D = parameters.D != null ? Convert.ToBase64String(parameters.D) : null
			};

			return JsonConvert.SerializeObject(parasJson);
		}
	}
}
