using System.Collections.Generic;
using System.Threading.Tasks;
using Shipping.Aggregator.Models;

namespace Shipping.Aggregator.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderResponseModel>> GetOrdersByUserName(string userName);
    }
}