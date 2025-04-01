using AutoMapper;
using ordermanagement.shared.Dto;
using ordermanagement.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ordermanagement.data;

namespace ordermanagement.core
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OrderResponsedto> CreateOrderAsync(CreateOrderDto orderDto)
        {
            // Stok kontrolü
            foreach (var item in orderDto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product == null || product.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Ürün ID {item.ProductId} için yeterli stok yok veya ürün bulunamadı");
                }
            }

            // Sipariş oluştur
            var order = new Order
            {
                CustomerId = orderDto.CustomerId,
                OrderDate = DateTime.UtcNow,
                Status = "Created",
                TotalAmount = 0 // Hesaplanacak
            };

            await _unitOfWork.Orders.AddAsync(order);

            // Sipariş kalemlerini ekle ve stokları güncelle
            decimal totalAmount = 0;
            foreach (var itemDto in orderDto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId);

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = itemDto.ProductId,
                    ProductName = product.Name,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price
                };

                totalAmount += product.Price * itemDto.Quantity;
                product.StockQuantity -= itemDto.Quantity;

                await _unitOfWork.OrderItems.AddAsync(orderItem);
                _unitOfWork.Products.UpdateAsync(product);
            }

            order.TotalAmount = totalAmount;
            await _unitOfWork.CommitAsync();

            return _mapper.Map<OrderResponsedto>(order);
        }

        public async Task<IEnumerable<OrderResponsedto>> GetOrdersByCustomerAsync(string customerId)
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            var customerOrders = orders.Where(o => o.CustomerId == customerId).ToList();
            return _mapper.Map<IEnumerable<OrderResponsedto>>(customerOrders);
        }

        public async Task<OrderResponsedto> GetOrderDetailsAsync(int orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null) throw new KeyNotFoundException("Sipariş bulunamadı");

            var orderItems = (await _unitOfWork.OrderItems.GetAllAsync())
                .Where(oi => oi.OrderId == orderId).ToList();

            order.OrderItems = orderItems;
            return _mapper.Map<OrderResponsedto>(order);
        }

        public async Task DeleteOrderAsync(int orderId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null) throw new KeyNotFoundException("Sipariş bulunamadı");

            // Sipariş kalemlerini sil ve stokları geri al
            var orderItems = (await _unitOfWork.OrderItems.GetAllAsync())
                .Where(oi => oi.OrderId == orderId).ToList();

            foreach (var item in orderItems)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    _unitOfWork.Products.UpdateAsync(product);
                }

                await _unitOfWork.OrderItems.DeleteAsync(item);
            }

            await _unitOfWork.Orders.DeleteAsync(order);
            await _unitOfWork.CommitAsync();
        }
    }
}
