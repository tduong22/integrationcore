using AutoMapper;
using Integration.Common.Actor.UnifiedActor.Actions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseTransformAction : BaseAction
    {
        protected IMapper Mapper;

        public BaseTransformAction(IMapper mapper)
        {
            Mapper = mapper;
        }

        public virtual TOutputModel Transform<TInputModel, TOutputModel>(TInputModel inputModel) => Mapper.Map<TInputModel, TOutputModel>(inputModel);

        public virtual TOutputModel Transform<TInputModel, TOutputModel>(TInputModel inputModel, IDictionary<string, object> contextItems)
        {
            return Mapper.Map<TInputModel, TOutputModel>(inputModel, opts =>
            {
                foreach (var key in contextItems.Keys)
                {
                    opts.Items.Add(key, contextItems[key]);
                }
            });
        }

        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
