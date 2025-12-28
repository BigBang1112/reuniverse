namespace BlazorWasmSample;

public enum UserStatus
{
    Active,
    Suspended,
    Deleted
}

public class Address
{
    public required string Street { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required GeoLocation? Location { get; set; }
}

public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class UserPreferences
{
    public bool ReceiveNewsletter { get; set; }
    public required Dictionary<string, string> CustomSettings { get; set; }
}

public class Order
{
    public Guid OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public required List<OrderItem> Items { get; set; }
    public decimal TotalAmount { get; set; }
    public required Dictionary<string, object?> Metadata { get; set; }
}

public class OrderItem
{
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public required List<string> Tags { get; set; }
}

public class User
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public UserStatus Status { get; set; }
    public DateTime? LastLogin { get; set; }

    public required Address PrimaryAddress { get; set; }
    public required List<Address> PreviousAddresses { get; set; }

    public UserPreferences? Preferences { get; set; }
    public required List<Order> Orders { get; set; }

    public required Dictionary<string, object> Extensions { get; set; }
}