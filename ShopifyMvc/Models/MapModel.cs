public class UpdateShopifyProductRequest
{
    public string id { get; set; }
    public string title { get; set; }
    public List<Variant> variants { get; set; }
}

public class Variant
{
    public string id { get; set; }
    public string price { get; set; }
}