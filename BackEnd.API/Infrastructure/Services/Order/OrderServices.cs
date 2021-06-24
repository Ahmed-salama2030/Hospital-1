using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Core.Dtos.Order;
using Core.Dtos.Payment;
using Core.Entities;
using Core.Entities.pay;
using Core.Interfaces;
using Core.Interfaces.Order;
using Core.Interfaces.payments;
using Microsoft.Extensions.Options;
using Stripe;

namespace Infrastructure.Services.Order
{
    public class OrderServices : IOrder
    {
        private readonly IMapper _mapper;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly IUnitOfWork<Payment> _payment;
        private readonly IPaymentStripe _stripServices;

        public OrderServices(IMapper mapper,
              IOptions<StripeSettings> stripeSettings,
              IPaymentStripe stripServices,
              IUnitOfWork<Payment> Payment)
        {
            _stripServices = stripServices;
            _payment = Payment;
           _stripeSettings = stripeSettings;
           _mapper = mapper;
        }

     

        public Core.Entities.Order ChargeOnDelivery(Core.Entities.Order order)
        {
            order.IsPaidOndelivery = true;
            order.IsPaidOnline = true;
            return order;
        }

        public bool OrderOPerationPaymentDelivery(Guid userId, RegisterOrderDto registerOrderDto)
        {
            var order=_mapper.Map<Core.Entities.Order>(registerOrderDto);
             ChargeOnDelivery(order);

             return true;
        }

        public async Task<bool> OrderPerationPaymentOnlineAsync(Guid UserId, string stripeToken, PaymentChargeDto paymentChargeDto)
        {
         var result=  await _stripServices.Charge(UserId,stripeToken,paymentChargeDto);

         
         return true;
        }
 

        Task<bool> IOrder.OrderOPerationPaymentDelivery(Guid userId, RegisterOrderDto registerOrderDto)
        {
            throw new NotImplementedException();
        }

        Task<bool> IOrder.RegisterOrder(Guid userId, RegisterOrderDto registerOrderDto)
        {
            throw new NotImplementedException();
        }
    }

     
}
 