using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RefineFluteAPI.Models
{
    public sealed class CustomerSettingMap:IEntityTypeConfiguration<CustomerSetting>
    {
        public void Configure(EntityTypeBuilder<CustomerSetting> builder)
        {
                builder.Property(e => e.CustomerId)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                builder.Property(e => e.BirthDate).IsUnicode(false);

                builder.Property(e => e.Setting).IsUnicode(false);

                builder.HasOne(d => d.Customer)
                    .WithOne(p => p.CustomerSetting)
                    .HasForeignKey<CustomerSetting>(d => d.CustomerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CustomerSetting_Customer");

        }
    }
}
