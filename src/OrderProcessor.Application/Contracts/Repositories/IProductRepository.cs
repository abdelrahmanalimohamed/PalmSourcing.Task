namespace OrderProcessor.Application.Contracts.Repositories
{
    public interface IProductRepository
    {
        Task<decimal> GetBasePriceAsync(
            string sku,
            CancellationToken ct);
    }
}