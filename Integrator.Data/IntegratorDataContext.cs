using Integrator.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Integrator.Data
{
    public class IntegratorDataContext:DbContext
    {
        private readonly string _connectionString;

        public IntegratorDataContext(DbContextOptions<IntegratorDataContext> options) : base(options)
        {
        }

        public IntegratorDataContext() : base()
        {
        }

        public IntegratorDataContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!string.IsNullOrEmpty(_connectionString))
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Entities

            modelBuilder.Entity<Card>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Shop)
                    .WithMany(x => x.Cards)
                    .HasForeignKey(x => x.ShopId);

                entity.HasOne(x => x.CardTranslation)
                    .WithOne(x => x.Card)
                    .HasForeignKey<CardTranslation>(x => x.CardId);

                entity.HasOne(x => x.CardDetail)
                    .WithOne(x => x.Card)
                    .HasForeignKey<CardDetail>(x => x.CardId);


                entity.HasOne(x => x.BrandDraft)
                    .WithOne(x => x.Card)
                    .HasForeignKey<BrandDraft>(x => x.CardId);

                entity.HasOne(x => x.CategoryDraft)
                    .WithOne(x => x.Card)
                    .HasForeignKey<CategoryDraft>(x => x.CardId);
            });

            modelBuilder.Entity<CardImage>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Card)
                    .WithMany(x => x.Images)
                    .HasForeignKey(x => x.CardId);
            });

            modelBuilder.Entity<CardTranslation>(entity =>
            {
                entity.HasKey(x => x.CardId);
            });


            modelBuilder.Entity<CardDetail>(entity =>
            {
                entity.HasKey(x => x.CardId);

                entity.HasOne(x => x.Brand)
                    .WithMany(x => x.CardDetails)
                    .HasForeignKey(x => x.BrandId);

                entity.HasOne(x => x.Category)
                    .WithMany(x => x.CardDetails)
                    .HasForeignKey(x => x.CategoryId);
            });

            modelBuilder.Entity<CardDetailSize>(entity =>
            {
                entity.HasKey(x => new { x.CardId, x.SizeId });

                entity.HasOne(x => x.CardDetail)
                    .WithMany(x => x.CardDetailSizes)
                    .HasForeignKey(x => x.CardId);

                entity.HasOne(x => x.Size)
                    .WithMany(x => x.CardDetailSizes)
                    .HasForeignKey(x => x.SizeId);
            });

            modelBuilder.Entity<Size>(entity =>
            {
                entity.HasKey(x => x.Id);
            });


            modelBuilder.Entity<Shop>(entity =>
            {
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<CategoryDraft>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Ignore(x => x.LongFullName);

                entity.HasOne(x => x.Category)
                    .WithMany(x => x.CategoryDrafts)
                    .HasForeignKey(x => x.CategoryId);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(x => x.Id);
            });

            modelBuilder.Entity<BrandDraft>(entity =>
            {
                entity.HasKey(x => x.Id);
                
                entity.Ignore(x => x.LongFullName);

                entity.HasOne(x => x.Brand)
                    .WithMany(x => x.BrandDrafts)
                    .HasForeignKey(x => x.BrandId);
            });


            modelBuilder.Entity<Template>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Ignore(x => x.GetCardText);
            });

            modelBuilder.Entity<CardDetailTemplateMatch>(entity =>
            {
                entity.HasKey(x => new { x.CardDetailId, x.TemplateId });

                entity.HasOne(x => x.CardDetail)
                    .WithMany(x => x.TemplateMatches)
                    .HasForeignKey(x => x.CardDetailId);

                entity.HasOne(x => x.Template)
                    .WithMany(x => x.CardDetailMatches)
                    .HasForeignKey(x => x.TemplateId);
            });


            //modelBuilder.Entity<ThoughtCognitiveError>(entity =>
            //{
            //    entity.HasKey(x => new { x.ThoughtId, x.CognitiveErrorId });
            //    entity.HasOne(x => x.Thought)
            //        .WithMany(x => x.ThoughtCognitiveErrors)
            //        .HasForeignKey(x => x.ThoughtId);
            //});

            //modelBuilder.Entity<ThoughtEmotion>(entity =>
            //{
            //    entity.HasKey(x => new { x.ThoughtId, x.EmotionId, x.State });
            //    entity.HasOne(x => x.Thought)
            //        .WithMany(x => x.ThoughtEmotions)
            //        .HasForeignKey(x => x.ThoughtId);
            //});

            //modelBuilder.Entity<Patient>(entity =>
            //{
            //    entity.HasKey(x => x.Id);
            //});

            //modelBuilder.Entity<Psychologist>(entity =>
            //{
            //    entity.HasKey(x => x.Id);
            //});

            #endregion
        }
    }
}
