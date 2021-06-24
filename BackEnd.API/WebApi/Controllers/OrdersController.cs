using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Core.Dtos.Order;
using Core.Dtos.product;
using Core.Entities;
using Core.Entities.pay;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Stripe;
using WebApi.helper.ExtensionsMethod;
using WebApi.helper.pagination;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/{userId}/[controller]")]
    [AllowAnonymous]
    public class OrdersController : ControllerBase
    {
        private readonly IUnitOfWork<Core.Entities.Order> _order;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork<ProductOrder> _productOrder;
        private readonly IUnitOfWork<Core.Entities.Product> _product;
        private readonly IUnitOfWork<ShoppingCart> _shoppingCart;
        private readonly IStringLocalizer<OrdersController> _localizer;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly IUnitOfWork<Payment> _payment;
        public OrdersController(
            IUnitOfWork<Core.Entities.Order> Order,
            IOptions<StripeSettings> stripeSettings,
            IUnitOfWork<Payment> Payment,
            IUnitOfWork<Core.Entities.Product> product,
            IUnitOfWork<ProductOrder> ProductOrder,
            IUnitOfWork<ShoppingCart> shoppingCart,
           IStringLocalizer<OrdersController> localizer,

            IMapper mapper)
        {
            _shoppingCart = shoppingCart;
            _localizer = localizer;
            _product = product;
            _productOrder = ProductOrder;
            _order = Order;
            _mapper = mapper;
            _payment = Payment;
            _stripeSettings = stripeSettings;
        }
        [HttpGet]
        public async Task<IActionResult> Get(Guid userId, [FromQuery] PaginationParam paginationParam)
        {

            var Orders = _order.Table.GetQueryable(a => a.UserId == userId, po => po.ProductOrders);

            if ((!string.IsNullOrEmpty(paginationParam.filterType)) && (!string.IsNullOrEmpty(paginationParam.filterValue)))
                Orders = Filter(Orders, paginationParam);
            if ((!string.IsNullOrEmpty(paginationParam.sortType)))
                Orders = Sort(Orders, paginationParam);
            var PagedList = await PagedList<Core.Entities.Order>.CreateAsync(Orders, paginationParam.pageNumber, paginationParam.PageSize);
            var OrdersReturn = _mapper.Map<IEnumerable<OrderListDtos>>(PagedList);
            Response.AddPagination(PagedList.CurrentPage, PagedList.PageSize, PagedList.TotalItems, PagedList.TotalPages);
            return Ok(OrdersReturn);

        }
        [HttpGet("{id}", Name = "GetOrder")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var returnOrder = await _shoppingCart.Table.FindBy(u => u.Id == id);
            if (returnOrder == null)
                return BadRequest(_localizer["not fount"].Value);
            return Ok(_mapper.Map<OrderListDtos>(returnOrder));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(Guid userId, RegisterOrderDto registerOrderDto)
        {
            var productShopping = _shoppingCart.Table.GetQueryable(u => u.UserId == userId);
            if (productShopping == null)
             return BadRequest(_localizer["not fount"].Value);
             var order = CreateOrder(userId, registerOrderDto);
            registerProductOrder(order.Id, productShopping);
            _shoppingCart.Table.DeleteRange(productShopping.ToArray());
           var result = await _order.SaveAllAsync();
            if (result)
            {
                await _productOrder.SaveAllAsync();
                await _shoppingCart.SaveAllAsync();
                var OrderDto = await _order.Table.FindBy(u => u.Id == order.Id);
                var newOrderDto = _mapper.Map<OrderListDtos>(OrderDto);
                return CreatedAtRoute("GetOrder", new { controller = "api/" + userId + "/Orders", id = OrderDto.Id }, newOrderDto);
            }
            else
                return BadRequest(_localizer["fails add"].Value);
        }


        [HttpGet("orderDetail/{id}")]
        public async Task<IActionResult> OrderDetails(Guid id, [FromQuery] PaginationParam paginationParam)
        {
            var orderDetails = _productOrder.Table.GetQueryable(u => u.OrderId == id).Join(_product.Table.GetQueryable(c => c.Category, p => p.ProductImages, co => co.ProductColors), po => po.ProductId, p => p.Id, (po, p) => new OrderDetailDtos
            {
                ProductQuantity = po.Quantity,
                Products = _mapper.Map<ProductListShoppingDto>(p)
            });
            var PagedList = await PagedList<OrderDetailDtos>.CreateAsync(orderDetails, paginationParam.pageNumber, paginationParam.PageSize);
            Response.AddPagination(PagedList.CurrentPage, PagedList.PageSize, PagedList.TotalItems, PagedList.TotalPages);
            return Ok(PagedList);

        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid userId, Guid id)
        {
            var Order = await _order.Table.SingleOrDefaultAsync(a => a.Id == id);
            if (Order == null)
                return BadRequest(_localizer["not fount"].Value);
            _order.Table.Delete(Order);
            var result = await _order.SaveAllAsync();
            if (result)
                return Ok();
            else
                return BadRequest(_localizer["not delete"].Value);
        }
        [HttpDelete("{orderid}/productdelete/{productid}")]
        public async Task<IActionResult> DeleteOrderProduct(Guid productid, Guid orderid)
        {
            var poroductOrder = await _productOrder.Table.SingleOrDefaultAsync(a => a.ProductId == productid && a.OrderId == orderid);
            if (poroductOrder == null)
                return BadRequest(_localizer["not fount"].Value);
            _productOrder.Table.Delete(poroductOrder);
            var result = await _productOrder.SaveAllAsync();
            if (result)
                return Ok();
            else
                return BadRequest(_localizer["not delete"].Value);
        }


        [HttpPut("productupdate")]
        public async Task<IActionResult> UpdateProductOrder(ProductOrderRegisterDto productorder)
        {
            var poroductOrder = await _productOrder.Table.SingleOrDefaultAsync(a => a.ProductId == productorder.ProductId && a.OrderId == productorder.OrderId);
            if (poroductOrder == null)
                return BadRequest(_localizer["not fount"].Value);
            poroductOrder.Quantity = productorder.Quantity;
            _productOrder.Table.Update(poroductOrder);
            var result = await _productOrder.SaveAllAsync();
            if (result)
                return Ok();
            else
                return BadRequest(_localizer["not update"].Value);
        }

        private void RegisterOrder(IQueryable<ShoppingCart> productShopping)
        {

        }
        private void registerProductOrder(Guid orderId, IQueryable<ShoppingCart> productShopping)
        {
            foreach (var shopping in productShopping)
            {
                var productOrder = new ProductOrder
                {
                    OrderId = orderId,
                    ProductId = shopping.ProductId,
                    Quantity = shopping.Quantity

                };
                _productOrder.Table.Add(productOrder);

            }

        }





        public Core.Entities.Order CreateOrder(Guid userId, RegisterOrderDto registerOrderDto)
        {

            var order = new Core.Entities.Order()
            {
                TotalPrice = registerOrderDto.Price,
                CreatedDate = DateTime.Now,
                DateOrder = DateTime.Now,
                DateArrive = DateTime.Now.AddDays(1),
                TotalShipping = 150,
                UserId = userId,
                firstName = registerOrderDto.firstName,
                LastName = registerOrderDto.LastName,
                PhoneNumber = registerOrderDto.PhoneNumber,
                City = registerOrderDto.City,
                State = registerOrderDto.State,
                Country = registerOrderDto.Country,
                IsPaidOnline = registerOrderDto.IsPaidOnline,
                IsPaidOndelivery = registerOrderDto.IsPaidOndelivery
            };
            _order.Table.Add(order);

            return order;
        }
        private IQueryable<Core.Entities.Order> Filter(IQueryable<Core.Entities.Order> orders, PaginationParam paginationParam)
        {
            return orders;

        }

        private IQueryable<Core.Entities.Order> Sort(IQueryable<Core.Entities.Order> orders, PaginationParam paginationParam)
        {
            return orders.OrderByDescending(d => d.CreatedDate);
        }
    }
}