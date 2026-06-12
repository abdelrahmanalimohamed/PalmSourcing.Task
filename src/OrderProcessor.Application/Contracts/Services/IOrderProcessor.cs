using OrderProcessor.Application.DTOs;

namespace OrderProcessor.Application.Contracts.Services
{
    public interface IOrderProcessor
    {
        Task<OrderResult> ProcessAsync(
                  ProcessOrderRequest request,
                  CancellationToken ct = default);
    }
}