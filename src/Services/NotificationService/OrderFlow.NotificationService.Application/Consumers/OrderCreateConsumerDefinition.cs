using MassTransit;

namespace OrderFlow.NotificationService.Application.Consumers;

public class OrderCreateConsumerDefinition : ConsumerDefinition<OrderCreateConsumer>
{
    public OrderCreateConsumerDefinition()
    {
        EndpointName = "notification-order-created";
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderCreateConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r =>
            r.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromSeconds(5),
                maxInterval: TimeSpan.FromSeconds(20),
                intervalDelta: TimeSpan.FromSeconds(5)));
                base.ConfigureConsumer(endpointConfigurator,consumerConfigurator);
    }
}
