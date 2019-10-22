using Integration.Common.Model;

namespace Integration.Common.Actor.Model
{
    /// <summary>
    /// For Sending back result to parent if needed
    /// </summary>
    public class MessageObjectResult
    {
        //no data return needed
        public static MessageObjectResult None;
        public ActorIdentity SenderActorId { get; set; }
        public ActorIdentity ParentActorId { get; set; }
        public string Type { get; set; }
        //using deserializer to get back the result if neededed
        public string Result { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string ResultMessage { get; set; }
        public static MessageObjectResult CreateEmpty()
        {
            return new MessageObjectResult();
        }
    }
}