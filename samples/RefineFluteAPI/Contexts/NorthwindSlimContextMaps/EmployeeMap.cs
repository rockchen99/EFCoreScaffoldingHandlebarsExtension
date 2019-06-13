using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RefineFluteAPI.Models
{
    public sealed class EmployeeMap:IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
                builder.Property(e => e.EmployeeId).ValueGeneratedNever();

                builder.Property(e => e.City).IsUnicode(false);

                builder.Property(e => e.Country).IsUnicode(false);

                builder.Property(e => e.FirstName).IsUnicode(false);

                builder.Property(e => e.LastName).IsUnicode(false);

        }
    }
}
