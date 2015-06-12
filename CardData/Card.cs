using System.Collections.Generic;

namespace CardData
{
	public class Card
	{
		public long CardId { get; set; }

		public long MultiverseId { get; set; }

		public string Name { get; set; }

		public string ManaCost { get; set; }

		public int ConvertedManaCost { get; set; }

		public string OracleText { get; set; }

		public int CardNumber { get; set; }

		public string Power { get; set; }

		public string Toughness { get; set; }

		public string Loyalty { get; set; }

		public virtual ICollection<CardType> Types { get; set; }

		public virtual ICollection<CardSet> Sets { get; set; }
	}
}
