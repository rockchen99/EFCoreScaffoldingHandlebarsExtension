using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefineFluteAPI.Models
{
    public partial class Territory
    {
        public Territory()
        {
            EmployeeTerritories = new HashSet<EmployeeTerritories>();
        }

        [StringLength(20)]
        public string TerritoryId { get; set; }
        [Required]
        [StringLength(50)]
        public string TerritoryDescription { get; set; }

        [InverseProperty("Territory")]
        public virtual ICollection<EmployeeTerritories> EmployeeTerritories { get; set; }
    }
}
