namespace OrderProcessor.Domain.Models
{
    public record OrderLine(
        string Sku, 
        int Quantity,
        string? Embroidery = null);
}