using System;
using System.Threading.Tasks;

namespace Integration.Common.Interface
{
    public interface IRemindableAction
    {
        Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period);
    }
}
