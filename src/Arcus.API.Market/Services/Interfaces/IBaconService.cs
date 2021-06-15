using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arcus.API.Market.Services.Interfaces
{
    public interface IBaconService
    {
        Task<List<string>> GetBaconAsync();
    }
}
