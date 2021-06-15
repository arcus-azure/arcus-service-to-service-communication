using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arcus.API.Market.Repositories.Interfaces
{
    public interface IMarketRepository
    {
        Task<List<string>> OrderBaconAsync(int amount);
    }
}
