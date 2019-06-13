using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefineFluteAPI.Models
{
    public partial class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int? OrderId { get; set; }
        public int? ProductId { get; set; }
        [Column(TypeName = "money")]
        public decimal? UnitPrice { get; set; }
        public short? Quantity { get; set; }
        public double? Discount { get; set; }

        [ForeignKey("OrderId")]
        [InverseProperty("OrderDetail")]
        public virtual Order Order { get; set; }
        [ForeignKey("ProductId")]
        [InverseProperty("OrderDetail")]
        public virtual Product Product { get; set; }
    }
}
