namespace Integration.Common.Flow
{
    public interface IOrchestrationFlow<TStep> where TStep : IStep
    {
        IFlow<TStep> Flow { get; set; }
        TStep CurrentRefStep { get; set; }
        TStep CurrentStep { get; set; }
        TStep NextStep { get; set; }
    }
}
