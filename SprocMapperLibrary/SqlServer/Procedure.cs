﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SprocMapperLibrary.SqlServer
{
    /// <summary>
    /// 
    /// </summary>
    public class Procedure : AbstractQuery
    {
        private readonly SqlConnection _conn;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        public Procedure(SqlConnection conn)
        {
            _conn = conn;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Procedure AddSqlParameter(SqlParameter item)
        {
            ParamList.Add(item);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Procedure AddSqlParameter(string parameterName, object value)
        {
            if (parameterName == null)
                throw new NullReferenceException(nameof(parameterName));

            ParamList.Add(new SqlParameter() { Value = value, ParameterName = parameterName });
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="value"></param>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public Procedure AddSqlParameter(string parameterName, SqlDbType dbType, object value)
        {
            if (parameterName == null)
                throw new NullReferenceException(nameof(parameterName));

            ParamList.Add(new SqlParameter() { Value = value, ParameterName = parameterName, SqlDbType = dbType });
            return this;
        }

        /// <summary>
        /// Adds a list of SqlParameters to be passed into stored procedure.
        /// </summary>
        /// <returns></returns>
        public Procedure AddSqlParameterCollection(IEnumerable<SqlParameter> sqlParameterCollection)
        {
            if (sqlParameterCollection == null)
                throw new NullReferenceException(nameof(sqlParameterCollection));

            ParamList.AddRange(sqlParameterCollection);
            return this;
        }

        /// <summary>
        /// Execute a MSSql stored procedure synchronously.
        /// </summary>
        /// <param name="storedProcedure"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>Number of affected records.</returns>
        public int ExecuteNonQuery(string storedProcedure, int? commandTimeout = null)
        {
            int affectedRecords;

            OpenConn(_conn);

            using (SqlCommand command = new SqlCommand(storedProcedure, _conn))
            {
                SetCommandProps(command, commandTimeout);
                affectedRecords = command.ExecuteNonQuery();                
            }

            return affectedRecords;
        }

        /// <summary>
        /// Execute a stored procedure asynchronously.
        /// </summary>
        /// <param name="storedProcedure"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>Number of affected records.</returns>
        public async Task<int> ExecuteNonQueryAsync(string storedProcedure, int? commandTimeout = null)
        {
            int affectedRecords;

            await OpenConnAsync(_conn);
            using (SqlCommand command = new SqlCommand(storedProcedure, _conn))
            {
                SetCommandProps(command, commandTimeout);
                affectedRecords = await command.ExecuteNonQueryAsync();
            }

            return affectedRecords;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storedProcedure"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>First column of the first row in the result set.</returns>
        public T ExecuteScalar<T>(string storedProcedure, int? commandTimeout = null)
        {
            T obj;

            OpenConn(_conn);
            using (SqlCommand command = new SqlCommand(storedProcedure, _conn))
            {
                SetCommandProps(command, commandTimeout);
                obj = (T)command.ExecuteScalar();
            }

            return obj;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storedProcedure"></param>
        /// <param name="commandTimeout"></param>
        /// <returns>First column of the first row in the result set.</returns>
        public async Task<T> ExecuteScalarAsync<T>(string storedProcedure, int? commandTimeout = null)
        {
            T obj;

            await OpenConnAsync(_conn);
            using (SqlCommand command = new SqlCommand(storedProcedure, _conn))
            {
                SetCommandProps(command, commandTimeout);
                obj = (T)await command.ExecuteScalarAsync();
            }

            return obj;
        }
    }
}