using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RefineFluteAPI.Models
{
    public sealed class ProductMap:IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
                builder.Property(e => e.ProductId).ValueGeneratedNever();

                builder.Property(e => e.Freight).HasDefaultValueSql("((0))");

                builder.Property(e => e.ProductName).IsUnicode(false);

                builder.Property(e => e.RowVersion).IsRowVersion();

                builder.HasOne(d => d.Category)
                    .WithMany(p => p.Product)
                    .HasForeignKey(d => d.CategoryId)
                    .HasConstraintName("FK_Products_Category");

        }
    }
}
