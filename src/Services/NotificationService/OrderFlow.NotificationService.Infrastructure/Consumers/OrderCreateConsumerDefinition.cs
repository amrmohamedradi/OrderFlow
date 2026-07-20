using MassTransit;
using OrderFlow.NotificationService.Infrastructure.Persistence;

namespace OrderFlow.NotificationService.Infrastructure.Consumers;

public class OrderCreateConsumerDefinition : ConsumerDefinition<OrderFlow.NotificationService.Application.Consumers.OrderCreateConsumer>
{
    public OrderCreateConsumerDefinition()
    {
        EndpointName = "notification-order-created";
    }

    protected override void ConfigureConsumer(
    IReceiveEndpointConfigurator endpointConfigurator,
    IConsumerConfigurator<OrderFlow.NotificationService.Application.Consumers.OrderCreateConsumer> consumerConfigurator,
    IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
            r.Exponential(
                retryLimit: 3,
                minInterval: TimeSpan.FromSeconds(5),
                maxInterval: TimeSpan.FromSeconds(20),
                intervalDelta: TimeSpan.FromSeconds(5)));

        endpointConfigurator
            .UseEntityFrameworkOutbox<NotificationDbContext>(context);
    }
}
