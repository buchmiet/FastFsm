using Machines.Tests.Payloads;

namespace Machines.Tests.Machines.Legacy;


[StateMachine(typeof(NotificationState), typeof(NotificationTrigger))]
[PayloadType(typeof(NotificationData))]
public partial class NotificationMachine
{
    public string LastSentMessage { get; private set; }
    public int RecipientCount { get; private set; }

    [Transition(NotificationState.Ready, NotificationTrigger.Send, NotificationState.Sent,
        Action = (SendNotification))]
    private void Configure() { }

    private void SendNotification(NotificationData notification)
    {
        LastSentMessage = notification.Message;
        RecipientCount = notification.Recipients.Length;
    }
}