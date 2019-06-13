using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RefineFluteAPI.Models
{
    public sealed class TerritoryMap:IEntityTypeConfiguration<Territory>
    {
        public void Configure(EntityTypeBuilder<Territory> builder)
        {
                builder.Property(e => e.TerritoryId)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                builder.Property(e => e.TerritoryDescription).IsUnicode(false);

        }
    }
}
