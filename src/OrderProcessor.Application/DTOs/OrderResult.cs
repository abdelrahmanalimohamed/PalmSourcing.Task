namespace OrderProcessor.Application.DTOs
{
    public sealed record OrderResult(
     bool Success,
     string Message,
     decimal Total)
    {
        public static OrderResult Ok(decimal total)
            => new(true, "OK", total);
        public static OrderResult Fail(string message)
            => new(false, message, 0);
    }
}