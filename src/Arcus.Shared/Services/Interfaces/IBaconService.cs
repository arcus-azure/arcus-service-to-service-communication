using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arcus.Shared.Services.Interfaces
{
    public interface IBaconService
    {
        Task<List<string>> GetBaconAsync();
    }
}
