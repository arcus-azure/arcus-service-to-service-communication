using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arcus.API.Bacon.Repositories.Interfaces
{
    public interface IBaconRepository
    {
        Task<List<string>> GetFlavorsAsync();
    }
}
