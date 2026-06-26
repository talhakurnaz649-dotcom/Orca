using System;
using System.Collections.Generic;

namespace Orca.Models;

public partial class Game
{
    public int GameId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? OldPrice { get; set; }

    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }

    public string Platform { get; set; } = null!;

    public decimal Rating { get; set; }

    public int Stock { get; set; }

    public bool IsActive { get; set; }

    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
