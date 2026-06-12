using OrderProcessor.Domain.Models;

namespace OrderProcessor.Application.DTOs
{
    public sealed record ProcessOrderRequest(
      int SchoolId,
      string ParentEmail,
      IReadOnlyCollection<OrderLine> Lines);
}