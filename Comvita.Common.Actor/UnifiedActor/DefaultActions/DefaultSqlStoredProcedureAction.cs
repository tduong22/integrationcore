using Comvita.Common.Actor.Models;
using Comvita.Common.Actor.UnifiedActor.Actions;
using Comvita.Common.Actor.UnifiedActor.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.DefaultActions
{
    public class DefaultSqlStoredProcedureAction : BaseSqlStoredProcedureAction, IDefaultSqlStoredProcedureAction
    {
        public const string DISPATCHER_ACTION_NAME = "DISPATCHER_ACTION_NAME";
        

        public override Task<bool> ValidateDataAsync(string actionName, object payload,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(true);
        }

        public Task InvokeStoredProcedure<TPayload>(DispatchRequest dispatchRequest)
        {
           // var sqlOption = (SqlRequest)dispatchRequest.DeserializeRequestData();
           // List<TPayload> listOfData;
           // listOfData = GetData<TPayload>(sqlOption.StoredProcedureName, sqlOption.Parameters, System.Data.CommandType.StoredProcedure);
            
            throw new NotImplementedException("InvokeStoredProcedure<TPayload>(DispatchRequest dispatchRequest is not implemented on DefaultSqlStoredAction");
            //return ChainNextActorsAsync(DefaultNextActorRequestContext, listOfData, typeof(List<TPayload>), CancellationToken.None);
        }

        public DefaultSqlStoredProcedureAction(string connectionString) : base(connectionString)
        {
        }
    }
}
