using System;
using System.Linq;
using System.Threading;
using System.Web;
using CardData;
using HtmlAgilityPack;

namespace ConsoleApplication1
{
	public class SiteParser
	{
		private static readonly Random RandomNumber = new Random();
		private static readonly HtmlWeb AgilityWeb = new HtmlWeb();

		private const string GathererRootUrl = "http://gatherer.wizards.com";
		private const string SearchPageUrl = "/Pages/Search/Default.aspx?";

		private const string SetDropDownListId = "ctl00_ctl00_maincontent_content_searchcontrols_setaddtext";
		private const string SearchTermDisplay = "ctl00_ctl00_ctl00_MainContent_SubContent_SubContentHeader_searchTermDisplay";

		internal static void GetSetsFromRootPage()
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

					Thread.Sleep(1000 * RandomNumber.Next(15));
				}

				context.SaveChanges();
			}
		}

		private static int GetCardCountForSet(string setName)
		{
			var searchUrl = GathererRootUrl + SearchPageUrl + "set=[" + setName + "]";
			//add try/catch watch for timeouts
			var setSearchResultsPage = AgilityWeb.Load(searchUrl);
			var searchTermDisplay = setSearchResultsPage.GetElementbyId(SearchTermDisplay);

			var resultCountStart = searchTermDisplay.InnerText.LastIndexOf("(", System.StringComparison.Ordinal) + 1;
			var resultCountEnd = searchTermDisplay.InnerText.LastIndexOf(")", System.StringComparison.Ordinal);

			var countAsString = searchTermDisplay.InnerText.Substring(resultCountStart, resultCountEnd - resultCountStart);
			//use int.tryparse to avoid breaking
			return int.Parse(countAsString);
		}
	}
}
