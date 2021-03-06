﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Crane
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseInitialiser
    {
        /// <summary>
        /// 
        /// </summary>
        protected List<SqlParameter> ParamList;

        /// <summary>
        /// 
        /// </summary>
        protected BaseInitialiser()
        {
            ParamList = new List<SqlParameter>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        protected void OpenConn(DbConnection conn)
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        protected async Task OpenConnAsync(DbConnection conn)
        {
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="sqlCommand"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="transaction"></param>
        protected void SetCommandProps(DbCommand command, DbTransaction transaction, int? commandTimeout, string sqlCommand)
        {
            command.CommandType = IsStoredProcedure(sqlCommand) ? CommandType.StoredProcedure : CommandType.Text;

            if (transaction != null)
                command.Transaction = transaction;

            if (commandTimeout.HasValue)
                command.CommandTimeout = commandTimeout.Value;

            if (ParamList != null && ParamList.Any())
                command.Parameters.AddRange(ParamList.ToArray());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public string GetCleanSqlCommand(string sqlCommand)
        {
            if (sqlCommand == null)
                throw new CraneException("SQL command can't be null");

            sqlCommand = sqlCommand.Trim();

            return sqlCommand;
        }

        /// <summary>
        /// Determines if SQL CommandType is Text or StoredProcedure
        /// </summary>
        /// /// <param name="sqlCommand"></param>
        public bool IsStoredProcedure(string sqlCommand)
        {
            // Note: sqlCommand has already been trimmed in a prior process

            if (sqlCommand.StartsWith("EXEC(", StringComparison.OrdinalIgnoreCase))          
                return false;

            if (sqlCommand.StartsWith("EXECUTE(", StringComparison.OrdinalIgnoreCase))
                return false;

            if (sqlCommand.StartsWith("SP_", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!sqlCommand.Contains(" "))           
                return true;
            
            return false;
        }
    }
}
