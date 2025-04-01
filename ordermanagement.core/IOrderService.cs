using ordermanagement.shared.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ordermanagement.core
{
    public interface IOrderService
    {
        Task<OrderResponsedto> CreateOrderAsync(CreateOrderDto orderDto);
        Task<IEnumerable<OrderResponsedto>> GetOrdersByCustomerAsync(string customerId);
        Task<OrderResponsedto> GetOrderDetailsAsync(int orderId);
        Task DeleteOrderAsync(int orderId);
    }
}
