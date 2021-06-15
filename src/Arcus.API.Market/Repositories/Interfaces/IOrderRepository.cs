using System.Threading.Tasks;

namespace Arcus.API.Market.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task OrderBaconAsync(int amount);
    }
}
