using Xunit;

namespace OrderFlow.OrderService.IntegrationTests.Infrastructure;

/// <summary>
/// Collection definition that ensures the <see cref="OrderServiceWebAppFactory"/> (with its
/// Testcontainer SQL Server) is shared across all integration test classes, preventing
/// multiple containers from starting simultaneously.
/// </summary>
[CollectionDefinition("OrderService Integration")]
public class OrderServiceIntegrationCollection : ICollectionFixture<OrderServiceWebAppFactory>
{
    // This class has no code — it is just a collection marker.
}
