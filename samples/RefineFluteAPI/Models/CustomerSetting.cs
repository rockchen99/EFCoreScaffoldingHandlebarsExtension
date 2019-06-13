using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefineFluteAPI.Models
{
    public partial class CustomerSetting
    {
        [Key]
        [StringLength(5)]
        public string CustomerId { get; set; }
        [StringLength(5)]
        public string BirthDate { get; set; }
        [Required]
        [StringLength(50)]
        public string Setting { get; set; }

        [ForeignKey("CustomerId")]
        [InverseProperty("CustomerSetting")]
        public virtual Customer Customer { get; set; }
    }
}
