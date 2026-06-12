namespace OrderProcessor.Application.Models
{
    internal sealed record ProcessedLine(
         string Sku,
         bool InStock,
         decimal LineTotal);
}