namespace OrderProcessor.Application.Contracts.Repositories
{
    public interface IInventoryRepository
    {
        Task<int> GetStockAsync(
                string sku,
                CancellationToken ct);
    }
}