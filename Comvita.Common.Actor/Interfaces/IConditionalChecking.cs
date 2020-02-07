using System;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.Interfaces
{
    public interface IConditionalChecking
    {
        string GetName();
        Task InitializeAsync();
        Task<bool> DoCheckAsync();
    }

    public abstract class BaseConditionalChecking : IConditionalChecking
    {
       public string ConditionName;

       public abstract string FailedMessage {get;}

       public abstract Task<bool> DoCheckAsync();

        public string GetName() => ConditionName;

        public abstract Task InitializeAsync();
    }
    
    public class TimeOutConditionalChecking : BaseConditionalChecking
    {
        private TimeSpan timeElapsed;

        public override string  FailedMessage => $"Condition has failed because it exceeded the time required ({timeElapsed.TotalMinutes}) to be completed.";

        public TimeOutConditionalChecking(TimeSpan timeElapsed)
        {
            TimeElapsed = timeElapsed;
            ConditionName = nameof(TimeOutConditionalChecking);
        }

        public TimeSpan TimeElapsed { get => timeElapsed; set => timeElapsed = value; }

        public override Task<bool> DoCheckAsync()
        {
           return Task.FromResult(true);
        }

        public override Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    } 
}
