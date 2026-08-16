namespace Backend.DTOs
{
    public class VerifyPaymentRequest
    {
        public string TxHash { get; set; }
    }

    public class PaymentResponse
    {
        public string Message { get; set; }
        public DateTime NextPayment { get; set; }
        public DateTime LastPayment { get; set; }
        public string Status { get; set; }
        public bool IsSubscribed { get; set; }
    }
}
