namespace BlazorWasmSample;

public enum UserStatus
{
    Active,
    Suspended,
    Deleted
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
    public GeoLocation Location { get; set; }
}

public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class UserPreferences
{
    public bool ReceiveNewsletter { get; set; }
    public Dictionary<string, string> CustomSettings { get; set; }
}

public class Order
{
    public Guid OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal TotalAmount { get; set; }
    public Dictionary<string, object?> Metadata { get; set; }
}

public class OrderItem
{
    public string Sku { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public List<string> Tags { get; set; }
}

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public UserStatus Status { get; set; }
    public DateTime? LastLogin { get; set; }

    public Address PrimaryAddress { get; set; }
    public List<Address> PreviousAddresses { get; set; }

    public UserPreferences? Preferences { get; set; }
    public List<Order> Orders { get; set; }

    public Dictionary<string, object> Extensions { get; set; }
}