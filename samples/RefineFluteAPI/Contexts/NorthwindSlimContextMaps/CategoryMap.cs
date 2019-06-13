using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RefineFluteAPI.Models
{
    public sealed class CategoryMap:IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
                builder.Property(e => e.CategoryId).ValueGeneratedNever();

                builder.Property(e => e.CategoryName).IsUnicode(false);

        }
    }
}
