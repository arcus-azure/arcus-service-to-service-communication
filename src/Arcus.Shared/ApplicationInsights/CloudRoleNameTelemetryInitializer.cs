using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Arcus.Shared.ApplicationInsights
{
    public class CloudRoleNameTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string _componentName;

        public CloudRoleNameTelemetryInitializer(string componentName)
        {
            _componentName = componentName;
        }

        public static CloudRoleNameTelemetryInitializer CreateForComponent(string componentName)
        {
            return new CloudRoleNameTelemetryInitializer(componentName);
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = _componentName;
        }
    }
}
