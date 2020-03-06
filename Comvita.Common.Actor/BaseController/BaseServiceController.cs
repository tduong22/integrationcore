using Comvita.Common.Actor.Interfaces;
using Integration.Common.Flow;
using Integration.Common.Interface;
using Integration.Common.Model;
using MessagePack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System;
using System.Diagnostics;
using System.Fabric;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.BaseController
{
    public class BaseServiceController : ControllerBase
    {
        protected ILogger Logger;
        protected IBinaryMessageSerializer BinaryMessageSerializer;
        protected StatelessServiceContext Context;
        protected IAsyncOrchestrationFlow<Step> AsyncActorFlow;

        protected ActorRequestContext DefaultNextActorRequestContextWithActionName(string actionName, string flowName = null) => new ActorRequestContext(Guid.NewGuid().ToString(), actionName, Guid.NewGuid().ToString(), new FlowInstanceId(Activity.Current.Id, flowName ?? (this.GetType().Name)));
        public BaseServiceController(StatelessServiceContext context, IBinaryMessageSerializer binaryMessageSerializer, IAsyncOrchestrationFlow<Step> asyncActorFlow, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger(this.GetType());
            BinaryMessageSerializer = binaryMessageSerializer;
            AsyncActorFlow = asyncActorFlow;
            Context = context;
            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }

        protected async Task ChainNextActorAsync(ActorRequestContext nextActorRequestContext, string startedPayload, object nextPayload, Type typeOfPayload, ExecutableOrchestrationOrder order, CancellationToken cancellationToken)
        {
            var flowInstanceId = nextActorRequestContext.FlowInstanceId;

           // var startedFlowInstanceId = new FlowInstanceId(Activity.Current.Id, (this.GetType().Name));
            var applicationName = Context.CodePackageActivationContext.ApplicationName;

            await TryCreateFlow(applicationName, flowInstanceId, startedPayload);

            var uri = new Uri(order.ActorServiceUri);

            //var startedFlowInstanceId = new FlowInstanceId(Activity.Current.Id, (this.GetType().Name));
            var proxy = ActorProxy.Create<IBaseMessagingActor>(order.ActorId == null ? ActorId.CreateRandom() : new ActorId(order.ActorId), uri);

            var payload = BinaryMessageSerializer.SerializePayload(nextPayload);
            await proxy.ChainProcessMessageAsync(nextActorRequestContext, payload, cancellationToken);
        }

        protected async Task TryCreateFlow(string applicationName, FlowInstanceId flowInstanceId, string startedPayload)
        {
            try
            {
                var startRequest = new StartFlowRequest()
                {
                    FlowInstanceId = flowInstanceId,
                    StartDate = DateTime.UtcNow,
                    UserStarted = $"{this.GetType().Name} - {flowInstanceId.Id}",
                    StartedPayload = startedPayload
                };
                await AsyncActorFlow.CreateFlowAsync(new ActorIdentity(Guid.NewGuid().ToString(), applicationName), flowInstanceId, startRequest, CancellationToken.None);

            }
            catch (Exception ex)
            {
                Logger.LogInformation(ex, $"Failed to create flow to flow service");
            }
        }

        protected T XmlDeserializeInput<T>(string xml)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer =
                new System.Xml.Serialization.XmlSerializer(typeof(T));

            StringReader reader = new StringReader(xml);

            var result = (T)xmlSerializer.Deserialize(reader);
            return result;
        }
    }
}
