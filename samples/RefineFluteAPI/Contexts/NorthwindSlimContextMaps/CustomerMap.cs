using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RefineFluteAPI.Models
{
    public sealed class CustomerMap:IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
                builder.Property(e => e.CustomerId)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                builder.Property(e => e.City).IsUnicode(false);

                builder.Property(e => e.CompanyName).IsUnicode(false);

                builder.Property(e => e.ContactName).IsUnicode(false);

                builder.Property(e => e.Country).IsUnicode(false);

        }
    }
}
