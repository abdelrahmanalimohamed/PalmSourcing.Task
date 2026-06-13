using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OrderProcessor.Application.Contracts.Services;
using OrderProcessor.Domain.Models;

namespace OrderHub.Pages
{
    public class ConfirmOrderModel : PageModel
    {
        private readonly IOrderService _orders;

        public ConfirmOrderModel(IOrderService orders)
        {
            _orders = orders;
        }

        public string SchoolName { get; private set; } = string.Empty;
        public IReadOnlyList<OrderLine> Lines { get; private set; } = [];
        public decimal Subtotal { get; private set; }

        public async Task<IActionResult> OnGetAsync(int orderId, CancellationToken ct)
        {
            var order = await _orders.GetAsync(orderId, ct);
            if (order is null) return NotFound();

            if (order.SchoolId != User.GetSchoolId())
                return NotFound();

            SchoolName = order.SchoolName;
            Lines = order.Lines;
            Subtotal = Lines.Sum(l => l.UnitPrice * l.Quantity);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int orderId, IFormCollection form, CancellationToken ct)
        {
            var order = await _orders.GetAsync(orderId, ct);
            if (order is null) return NotFound();

            if (order.SchoolId != User.GetSchoolId())
                return Forbid();

            var updatedQuantities = new Dictionary<int, int>();
            foreach (var line in order.Lines)
            {
                var key = $"qty_{line.Id}";
                if (form.TryGetValue(key, out var raw)
                    && int.TryParse(raw, out var qty)
                    && qty >= 0)
                {
                    updatedQuantities[line.Id] = qty;
                }
            }

            var confirmedTotal = await _orders.RepriceLinesAsync(order, updatedQuantities, ct);

            await _orders.ConfirmAsync(orderId, order.Lines, confirmedTotal, ct);
            return RedirectToPage("OrderConfirmed", new { orderId });
        }
    }
}