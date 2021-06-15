# Arcus - Service-to-Service Correlation POC

POC to have end-to-end correlation stitching operations across services together in the Azure Application Insights Application Map.

![Arcus](https://raw.githubusercontent.com/arcus-azure/arcus/master/media/arcus.png)

## Scope

Provide end-to-end correlation stitching operations across services together in the Azure Application Insights Application Map.

Depending on progress, we will include a message broker in the middle to see if we can plot it correctly on the Application Map as well.

This POC will fully rely on Azure Application Insight's `TelemetryClient` to easily set it up correctly and see how we can port this to Arcus Observability & Serilog.

### Official Telemetry Correlation Guidance

As per [the guidance](https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation#data-model-for-telemetry-correlation):

> Application Insights defines a [data model](../../azure-monitor/app/data-model.md) for distributed telemetry correlation. To associate telemetry with a logical operation, every telemetry item has a context field called `operation_Id`. This identifier is shared by every telemetry item in the distributed trace. So even if you lose telemetry from a single layer, you can still associate telemetry reported by other components.
> 
> A distributed logical operation typically consists of a set of smaller operations that are requests processed by one of the components. These operations are defined by [request telemetry](../../azure-monitor/app/data-model-request-telemetry.md). Every request telemetry item has its own `id` that identifies it uniquely and globally. And all telemetry items (such as traces and exceptions) that are associated with the request should set the `operation_parentId` to the value of the request `id`.
> 
> Every outgoing operation, such as an HTTP call to another component, is represented by [dependency telemetry](../../azure-monitor/app/data-model-dependency-telemetry.md). Dependency telemetry also defines its own `id` that's globally unique. Request telemetry, initiated by this dependency call, uses this `id` as its `operation_parentId`.
> 
> You can build a view of the distributed logical operation by using `operation_Id`, `operation_parentId`, and `request.id` with `dependency.id`. These fields also define the causality order of telemetry calls.

This means that we are handling the operation ID (aka `operation_Id`) correctly today, but we need to:

- Provide tracking of parent IDs for operations (aka `operation_ParentId`)
- Keep track of the unique IDs for request telemetry items, to use as parent ID for other telemetry
- Keep track of the unique IDs for dependency telemetry items, to use as parent ID for other telemetry

Learn more in [this example](https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation#example).

### What is not included

- Integrate Azure API Management with our Arcus Observability (see [arcus-azure/arcus-api-gateway-poc](https://github.com/arcus-azure/arcus-api-gateway-poc) instead)

## Getting Started

Before you can run this, you need to:

1. Provision an Azure API Management instance with a self-hosted gateway
2. Configure the gateway in Docker Compose
3. Create a Bacon API based on the OpenAPI spec of our local API ([url](http://localhost:789/api/docs/index.html))
4. Make Bacon API available locally
5. Run solution with Docker Compose
6. Get bacon by calling the self-hosted gateway - GET http://localhost:700/api/v1/bacon

## Observability

End-to-end correlation across component will be shown here.

## Action items

None at the moment.

_Some of the action items can be easily found by searching for `TODO: Contribute Upstream` or using the Task List._

## Clarification Required

None at the moment.
