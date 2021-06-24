namespace Core.Dtos.Payment
{
    public class PaymentChargeDto
    {
        public System.Guid OrderId { get; set; }
        public int price { get; set; }
    }
}