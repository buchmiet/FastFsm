using Tests.Machines.Payloads;
using Abstractions.Attributes;

namespace Tests.Machines.Machines.Legacy;


[StateMachine(typeof(NotificationState), typeof(NotificationTrigger))]
[PayloadType(typeof(NotificationData))]
public partial class NotificationMachine
{
    public string LastSentMessage { get; private set; } = null!;
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
