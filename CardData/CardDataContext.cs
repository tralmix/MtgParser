using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		public virtual IDbSet<CardSubType> SubTypes { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Card>()
				.ToTable("Cards")
				.HasKey(c => c.Id)
				.Property(c=>c.Id)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

			modelBuilder.Entity<Card>()
				.Property(c => c.MultiverseId);

			modelBuilder.Entity<Card>()
				.Property(c => c.Name);

			modelBuilder.Entity<Card>()
				.Property(c => c.OracleText);

			modelBuilder.Entity<Card>()
				.Property(c => c.Power);

			modelBuilder.Entity<Card>()
				.Property(c => c.Toughness);

			modelBuilder.Entity<Card>()
				.Property(c => c.Loyalty);

			modelBuilder.Entity<Card>()
				.HasMany(c => c.Types);

			modelBuilder.Entity<Card>()
				.HasMany(c => c.SubTypes);

			modelBuilder.Entity<Card>()
				.HasMany(c => c.Sets);

			modelBuilder.Entity<CardSet>()
				.ToTable("Sets")
				.HasKey(s => s.SetId)
				.Property(s=>s.SetId)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

			modelBuilder.Entity<CardSubType>()
				.ToTable("SubType")
				.HasKey(s => s.Id)
				.Property(s => s.Id)
				.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

			base.OnModelCreating(modelBuilder);
		}
	}
}
