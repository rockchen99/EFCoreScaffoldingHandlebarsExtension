using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefineFluteAPI.Models
{
    public partial class Product
    {
        public Product()
        {
            OrderDetail = new HashSet<OrderDetail>();
        }

        public int ProductId { get; set; }
        [Required]
        [StringLength(5)]
        public string ProductName { get; set; }
        public int? CategoryId { get; set; }
        [Column(TypeName = "money")]
        public decimal? UnitPrice { get; set; }
        public bool? Discontinued { get; set; }
        [Required]
        public byte[] RowVersion { get; set; }
        [Column(TypeName = "money")]
        public decimal? Freight { get; set; }

        [ForeignKey("CategoryId")]
        [InverseProperty("Product")]
        public virtual Category Category { get; set; }
        [InverseProperty("Product")]
        public virtual ICollection<OrderDetail> OrderDetail { get; set; }
    }
}
