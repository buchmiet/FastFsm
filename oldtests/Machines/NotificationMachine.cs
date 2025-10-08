using Abstractions.Attributes;
using FastFsm.Tests.Features.Payload;
using FastFsm.Tests.Payloads;

namespace FastFsm.Tests.Machines;


[StateMachine(typeof(NotificationState), typeof(NotificationTrigger))]
[PayloadType(typeof(NotificationData))]
public partial class NotificationMachine
{
    public string LastSentMessage { get; private set; }
    public int RecipientCount { get; private set; }

    [Transition(NotificationState.Ready, NotificationTrigger.Send, NotificationState.Sent,
        Action = nameof(SendNotification))]
    private void Configure() { }

    private void SendNotification(NotificationData notification)
    {
        LastSentMessage = notification.Message;
        RecipientCount = notification.Recipients.Length;
    }
}