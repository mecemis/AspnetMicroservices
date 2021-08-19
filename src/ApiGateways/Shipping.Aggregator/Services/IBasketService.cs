using System.Threading.Tasks;
using Shipping.Aggregator.Models;

namespace Shipping.Aggregator.Services
{
    public interface IBasketService
    {
        Task<BasketModel> GetBasket(string userName);
    }
}