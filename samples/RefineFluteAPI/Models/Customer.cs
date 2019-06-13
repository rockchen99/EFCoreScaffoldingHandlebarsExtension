using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefineFluteAPI.Models
{
    public partial class Customer
    {
        public Customer()
        {
            Order = new HashSet<Order>();
        }

        [StringLength(5)]
        public string CustomerId { get; set; }
        [Required]
        [StringLength(40)]
        public string CompanyName { get; set; }
        [StringLength(30)]
        public string ContactName { get; set; }
        [StringLength(15)]
        public string City { get; set; }
        [StringLength(15)]
        public Country Country { get; set; }

        [InverseProperty("Customer")]
        public virtual CustomerSetting CustomerSetting { get; set; }
        [InverseProperty("Customer")]
        public virtual ICollection<Order> Order { get; set; }
    }
}
