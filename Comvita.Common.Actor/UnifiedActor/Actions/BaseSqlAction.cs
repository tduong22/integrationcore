using Comvita.Common.Actor.Models;
using Dapper;
using Integration.Common.Actor.UnifiedActor.Actions;
using Integration.Common.Utility.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseSqlAction : BaseAction
    {

        protected readonly IDbConnection _dbConnection;
        private readonly IDictionary<string, string> databaseConfig;
        private readonly int[] _sqlExceptions = new[] { 53, -2 };
        private const int RetryCount = 3;
        private const int WaitBetweenRetriesInSeconds = 15;
        private readonly AsyncRetryPolicy _retryPolicy;

        protected BaseSqlAction(IServiceConfiguration serviceConfiguration, IDbConnection dbConnection)
            : base()
        {
            _retryPolicy = Policy.Handle<SqlException>(exception => _sqlExceptions.Contains(exception.Number))
            .WaitAndRetryAsync(
                retryCount: RetryCount,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(WaitBetweenRetriesInSeconds)
            );

            _dbConnection = dbConnection;
        }

        private async Task OpenConnectionAsync()
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }
            });
        }

        protected async Task<bool> ExecuteQueryAsync(string query, object param = null)
        {
            try
            {
                await OpenConnectionAsync();

                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _dbConnection.ExecuteAsync(query, param);
                });

                _dbConnection.Close();
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"{CurrentActor} failed to execute query {query}. Message: {e.Message}");
                throw;
            }
        }

        protected async Task<bool> ExecuteScalarAsync(string query, DynamicParameters param = null)
        {
            try
            {
                await OpenConnectionAsync();

                await _retryPolicy.ExecuteAsync(async () =>
                {
                    await _dbConnection.ExecuteScalarAsync(query, param, commandType: CommandType.Text);
                });

                _dbConnection.Close();
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"{CurrentActor} failed to execute query {query}. Message: {e.Message}");
                throw;
            }
        }

        protected async Task<bool> ExecuteQueryAsync(List<string> queries)
        {
            bool isSuccess = false;

            try
            {
                await OpenConnectionAsync();

                using (var transaction = _dbConnection.BeginTransaction())
                {
                    try
                    {
                        foreach (var query in queries)
                        {
                            await _dbConnection.ExecuteAsync(query, transaction: transaction);
                        }
                        transaction.Commit();
                        isSuccess = true;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, $"{CurrentActor} failed to execute queries {queries}");
                        transaction.Rollback();
                        isSuccess = false;
                    }
                    finally
                    {
                        _dbConnection.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"{CurrentActor} failed to open connection. Message: {e.Message}");
                throw;
            }

            return isSuccess;
        }

        protected async Task<bool> ExecuteScalarAsync(List<SqlDapperRequest> sqlRequests)
        {
            bool isSuccess = false;

            try
            {
                await OpenConnectionAsync();

                using (var transaction = _dbConnection.BeginTransaction())
                {
                    try
                    {
                        foreach (var request in sqlRequests)
                        {
                            await _dbConnection.ExecuteScalarAsync(request.QueryString, request.Params, commandType: CommandType.Text, transaction: transaction);
                        }
                        transaction.Commit();
                        isSuccess = true;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, $"{CurrentActor} failed to execute queries. Message: {e.Message}");
                        transaction.Rollback();
                        isSuccess = false;
                    }
                    finally
                    {
                        _dbConnection.Close();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"{CurrentActor} failed to open connection. Message: {e.Message}");
                throw;
            }

            return isSuccess;
        }
    }
}
