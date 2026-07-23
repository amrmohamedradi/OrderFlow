using Xunit;

namespace OrderFlow.OrderService.IntegrationTests.Infrastructure;

[CollectionDefinition("OrderService Integration")]
public class OrderServiceIntegrationCollection : ICollectionFixture<OrderServiceWebAppFactory>
{
    }
