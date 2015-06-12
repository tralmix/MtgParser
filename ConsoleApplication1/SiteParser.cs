using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Net;
using System.Drawing;
using System.Web;
using CardData;
using HtmlAgilityPack;

namespace ConsoleApplication1
{
	public class SiteParser
	{
		private static readonly Random RandomNumber = new Random();
		private static readonly HtmlWeb AgilityWeb = new HtmlWeb();

		private const string Delimiter = ";";
		private const string MultiverseIdText = "multiverseid";

		private const string GathererRootUrl = "http://gatherer.wizards.com";
		private const string SearchPageUrl = "/Pages/Search/Default.aspx?";
		private const string CardDetailsUrl = "/Pages/Card/Details.aspx?multiverseid=";

		private const string SetDropDownListId = "ctl00_ctl00_maincontent_content_searchcontrols_setaddtext";
		private const string SearchTermDisplay = "ctl00_ctl00_ctl00_MainContent_SubContent_SubContentHeader_searchTermDisplay";

		private const string CardDetailName = "ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_nameRow";
		private const string CardDetailManaCost = "ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_manaRow";
		private const string CardDetailConvertedManaCost = "ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_cmcRow";
		private const string CardDetailType = "ctl00_ctl00_ctl00_MainContent_SubContent_SubContent_typeRow";

		internal static void PopulateSetsFromRootPage()
		{
			var rootSearchPage = AgilityWeb.Load(GathererRootUrl);
			var setsDdl = rootSearchPage.GetElementbyId(SetDropDownListId);

			var sets = setsDdl.ChildNodes.Where(c => c.OriginalName == "option").ToList();

			using (var context = new CardDataContext())
			{
				var existingSets = context.Sets.Select(s => s.Name).ToList();

				foreach (var set in sets)
				{
					var setName = HttpUtility.HtmlDecode(set.Attributes.First(a => a.Name == "value").Value);

					if (string.IsNullOrWhiteSpace(setName)) continue;

					if (existingSets.Contains(setName)) continue;

					var setCount = GetCardCountForSet(setName);

					var dbSet = new CardSet
					{
						Name = setName,
						CardCount = setCount
					};

					context.Sets.Add(dbSet);
					existingSets.Add(setName);

					//Pause for random interval up to 15 seconds to prevent being blocked as a DOS attack.
					//Thread.Sleep(1000 * RandomNumber.Next(15));
				}

				context.SaveChanges();
			}
		}

		private static int GetCardCountForSet(string setName)
		{
			var searchUrl = GathererRootUrl + SearchPageUrl + "set=[" + setName + "]";

			var returnValue = 0;

			//add try/catch watch for timeouts
			try
			{
				var setSearchResultsPage = AgilityWeb.Load(searchUrl);
				var searchTermDisplay = setSearchResultsPage.GetElementbyId(SearchTermDisplay);

				var resultCountStart = searchTermDisplay.InnerText.LastIndexOf("(", StringComparison.Ordinal) + 1;
				var resultCountEnd = searchTermDisplay.InnerText.LastIndexOf(")", StringComparison.Ordinal);

				var countAsString = searchTermDisplay.InnerText.Substring(resultCountStart, resultCountEnd - resultCountStart);

				int.TryParse(countAsString, out returnValue);
			}
			catch (Exception)
			{
				//Eat exception
				//todo add logging
			}

			return returnValue;
		}

		internal static void PopulateCardsForEachSet()
		{
			List<CardSet> existingSets;
			using (var context = new CardDataContext())
			{
				existingSets = context.Sets.ToList();
			}

			var testSet = existingSets.First();

			//iterate over existingSets
			var pageFound = true;
			var page = 0;
			var multiverseIds = new List<long>();
			do
			{
				var urlToLoad = GathererRootUrl + SearchPageUrl + "set=[" + testSet.Name + "]" + "&page=" + page;
				HtmlDocument setResults;
				try
				{
					setResults = AgilityWeb.Load(urlToLoad);
				}
				catch (Exception)
				{
					pageFound = false;
					continue;
				}

				var idsToAdd = GetMultiverseIdsFromSearchPage(setResults);
				if (idsToAdd.Any() && !multiverseIds.Contains(idsToAdd.First()))
					multiverseIds.AddRange(idsToAdd);
				else
					pageFound = false;

				page++;
			} while (pageFound);

			PopulateCardsForSet(testSet.SetId, multiverseIds);
		}

