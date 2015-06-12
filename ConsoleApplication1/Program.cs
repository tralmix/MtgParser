namespace ConsoleApplication1
{
	class Program
	{
		static void Main(string[] args)
		{
			SiteParser.PopulateSetsFromRootPage();

			SiteParser.PopulateCardsForEachSet();
		}
	}
}
