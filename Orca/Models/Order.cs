using System;
using System.Collections.Generic;

namespace Orca.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual User User { get; set; } = null!;
}