		private static void PopulateCardsForSet(int cardSetId, List<long> multiverseIds)
		{
			var isFirstAttempt = true;
			using (var context = new CardDataContext())
			{
				var cardSet = context.Sets.Include("Cards").First(s => s.SetId == cardSetId);

				var existingCardsInSet = context.Sets.Include("Cards")
					.First(s => s.SetId == cardSetId)
					.Cards.Select(c => c.Name)
					.ToList();

				var existingTypes = context.Types.ToList();

				var existingManaSymbols = context.ManaSymbols.Select(s => s.Name).ToList();

				while (true)
				{
					var failedIds = new List<long>();

					foreach (var id in multiverseIds)
					{
						var url = GathererRootUrl + CardDetailsUrl + id;
						HtmlDocument cardDetailPage;
						try
						{
							cardDetailPage = AgilityWeb.Load(url);
						}
						catch (Exception)
						{
							failedIds.Add(id);
							continue;
						}

						//works
						var cardName = cardDetailPage.GetElementbyId(CardDetailName).GetInnerTextOfValueDiv().Trim();

						if (existingCardsInSet.Any(c => c == cardName))
							continue;

						#region Get Mana Cost and Save Mana Symbols
						var manaCostUiContainer = cardDetailPage.GetElementbyId(CardDetailManaCost);
						string manaCost = null;
						if (manaCostUiContainer != null)
						{
							var costSymbols = manaCostUiContainer.ChildNodes
								.First(n => n.Attributes.Contains("class") && n.Attributes.First(a => a.Name == "class").Value == "value")
								.ChildNodes.Where(n => n.Name == "img").ToList();
							foreach (var node in costSymbols)
							{
								var altText = node.Attributes.First(a => a.Name == "alt").Value;
								var imageUrl = node.Attributes.First(a => a.Name == "src").Value;
								var nameSubstring = imageUrl.Substring(imageUrl.IndexOf("name=", StringComparison.Ordinal));
								var indexOfAmpersand = nameSubstring.IndexOf("&", StringComparison.Ordinal);
								var startIndex = nameSubstring.IndexOf("=", StringComparison.Ordinal) + 1;
								var name = indexOfAmpersand == -1
									? nameSubstring.Substring(startIndex)
									: nameSubstring.Substring(startIndex, indexOfAmpersand - startIndex);

								var httpWebRequest = (HttpWebRequest)WebRequest.Create(GathererRootUrl + imageUrl);

								using (var httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
								{
									using (var stream = httpWebReponse.GetResponseStream())
									{
										//todo make this work
										//if (stream != null) Image.FromStream(stream).Save(name + ".jpeg");
									}
								}

								if (!existingManaSymbols.Contains(name))
								{
									existingManaSymbols.Add(name);

									var newManaSymbol = new ManaSymbol
									{
										Name = name,
										AltText = altText,
										Ordinal = 0,
										ImageLocation = null//Path.GetFullPath("/Images/" + name + ".jpeg")
									};
									context.ManaSymbols.Add(newManaSymbol);
								}

								manaCost += name + Delimiter;
							}
						}
						#endregion

						#region Convereted Mana Cost
						var convertedCostUiContainer = cardDetailPage.GetElementbyId(CardDetailConvertedManaCost);
						var convertedManaCost = 0;
						if (convertedCostUiContainer != null)
						{
							var cmcText = convertedCostUiContainer.GetInnerTextOfValueDiv();

							int cmc;
							int.TryParse(cmcText, out cmc);
							if (cmc != -1) convertedManaCost = cmc;
						}
						#endregion

						#region Types
						var typesContainer = cardDetailPage.GetElementbyId(CardDetailType);
						var cardsTypes = new List<CardType>();
						if (typesContainer != null)
						{
							var typesAsString = typesContainer.GetInnerTextOfValueDiv();
							var primaryTypesString = typesAsString.Split('—').First();
							var subTypesString = typesAsString.Split('—').Last();

							foreach (var primaryType in primaryTypesString.Split(' '))
							{
								var cardType = existingTypes.FirstOrDefault(t => t.Name == primaryType.Trim());
								if (cardType == null)
								{
									cardType = new CardType
									{
										Name = primaryType.Trim(),
										IsSubType = false
									};
									existingTypes.Add(cardType);
								}
								cardsTypes.Add(cardType);
							}

							foreach (var primaryType in primaryTypesString.Split(' '))
							{
								var cardType = existingTypes.FirstOrDefault(t => t.Name == primaryType.Trim());
								if (cardType == null)
								{
									cardType = new CardType
									{
										Name = primaryType.Trim(),
										IsSubType = true
									};
									existingTypes.Add(cardType);
								}
								cardsTypes.Add(cardType);
							}
						}
						#endregion

						var card = new Card
						{
							MultiverseId = id,
							Name = cardName,
							ManaCost = manaCost,
							ConvertedManaCost = convertedManaCost,
							Types = cardsTypes
						};

						if(cardSet.Cards.All(c=>c.Name != card.Name))
							cardSet.Cards.Add(card);

						context.Cards.Add(card);
					}

					if (isFirstAttempt && failedIds.Any())
					{
						multiverseIds = failedIds;
						isFirstAttempt = false;
						continue;
					}

					break;
				}
				context.SaveChanges();
			}
		}

		private static List<long> GetMultiverseIdsFromSearchPage(HtmlDocument searchPage)
		{
			var multiverseIds = new List<long>();
			var links = searchPage.DocumentNode.Descendants()
						.Where(htmlNode => htmlNode.Attributes.Contains("href"))
						.Select(htmlNode => htmlNode.Attributes.First(a => a.Name == "href"))
						.Select(a => a.Value)
						.ToList();
			foreach (var link in links)
			{
				var startIndex = link.IndexOf(MultiverseIdText, StringComparison.Ordinal);
				if (startIndex == -1) continue;
				var subText = link.Substring(startIndex);
				var indexOfAmpersand = subText.IndexOf("&", StringComparison.Ordinal);
				startIndex = subText.IndexOf("=", StringComparison.Ordinal) + 1;

				var idAsString = indexOfAmpersand == -1
					? subText.Substring(startIndex)
					: subText.Substring(startIndex, indexOfAmpersand - startIndex);

				int multiverseId;
				int.TryParse(idAsString, out multiverseId);
				if (multiverseId != 0)
					multiverseIds.Add(multiverseId);
			}
			return multiverseIds.Distinct().ToList();
		}
	}

	public static class HtmlNodeExtensions
	{
		public static string GetInnerTextOfValueDiv(this HtmlNode node)
		{
			return
				node.ChildNodes.First(
					n => n.Attributes.Contains("class") && n.Attributes.First(a => a.Name == "class").Value == "value").InnerText;
		}
	}
}
