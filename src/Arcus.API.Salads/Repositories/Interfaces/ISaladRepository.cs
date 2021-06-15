using System.Collections.Generic;
using System.Threading.Tasks;

namespace Arcus.API.Salads.Repositories.Interfaces
{
    public interface ISaladRepository
    {
        Task<List<string>> GetDishRecommendationsAsync();
    }
}
