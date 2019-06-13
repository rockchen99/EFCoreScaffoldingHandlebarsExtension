using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RefineFluteAPI.Models
{
    public sealed class Category1Map:IEntityTypeConfiguration<Category1>
    {
        public void Configure(EntityTypeBuilder<Category1> builder)
        {
                builder.HasKey(e => e.CategoryId);

                builder.ToTable("Category", "dbo2");

                builder.Property(e => e.CategoryId).ValueGeneratedNever();

                builder.Property(e => e.CategoryName)
                    .IsRequired()
                    .HasMaxLength(15)
                    .IsUnicode(false);

        }
    }
}
