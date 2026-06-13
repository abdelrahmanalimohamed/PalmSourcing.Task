(function () {
  "use strict";

  const subtotalEl = document.getElementById("subtotal");

  function computeSubtotal() {
    let total = 0;

    document.querySelectorAll(".qty-input").forEach(function (input) {
      const qty = Math.max(0, parseInt(input.value, 10) || 0);
      const unitPrice = parseFloat(input.dataset.unitPrice) || 0;
      
      const lineTotal = input
        .closest("tr")
        .querySelector(".line-total");

      if (lineTotal) {
        lineTotal.textContent = "£" + (unitPrice * qty).toFixed(2);
      }

      total += unitPrice * qty;
    });

    subtotalEl.textContent = "£" + total.toFixed(2);
  }

  document.querySelectorAll(".qty-input").forEach(function (input) {
    input.addEventListener("input", computeSubtotal);
  });
}());