using AutoMapper;
using Integration.Common.Actor.UnifiedActor.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseSqlStoredProcedureAction : BaseAction
    {
        protected string ConnectionString { get; set; }

        protected BaseSqlStoredProcedureAction(string connectionString) : base()
        {
            ConnectionString = connectionString;
        }

        protected SqlConnection GetConnection()
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            if (connection.State != ConnectionState.Open)
                connection.Open();
            return connection;
        }

        protected DbCommand GetCommand(DbConnection connection, string commandText, CommandType commandType)
        {
            SqlCommand command = new SqlCommand(commandText, connection as SqlConnection);
            command.CommandType = commandType;
            return command;
        }

        protected int ExecuteNonQuery(string procedureName, List<DbParameter> parameters, CommandType commandType = CommandType.StoredProcedure)
        {
            int returnValue = -1;

            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    DbCommand cmd = GetCommand(conn, procedureName, commandType);

                    if (parameters != null && parameters.Count > 0)
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                    }

                    returnValue = cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to ExecuteNonQuery for " + procedureName);
            }

            return returnValue;
        }


        protected List<T> GetData<T>(string procedureName, List<DbParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            try
            {
                List<T> list = new List<T>();
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    DbCommand cmd = GetCommand(conn, procedureName, commandType);
                    if (parameters != null && parameters.Count > 0)
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                    }

                    var ds = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (ds.HasRows)
                    {
                        Mapper.AssertConfigurationIsValid();
                        list = Mapper.Map<IDataReader, List<T>>(ds);
                    }


                    return list;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to GetData for " + procedureName);
            }

            return new List<T>();
        }

        protected List<object> GetData(string procedureName, Type typeOfPayload, Type typeOfOutput, List<DbParameter> parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            try
            {
                List<object> listOfData = new List<object>();
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    DbCommand cmd = GetCommand(conn, procedureName, commandType);
                    if (parameters != null && parameters.Count > 0)
                    {
                        cmd.Parameters.AddRange(parameters.ToArray());
                    }

                    var ds = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    if (ds.HasRows)
                    {
                        while (ds.Read())
                        {
                            Mapper.AssertConfigurationIsValid();
                            var item = Mapper.Map(ds, typeOfPayload, typeOfOutput);
                            listOfData.Add(item);
                        }


                    }


                    return listOfData;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to GetData for " + procedureName);
            }

            return new List<object>();
        }


    }
}

