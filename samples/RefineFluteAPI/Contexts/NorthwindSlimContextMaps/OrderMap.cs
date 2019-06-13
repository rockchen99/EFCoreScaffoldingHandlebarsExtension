using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RefineFluteAPI.Models
{
    public sealed class OrderMap:IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
                builder.Property(e => e.OrderId).ValueGeneratedNever();

                builder.Property(e => e.CustomerId).IsUnicode(false);

                builder.Property(e => e.Freight).HasDefaultValueSql("((0))");

                builder.HasOne(d => d.Customer)
                    .WithMany(p => p.Order)
                    .HasForeignKey(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Order_Customers");

        }
    }
}
