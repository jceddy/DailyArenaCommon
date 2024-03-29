﻿using DailyArena.Common.Core.Cryptography;
using DailyArena.Common.Core.Utility;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DailyArena.Common.Core.Database
{
	/// <summary>
	/// Class that imports and parses the card database, and keeps track of server timestamps for cached card/set/deck data.
	/// </summary>
	public static class CardDatabase
	{
		/// <summary>
		/// Dictionary to keep track of server-side cache timestamps.
		/// </summary>
		private static Dictionary<string, string> _serverTimestamps = new Dictionary<string, string>();

		/// <summary>
		/// Language mappings for card names and images for non-English languages.
		/// </summary>
		private static Dictionary<string, Tuple<string, string>> _languageMappings = new Dictionary<string, Tuple<string, string>>();

		/// <summary>
		/// Flag that determines whether cached language mapping data is stale.
		/// </summary>
		private static bool _downloadLanguageMappings = false;

		/// <summary>
		/// Get a server-side cache timestamp by key.
		/// </summary>
		/// <param name="key">The key of the server-side cache timestamp to fetch.</param>
		/// <returns>The cache timestamp as an ISO 8601 string.</returns>
		public static string GetServerTimestamp(string key)
		{
			return _serverTimestamps.ContainsKey(key) ? _serverTimestamps[key] : null;
		}

		/// <summary>
		/// Timestamp of the last client-side card database update.
		/// </summary>
		public static string LastCardDatabaseUpdate { get; private set; }

		/// <summary>
		/// Timestamps of the last client-side standard sets update.
		/// </summary>
		public static string LastStandardSetsUpdate { get; private set; }

		/// <summary>
		/// Gets or sets a boolean that determines whether data files generated by the database are protected.
		/// </summary>
		public static bool UseProtectedData { get; private set; }

		/// <summary>
		/// The user's two-character culture code.
		/// </summary>
		public static string CurrentCulture { get; private set; }

		public static Tuple<string, string> GetMappedLanguageData(Card card)
		{
			Regex pattern = new Regex("[^A-Za-z0-9]");
			var replacedName = pattern.Replace(card.Name, "");
			string mappingKey = $"{replacedName}_{pattern.Replace(card.Set.Name, "")}_{card.CollectorNumber}";

			if (_languageMappings.ContainsKey(mappingKey))
			{
				return _languageMappings[mappingKey];
			}
			else
			{
				return _languageMappings.Where(x => x.Key.StartsWith($"{replacedName}_")).Select(x => x.Value).FirstOrDefault();
			}
		}

		/// <summary>
		/// Initialize the card database. Resets all timestamps, and reloads all card/set data from the server.
		/// </summary>
		/// <param name="useProtectedData">Whether data filed generated by the database are protected.</param>
		public static void Initialize(bool useProtectedData)
		{
			UseProtectedData = useProtectedData;
			LastCardDatabaseUpdate = "1970-01-01T00:00:00Z";
			LastStandardSetsUpdate = "1970-01-01T00:00:00Z";
			Card.ClearCards();
			Set.ClearSets();
			LoadCardDatabase();
			UpdateCardDatabase();

			CurrentCulture = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
			if (CurrentCulture != "en")
			{
				LoadLanguageMappings();
				UpdateLanguageMappings();

				foreach (Card card in Card.AllCards)
				{
					card.UpdateLanguageMappings();
				}
			}
		}

		/// <summary>
		/// Load language mappings for the current culture.
		/// </summary>
		private static void LoadLanguageMappings()
		{
			string json = null;
			if (UseProtectedData)
			{
				if (File.Exists($"language_mappings_{CurrentCulture}.dat"))
				{
					var userName = Environment.UserName;
					var salt = HashAString(userName);

					byte[] protectedBytes = File.ReadAllBytes($"language_mappings_{CurrentCulture}.dat");
					byte[] bytes = Unprotect(protectedBytes, salt);
					json = Encoding.ASCII.GetString(bytes);
				}
			}
			else
			{
				if (File.Exists($"language_mappings_{CurrentCulture}.json"))
				{
					json = File.ReadAllText($"language_mappings_{CurrentCulture}.json");
				}
			}

			if (!string.IsNullOrWhiteSpace(json))
			{
				dynamic data = JsonConvert.DeserializeObject(json);

				_languageMappings = data.ToObject<Dictionary<string, Tuple<string, string>>>();
			}
		}

		/// <summary>
		/// Re-load language mappings from the server if the cached data is stale.
		/// </summary>
		private static void UpdateLanguageMappings()
		{
			var ver = Guid.NewGuid();
			var languageMappingJsonUrl = $"https://clans.dailyarena.net/language_mappings_{CurrentCulture}.json?_c={ver}";

			if (_downloadLanguageMappings || _languageMappings.Count == 0)
			{
				bool saveLanguageMappings = true;

				try
				{
					var result = WebUtilities.FetchStringFromUrl(languageMappingJsonUrl);
					dynamic data = JToken.Parse(result);
					_languageMappings = new Dictionary<string, Tuple<string, string>>();

					foreach (dynamic mapping in data)
					{
						_languageMappings[(string)mapping.Name] = new Tuple<string, string>((string)mapping.Value["printed_name"], (string)mapping.Value["scryfall_id"]);
					}
				}
				catch (WebException)
				{
					saveLanguageMappings = false;
				}

				if (saveLanguageMappings)
				{
					SaveLanguageMappings();
				}
			}
		}

		/// <summary>
		/// Save the language mappings to the client-side cache.
		/// </summary>
		public static void SaveLanguageMappings()
		{
			var userName = Environment.UserName;
			var salt = HashAString(userName);

			string json = JsonConvert.SerializeObject(_languageMappings);

			if (UseProtectedData)
			{
				byte[] bytes = Encoding.ASCII.GetBytes(json);
				byte[] protectedBytes = Protect(bytes, salt);
				File.WriteAllBytes($"language_mappings_{CurrentCulture}.dat", protectedBytes);
			}
			else
			{
				File.WriteAllText($"language_mappings_{CurrentCulture}.json", json);
			}
		}

		/// <summary>
		/// Load the card database from the client-side cache.
		/// </summary>
		private static void LoadCardDatabase()
		{
			string json = null;
			if (UseProtectedData)
			{
				if (File.Exists("database.dat"))
				{
					var userName = Environment.UserName;
					var salt = HashAString(userName);

					byte[] protectedBytes = File.ReadAllBytes("database.dat");
					byte[] bytes = Unprotect(protectedBytes, salt);
					json = Encoding.ASCII.GetString(bytes);
				}
			}
			else
			{
				if (File.Exists("database.json"))
				{
					json = File.ReadAllText("database.json");
				}
			}

			if (!string.IsNullOrWhiteSpace(json))
			{
				dynamic data = JsonConvert.DeserializeObject(json);

				try { LastCardDatabaseUpdate = data.LastCardDatabaseUpdate; } catch (RuntimeBinderException) { LastCardDatabaseUpdate = "1970-01-01T00:00:00Z"; }
				try { LastStandardSetsUpdate = data.LastStandardSetsUpdate; } catch (RuntimeBinderException) { LastStandardSetsUpdate = "1970-01-01T00:00:00Z"; }
				DateTime twoThousand = new DateTime(2000, 01, 01);
				foreach (dynamic set in data.Sets)
				{
					Set.CreateSet((string)set.Name, (string)set.Code, (string)set.ArenaCode, set.NotInBooster.ToObject<List<string>>(), (int)set.TotalCards,
						set.RarityCounts.ToObject<Dictionary<CardRarity, int>>(), (DateTime)(set.RotationEnter ?? twoThousand), (DateTime)(set.RotationExit ?? DateTime.MaxValue),
						set.ExtendedCardInfo == null ? new Dictionary<int, Set.CardInfo>() : set.ExtendedCardInfo.ToObject<Dictionary<int, Set.CardInfo>>());
				}
				foreach (dynamic card in data.Cards)
				{
					string type = (string)card.Type;
					if (!string.IsNullOrWhiteSpace(type))
					{
						Card.CreateCard((int)card.ArenaId, (string)card.Name, (string)card.Set, (string)card.CollectorNumber, (string)card.Rarity,
							(string)card.Colors, (int)card.Rank, type, (string)card.Cost, (int)card.Cmc, (string)card.ScryfallId);
					}
				}
			}
		}

		/// <summary>
		/// Save the card database to the client-side cache.
		/// </summary>
		private static void SaveCardDatabase()
		{
			var data = new
			{
				LastCardDatabaseUpdate,
				LastStandardSetsUpdate,
				Sets = Set.AllSets.Select(x => new
				{
					x.Name,
					x.Code,
					x.ArenaCode,
					x.NotInBooster,
					x.TotalCards,
					x.RarityCounts,
					x.RotationEnter,
					x.RotationExit,
					x.ExtendedCardInfo
				}),
				Cards = Card.AllCards.Select(x => new
				{
					x.ArenaId,
					x.CollectorNumber,
					Colors = x.Colors.ColorString,
					x.Name,
					Rarity = x.Rarity.Name,
					Set = x.Set.Name,
					x.Rank,
					x.Type,
					x.Cost,
					x.Cmc,
					x.ScryfallId
				})
			};

			var userName = Environment.UserName;
			var salt = HashAString(userName);

			string json = JsonConvert.SerializeObject(data);

			if (UseProtectedData)
			{
				byte[] bytes = Encoding.ASCII.GetBytes(json);
				byte[] protectedBytes = Protect(bytes, salt);
				File.WriteAllBytes("database.dat", protectedBytes);
			}
			else
			{
				File.WriteAllText("database.json", json);
			}
		}

		/// <summary>
		/// Check if the client-side card database cache is out of date. If so, re-load it from the server.
		/// </summary>
		private static void UpdateCardDatabase()
		{
			var ver = Guid.NewGuid();
			var timestampJsonUrl = $"https://clans.dailyarena.net/update_timestamps.json?_c={ver}";
			var cardDatabaseUrl = $"https://clans.dailyarena.net/card_database.json?_c={ver}";
			var standardSetsUrl = $"https://clans.dailyarena.net/standard_sets_info.json?_c={ver}";

			var downloadData = false;

			try
			{
				string result = WebUtilities.FetchStringFromUrl(timestampJsonUrl);
				using (StringReader resultStringReader = new StringReader(result))
				using (JsonTextReader resultJsonReader = new JsonTextReader(resultStringReader) { DateParseHandling = DateParseHandling.None })
				{
					dynamic json = JToken.ReadFrom(resultJsonReader);
					foreach (var timestamp in json)
					{
						_serverTimestamps[(string)timestamp.Name] = (string)timestamp.Value;
					}

					if ((string.Compare(_serverTimestamps["CardDatabase"], LastCardDatabaseUpdate) > 0) ||
						(string.Compare(_serverTimestamps["StandardSets"], LastStandardSetsUpdate) > 0))
					{
						downloadData = true;
						_downloadLanguageMappings = true;
					}
				}
			}
			catch (WebException)
			{
				// if we got a webexception here, set some fake server timestamp values to keep the application from downloading more data
				_serverTimestamps["CardDatabase"] = "1970-01-01T00:00:00Z";
				_serverTimestamps["StandardSets"] = "1970-01-01T00:00:00Z";
				_serverTimestamps["StandardLands"] = "1970-01-01T00:00:00Z";
				_serverTimestamps["StandardDecks"] = "1970-01-01T00:00:00Z";
				_serverTimestamps["BrawlDecks"] = "1970-01-01T00:00:00Z";
				_serverTimestamps["ArenaStandardDecks"] = "1970-01-01T00:00:00Z";
				_serverTimestamps["SetTranslations"] = "1970-01-01T00:00:00Z";
				_serverTimestamps["LanguageMappings"] = "1970-01-01T00:00:00Z";
				_serverTimestamps["HistoricDecks"] = "1970-01-01T00:00:00Z";
			}

			if (downloadData)
			{
				// Key => Name
				// Value =>
				//   Item1 => NotInBooster
				//   Item2 => RarityCounts
				//   Item3 => TotalCards
				//   Item4 => RotationEnter
				//   Item5 => RotationExit
				//   Item4 => ExtendedCardInfo
				Dictionary<string, Tuple<List<string>, Dictionary<CardRarity, int>, int, DateTime, DateTime, Dictionary<int, Set.CardInfo>>> standardSetsInfo =
					new Dictionary<string, Tuple<List<string>, Dictionary<CardRarity, int>, int, DateTime, DateTime, Dictionary<int, Set.CardInfo>>>();

				LastCardDatabaseUpdate = _serverTimestamps["CardDatabase"];
				LastStandardSetsUpdate = _serverTimestamps["StandardSets"];
				bool saveCardDatabase = true;
				DateTime twoThousand = new DateTime(2000, 01, 01);

				try
				{
					var result = WebUtilities.FetchStringFromUrl(standardSetsUrl);
					dynamic data = JToken.Parse(result);

					foreach (dynamic set in data)
					{
						Dictionary<int, Set.CardInfo> extendedCardInfo = new Dictionary<int, Set.CardInfo>();
						foreach (dynamic cardInfo in set.Value["extended_card_info"])
						{
							extendedCardInfo[int.Parse(cardInfo.Name)] = new Set.CardInfo((string)cardInfo.Value["color_identity"]);
						}

						standardSetsInfo[(string)set.Value["name"]] = new Tuple<List<string>, Dictionary<CardRarity, int>, int, DateTime, DateTime, Dictionary<int, Set.CardInfo>>(
							set.Value["not_in_booster"].ToObject<List<string>>(),
							set.Value["rarity_counts"].ToObject<Dictionary<CardRarity, int>>(),
							(int)set.Value["total_cards"],
							set.Value["rotation"] == null ? twoThousand : (set.Value["rotation"]["enterDate"] == null ? DateTime.MaxValue : DateTime.Parse((string)set.Value["rotation"]["enterDate"], CultureInfo.InvariantCulture)),
							(set.Value["rotation"] == null || set.Value["rotation"]["exitDate"] == null) ? DateTime.MaxValue : DateTime.Parse((string)set.Value["rotation"]["exitDate"], CultureInfo.InvariantCulture),
							extendedCardInfo
						);
					}
				}
				catch (WebException)
				{
					saveCardDatabase = false;
				}

				try
				{
					var result = WebUtilities.FetchStringFromUrl(cardDatabaseUrl);
					File.WriteAllText(".\\logs\\downloaded_card_database.json", result);
					dynamic data = JToken.Parse(result);

					Set.ClearSets();
					foreach (dynamic set in data["sets"])
					{
						string setName1 = (string)set.Name;
						string setName2 = setName1.Replace(":", "");
						if (standardSetsInfo.ContainsKey(setName1))
						{
							Tuple<List<string>, Dictionary<CardRarity, int>, int, DateTime, DateTime, Dictionary<int, Set.CardInfo>> setInfo = standardSetsInfo[setName1];
							Set.CreateSet(set.Name, (string)set.Value["scryfall"], (string)set.Value["arenacode"], setInfo.Item1, setInfo.Item3, setInfo.Item2, setInfo.Item4, setInfo.Item5, setInfo.Item6);
						}
						else if (standardSetsInfo.ContainsKey(setName2))
						{
							Tuple<List<string>, Dictionary<CardRarity, int>, int, DateTime, DateTime, Dictionary<int, Set.CardInfo>> setInfo = standardSetsInfo[setName2];
							Set.CreateSet(set.Name, (string)set.Value["scryfall"], (string)set.Value["arenacode"], setInfo.Item1, setInfo.Item3, setInfo.Item2, setInfo.Item4, setInfo.Item5, setInfo.Item6);
						}
						else
						{
							DateTime rotation = setName1.StartsWith("Arena") ? DateTime.MaxValue : twoThousand;
							// if it isn't a standard set, assume it has already "rotated"
							Set.CreateSet(set.Name, (string)set.Value["scryfall"], (string)set.Value["arenacode"], new List<string>(), 0,
								new Dictionary<CardRarity, int>() {
								{ CardRarity.Common, 0 },
								{ CardRarity.Uncommon, 0 },
								{ CardRarity.Rare, 0 },
								{ CardRarity.MythicRare, 0 }
								}, twoThousand, rotation, new Dictionary<int, Set.CardInfo>());
						}
					}
					Card.ClearCards();
					foreach (dynamic card in data.cards)
					{
						//if ((bool)(card.Value["collectible"] ?? false) || (bool)(card.Value["craftable"] ?? false))
						//{
							string scryfallId = string.Empty;
							if (card.Value["images"] != null)
							{
								scryfallId = (string)card.Value["images"]["normal"];
								if (!string.IsNullOrWhiteSpace(scryfallId))
								{
									scryfallId = scryfallId.Substring(scryfallId.LastIndexOf('/') + 1).Split('.')[0];
								}
							}
							int? rank = (int?)card.Value["rank"];
							string type = (string)card.Value["type"];
							if (!string.IsNullOrWhiteSpace(type))
							{
								Card.CreateCard((int)card.Value["id"], (string)card.Value["name"], (string)card.Value["set"], (string)card.Value["cid"],
									(string)card.Value["rarity"], string.Join("", card.Value["cost"].ToObject<string[]>()).ToUpper(),
									rank ?? -1, type, string.Join("", card.Value["cost"].ToObject<string[]>()).ToUpper(),
									(int)card.Value["cmc"], scryfallId);
							}
						//}
					}
				}
				catch (WebException)
				{
					saveCardDatabase = false;
				}

				if (saveCardDatabase)
				{
					SaveCardDatabase();
				}
			}
		}

		/// <summary>
		/// Gets or sets a delegate to protect data using a salt.
		/// </summary>
		public static Func<byte[], byte[], byte[]> Protect { get; set; } = (data, salt) => Protection.Protect(data, salt);

		/// <summary>
		/// Gets or sets a delegate to unprotect data using a salt.
		/// </summary>
		public static Func<byte[], byte[], byte[]> Unprotect { get; set; } = (data, salt) => Protection.Unprotect(data, salt);

		static byte[] HashAString(string stringToHash)
		{
			using (SHA256 sha256 = SHA256.Create())
			{
				return sha256.ComputeHash(Encoding.ASCII.GetBytes(stringToHash));
			}
		}
	}
}
