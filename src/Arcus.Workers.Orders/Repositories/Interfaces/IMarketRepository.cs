using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arcus.Workers.Orders.Repositories.Interfaces
{
    public interface IMarketRepository
    {
        Task<List<string>> OrderBaconAsync(int amount);
    }
}
