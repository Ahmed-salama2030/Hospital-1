using System;
using System.Threading.Tasks;
using Core.Dtos.Order;
using Core.Dtos.Payment;

namespace Core.Interfaces.Order
{
    public interface IOrder:IOrderPayment
    {
         Task<bool> RegisterOrder(Guid userId, RegisterOrderDto registerOrderDto);
         Task<bool> OrderPerationPaymentOnlineAsync(Guid UserId, string stripeToken, PaymentChargeDto paymentChargeDto);
         Task<bool> OrderOPerationPaymentDelivery(Guid userId, RegisterOrderDto registerOrderDto);
    }
}