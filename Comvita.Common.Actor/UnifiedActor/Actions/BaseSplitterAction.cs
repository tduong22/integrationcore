using Comvita.Common.Actor.Models;
using Integration.Common.Actor.UnifiedActor.Actions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    
    public abstract class BaseSplitterAction : BaseAction
    {
        public BaseSplitterAction() : base()
        {

        }
        protected async Task<SplitResult<T>> Split<T>(IEnumerable<T> listOfdata, Func<T, CancellationToken, Task<bool>> funcProcess, CancellationToken cancellationToken)
        {
            var result = new SplitResult<T>();
            foreach (var item in listOfdata)
            {
                if (cancellationToken.IsCancellationRequested) { return result; }
                try
                {
                    var isSucess = await funcProcess.Invoke(item, cancellationToken);
                    if (isSucess) { result.ListOfSuccess.Add(item); }
                    else
                    {
                        result.ListOfFailed.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    result.ListOfException.Add(ex);
                }
            }
            //do something else if needed
            return result;
        }

        protected async Task<SplitResult> Split(IEnumerable<object> listOfdata, Type typeOfPayload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException($"Task<SplitResult> Split(IEnumerable<object> listOfdata, Type typeOfPayload, CancellationToken cancellationToken) is not implemented");
            /*
            var result = new SplitResult();
            foreach (var item in listOfdata)
            {
                if (cancellationToken.IsCancellationRequested) { return result; }
                try
                {
                    await ChainNextActorsAsync(DefaultNextActorRequestContext, item, typeOfPayload, cancellationToken);
                    result.ListOfSuccess.Add(item);
                }
                catch (Exception ex)
                {
                    result.ListOfFailed.Add(item);
                    result.ListOfException.Add(ex);
                }
            }
            //do something else if needed
            return result;*/
        }
    }
}
