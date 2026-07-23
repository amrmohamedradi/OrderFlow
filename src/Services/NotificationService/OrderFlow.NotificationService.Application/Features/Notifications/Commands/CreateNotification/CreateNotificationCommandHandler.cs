using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.NotificationService.Application.Interfaces.Persistence;
using OrderFlow.NotificationService.Application.Interfaces.Repositories;
using OrderFlow.NotificationService.Application.Interfaces.Services;
using OrderFlow.NotificationService.Domain.Entities;

namespace OrderFlow.NotificationService.Application.Features.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, CreateNotificationResponse>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    private readonly IEmailService _emailService;

    public CreateNotificationCommandHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateNotificationCommandHandler> logger,
        IEmailService emailService)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<CreateNotificationResponse> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling CreateNotificationCommand for OrderId: {OrderId}, Email: {Email}",
            request.OrderId, request.Email);

        var notification = Notification.Create(request.OrderId, request.Email);
        await _notificationRepository.AddAsync(notification, cancellationToken);

                        using var emailCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await _emailService.SendAsync(
                notification.Email,
                "Order Created",
                $"Order {notification.OrderId} has been created.",
                emailCts.Token);

            notification.MarkAsSent();
            _logger.LogInformation(
                "Notification sent successfully. NotificationId: {NotificationId}, Status: {Status}",
                notification.Id, notification.Status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to send email for OrderId: {OrderId}. Notification stored with Failed status.",
                notification.OrderId);
            notification.MarkAsFailed();
        }

                        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification persisted. NotificationId: {NotificationId}, Status: {Status}",
            notification.Id, notification.Status);

        return new CreateNotificationResponse(notification.Id, notification.Email, notification.Status.ToString(), notification.CreatedAt, notification.SentAt);
    }
}
