using Arcus.Observability.Correlation;
using Arcus.Shared.Logging.Correlation;

namespace Arcus.Shared.Logging.Correlation
{
    /// <summary>
    ///  Options for handling correlation ID on incoming HTTP requests.
    /// </summary>
    public class CustomHttpCorrelationInfoOptions : CorrelationInfoOptions
    {
        /// <summary>
        /// Gets the correlation options specific for the upstream service.
        /// </summary>
        public CorrelationInfoUpstreamServiceOptions UpstreamService { get; } = new CorrelationInfoUpstreamServiceOptions();
    }
}
