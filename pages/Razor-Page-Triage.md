# Original Code : 
@page
@model ConfirmOrderModel
<h1>Order for @Html.Raw(Model.SchoolName)</h1>
<form method="post">
  @foreach (var line in Model.Lines) {
    <div onclick="updateQty(@line.Id)">
      <span>@line.Sku — @line.Embroidery</span>
      <input name="qty_@line.Id" value="@line.Quantity" />
      <span>£@(line.UnitPrice * line.Quantity)</span>
    </div>
  }
  <div style="font-size:18px;color:#333">Subtotal: £@Model.Subtotal</div>
  <button type="submit">Confirm</button>
</form>
<script>
  function updateQty(id) { document.forms[0].submit(); }
</script>

# Issue 1: Html.Raw(Model.SchoolName)
This bypasses Razor's automatic HTML encoding.
If a school name contains unexpected HTML: it would be rendered directly.
This introduces an XSS risk on an admin-facing page that handles order confirmation.
Fix: remove Html.Raw and use plain @Model.SchoolName encodes safely.

# Issue 2: Entire Form Posts on Any Click
<div onclick="updateQty(@line.Id)">
Clicking anywhere inside a line:
- Quantity Input
- SKU Text
- Embroidery Text
submits the entire form.
During back-to-school season admins often edit dozens of lines.
This creates:
- Unnecessary server load
- Poor UX
- Accidental submissions
Fix
Listen only for quantity changes and update totals asynchronously.

# Issue 3: No Input Validation
<input name="qty_@line.Id" value="@line.Quantity" />
qty_@line.Id is a free-text input, a crafty user could POST qty_5=-999
Could produce:
- Invalid Orders
- Inventory Issues
- Pricing Errors
Fix
Use:
<input type="number"
       min="1"
       max="999" />
plus server-side validation.

# Rewrite the page targeting .NET 8 Razor Pages with a clean split between server-rendered structure and client interaction.
For brevity, the PageModel assumes an existing
IOrderService.

Business rules remain in the Application layer.
The Razor Page acts only as an orchestration layer
between the UI and application services.

# When a school admin changes a line quantity, the subtotal updates without a page reload.
File exist in js folder as confirm-order.js

