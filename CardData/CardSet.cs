using System.Collections.Generic;

namespace CardData
{
	public class CardSet
	{
		public int SetId { get; set; }

		public string Name { get; set; }

		public int CardCount { get; set; }

		public bool SetPopulated { get; set; }

		public virtual ICollection<Card> Cards { get; set; }
	}
}
