using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardData
{
	public class Card
	{
		public long Id { get; set; }

		public long MultiverseId { get; set; }

		public string Name { get; set; }

		public string OracleText { get; set; }

		public int CardNumber { get; set; }

		public string Power { get; set; }

		public string Toughness { get; set; }

		public string Loyalty { get; set; }

		public List<CardType> Types { get; set; }

		public List<CardSubType> SubTypes { get; set; }

		public List<CardSet> Sets { get; set; }
	}
}
