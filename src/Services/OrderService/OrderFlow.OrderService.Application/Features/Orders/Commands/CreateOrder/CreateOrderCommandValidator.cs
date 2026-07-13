using FluentValidation;

namespace OrderFlow.OrderService.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator  : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.TotalPrice)
            .GreaterThan(0);
    }
}
