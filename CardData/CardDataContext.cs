using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace CardData
{
	public class CardDataContext :DbContext
	{
		public CardDataContext()
		{
			
		}

		public virtual IDbSet<Card> Cards { get; set; }

		public virtual IDbSet<CardSet> Sets { get; set; }

		public virtual IDbSet<CardType> Types { get; set; }

		public virtual IDbSet<ManaSymbol> ManaSymbols { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Card>()
				.ToTable("Cards")
				.HasKey(c => c.CardId)
				.Property(c=>c.CardId)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

			modelBuilder.Entity<Card>()
				.Property(c => c.MultiverseId);

			modelBuilder.Entity<Card>()
				.Property(c => c.Name)
				.HasMaxLength(256)
				.IsUnicode(false);

			modelBuilder.Entity<Card>()
				.Property(c => c.ManaCost)
				.HasMaxLength(30)
				.IsUnicode(false);

			modelBuilder.Entity<Card>()
				.Property(c => c.ConvertedManaCost);

			modelBuilder.Entity<Card>()
				.Property(c => c.OracleText)
				.HasMaxLength(2048)
				.IsUnicode(false);

			modelBuilder.Entity<Card>()
				.Property(c => c.Power)
				.HasMaxLength(10)
				.IsUnicode(false);

			modelBuilder.Entity<Card>()
				.Property(c => c.Toughness)
				.HasMaxLength(10)
				.IsUnicode(false);

			modelBuilder.Entity<Card>()
				.Property(c => c.Loyalty)
				.HasMaxLength(10)
				.IsUnicode(false);

			modelBuilder.Entity<Card>()
				.HasMany(c => c.Types);

			modelBuilder.Entity<Card>()
				.HasMany(c => c.Sets)
				.WithMany(s=>s.Cards);

			modelBuilder.Entity<CardSet>()
				.ToTable("Sets")
				.HasKey(s => s.SetId)
				.Property(s=>s.SetId)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

			modelBuilder.Entity<CardSet>()
				.Property(s => s.Name)
				.HasMaxLength(2048)
				.IsRequired()
				.IsUnicode(false);

			modelBuilder.Entity<CardSet>()
				.Property(s => s.CardCount)
				.IsRequired();

			modelBuilder.Entity<CardSet>()
				.HasMany(s => s.Cards)
				.WithMany(c => c.Sets);

			modelBuilder.Entity<CardType>()
				.ToTable("Types")
				.HasKey(s => s.TypeId)
				.Property(s => s.TypeId)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

			modelBuilder.Entity<CardType>()
				.Property(t => t.Name)
				.HasMaxLength(20)
				.IsUnicode(false)
				.IsRequired();

			modelBuilder.Entity<CardType>()
				.Property(t => t.IsSubType)
				.IsRequired();

			modelBuilder.Entity<ManaSymbol>()
				.ToTable("ManaSymbols")
				.HasKey(s => s.ManaSymbolId)
				.Property(ms => ms.ManaSymbolId)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

			modelBuilder.Entity<ManaSymbol>()
				.Property(t => t.Name)
				.HasMaxLength(20)
				.IsUnicode(false)
				.IsRequired();

			modelBuilder.Entity<ManaSymbol>()
				.Property(t => t.AltText)
				.HasMaxLength(40)
				.IsUnicode(false)
				.IsRequired();

			modelBuilder.Entity<ManaSymbol>()
				.Property(ms => ms.Ordinal);

			modelBuilder.Entity<ManaSymbol>()
				.Property(ms => ms.ImageLocation)
				.IsUnicode(false);

			base.OnModelCreating(modelBuilder);
		}
	}
}
