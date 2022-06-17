using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;

namespace Arcus.Shared.Services.Interfaces
{
    public interface IBaconService
    {
        Task<List<string>> GetBaconAsync();
        Task<List<string>> GetBaconAsync(MessageCorrelationInfo messageCorrelationInfo);
    }
}
