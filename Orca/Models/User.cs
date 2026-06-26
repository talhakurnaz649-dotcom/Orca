using System;
using System.Collections.Generic;

namespace Orca.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public bool IsAdmin { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}