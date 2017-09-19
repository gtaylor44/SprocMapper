﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using SprocMapperLibrary.CacheProvider;

namespace SprocMapperLibrary.SqlServer
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public class SqlServerSproc : BaseSproc
    {
        private SqlConnection _conn;
        private readonly string _connectionString;
        private readonly SqlCredential _credential;

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="credential"></param>
        /// <param name="cacheProvider"></param>
        public SqlServerSproc(string connectionString, SqlCredential credential,
            AbstractCacheProvider cacheProvider) : base(cacheProvider)
        {
            _connectionString = connectionString;
            _credential = credential;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="getObjectDel"></param>
        /// <param name="command"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="userConn"></param>
        /// <param name="cacheKey"></param>
        /// <param name="saveCacheDel"></param>
        /// <param name="commandType"></param>
        protected override IEnumerable<dynamic> ExecuteDynamicReaderImpl(Action<dynamic, List<dynamic>> getObjectDel,
            string command, int? commandTimeout, DbConnection userConn, string cacheKey, Action saveCacheDel, 
            CommandType? commandType)
        {
            var userProvidedConnection = false;
            try
            {
                userProvidedConnection = userConn != null;

                // Try open connection if not already open.
                if (!userProvidedConnection)
                    _conn = _credential == null ? new SqlConnection(_connectionString)
                        : new SqlConnection(_connectionString, _credential);

                else
                    _conn = userConn as SqlConnection;

                OpenConn(_conn);

                var result = new List<dynamic>();

                using (var cmd = new SqlCommand(command, _conn))
                {
                    // Set common SqlCommand properties
                    SetCommandProps(cmd, commandTimeout, commandType);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return new List<dynamic>();

                        var schema = reader.GetSchemaTable();

                        var dynamicColumnDic = SprocMapper.GetColumnsForDynamicQuery(schema);

                        while (reader.Read())
                        {
                            dynamic expando = new ExpandoObject();

                            foreach (var col in dynamicColumnDic)                                                          
                                ((IDictionary<String, object>)expando)[col.Value] = reader[col.Key];
                            
                            getObjectDel(expando, result);
                        }
                    }
                }

                if (cacheKey != null)
                    saveCacheDel();

                return result;
            }

            finally
            {
                if (!userProvidedConnection)
                    _conn.Dispose();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="getObjectDel"></param>
        /// <param name="command"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="userConn"></param>
        /// <param name="cacheKey"></param>
        /// <param name="saveCacheDel"></param>
        /// <param name="commandType"></param>
        protected override async Task<IEnumerable<dynamic>> ExecuteDynamicReaderImplAsync(Action<dynamic, List<dynamic>> getObjectDel,
            string command, int? commandTimeout, DbConnection userConn, string cacheKey, Action saveCacheDel, CommandType? commandType)
        {
            var userProvidedConnection = false;
            try
            {
                userProvidedConnection = userConn != null;

                // Try open connection if not already open.
                if (!userProvidedConnection)
                    _conn = _credential == null ? new SqlConnection(_connectionString)
                        : new SqlConnection(_connectionString, _credential);

                else
                    _conn = userConn as SqlConnection;

                await OpenConnAsync(_conn);

                List<dynamic> result = new List<dynamic>();

                using (SqlCommand cmd = new SqlCommand(command, _conn))
                {
                    // Set common SqlCommand properties
                    SetCommandProps(cmd, commandTimeout, commandType);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                            return new List<dynamic>();

                        var schema = reader.GetSchemaTable();
                        var dynamicColumnDic = SprocMapper.GetColumnsForDynamicQuery(schema);

                        while (await reader.ReadAsync())
                        {
                            dynamic expando = new ExpandoObject();

                            foreach (var col in dynamicColumnDic)
                                ((IDictionary<String, object>)expando)[col.Value] = reader[col.Key];

                            getObjectDel(expando, result);
                        }
                    }
                }

                if (cacheKey != null)
                    saveCacheDel();

                return result;
            }

            finally
            {
                if (!userProvidedConnection)
                    _conn.Dispose();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Performs synchronous version of stored procedure.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="getObjectDel"></param>
        /// <param name="command">The name of your stored procedure (with schema name if applicable).</param>
        /// <param name="commandTimeout"></param>
        /// <param name="partitionOnArr"></param>
        /// <param name="validateSelectColumns"></param>
        /// <param name="userConn"></param>
        /// <param name="cacheKey"></param>
        /// <param name="saveCacheDel"></param>
        /// <param name="commandType"></param>
        /// <param name="valueOrStringType"></param>
        /// <returns></returns>
        protected override IEnumerable<TResult> ExecuteReaderImpl<TResult>(Action<DbDataReader, List<TResult>> getObjectDel,
            string command, int? commandTimeout, string[] partitionOnArr, bool validateSelectColumns, DbConnection userConn,
            string cacheKey, Action saveCacheDel, CommandType? commandType, bool valueOrStringType = false)
        {
            var userProvidedConnection = false;
            try
            {
                userProvidedConnection = userConn != null;

                // Try open connection if not already open.
                if (!userProvidedConnection)
                    _conn = _credential == null ? new SqlConnection(_connectionString)
                        : new SqlConnection(_connectionString, _credential);

                else
                    _conn = userConn as SqlConnection;

                OpenConn(_conn);

                var result = new List<TResult>();
                using (SqlCommand cmd = new SqlCommand(command, _conn))
                {
                    // Set common SqlCommand properties
                    SetCommandProps(cmd, commandTimeout, commandType);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return new List<TResult>();

                        if (!valueOrStringType)
                        {
                            DataTable schema = reader.GetSchemaTable();
                            var rowList = schema?.Rows.Cast<DataRow>().ToList();

                            int[] partitionOnOrdinal = null;

                            if (partitionOnArr != null)
                                partitionOnOrdinal =
                                    SprocMapper.GetOrdinalPartition(rowList, partitionOnArr, SprocObjectMapList.Count);

                            SprocMapper.SetOrdinal(rowList, SprocObjectMapList, partitionOnOrdinal);

                            if (validateSelectColumns)
                                SprocMapper.ValidateSelectColumns(rowList, SprocObjectMapList, partitionOnOrdinal,
                                    command);

                            SprocMapper.ValidateSchema(schema, SprocObjectMapList, partitionOnOrdinal);
                        }

                        while (reader.Read())
                        {
                            getObjectDel(reader, result);
                        }
                    }
                }

                if (cacheKey != null)
                    saveCacheDel();

                return result;
            }

            finally
            {
                if (!userProvidedConnection)
                    _conn.Dispose();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Performs asynchronous version of stored procedure.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="getObjectDel"></param>
        /// <param name="command"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="partitionOnArr"></param>
        /// <param name="validateSelectColumns"></param>
        /// <param name="userConn"></param>
        /// <param name="cacheKey"></param>
        /// <param name="saveCacheDel"></param>
        /// <param name="commandType"></param>
        /// <param name="valueOrStringType"></param>
        /// <returns></returns>
        protected override async Task<IEnumerable<TResult>> ExecuteReaderAsyncImpl<TResult>(Action<DbDataReader, List<TResult>> getObjectDel,
            string command, int? commandTimeout, string[] partitionOnArr, bool validateSelectColumns, DbConnection userConn,
            string cacheKey, Action saveCacheDel, CommandType? commandType, bool valueOrStringType = false)
        {
            var userProvidedConnection = false;
            try
            {
                userProvidedConnection = userConn != null;

                // Try open connection if not already open.
                if (!userProvidedConnection)
                    _conn = _credential == null ? new SqlConnection(_connectionString)
                        : new SqlConnection(_connectionString, _credential);

                else
                    _conn = userConn as SqlConnection;

                await OpenConnAsync(_conn);

                var result = new List<TResult>();

                using (var cmd = new SqlCommand(command, _conn))
                {
                    // Set common SqlCommand properties
                    SetCommandProps(cmd, commandTimeout, commandType);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                            return new List<TResult>();

                        if (!valueOrStringType)
                        {
                            DataTable schema = reader.GetSchemaTable();
                            var rowList = schema?.Rows.Cast<DataRow>().ToList();

                            int[] partitionOnOrdinal = null;

                            if (partitionOnArr != null)
                                partitionOnOrdinal =
                                    SprocMapper.GetOrdinalPartition(rowList, partitionOnArr, SprocObjectMapList.Count);

                            SprocMapper.SetOrdinal(rowList, SprocObjectMapList, partitionOnOrdinal);

                            if (validateSelectColumns)
                                SprocMapper.ValidateSelectColumns(rowList, SprocObjectMapList, partitionOnOrdinal,
                                    command);

                            SprocMapper.ValidateSchema(schema, SprocObjectMapList, partitionOnOrdinal);
                        }

                        while (await reader.ReadAsync())
                        {
                            getObjectDel(reader, result);
                        }
                    }
                }

                if (cacheKey != null)
                    saveCacheDel();

                return result;
            }

            finally
            {
                if (!userProvidedConnection)
                    _conn.Dispose();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Execute a MSSql stored procedure synchronously.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="commandType"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="userConn"></param>
        /// <returns>Number of affected records.</returns>
        public override int ExecuteNonQuery(string command, CommandType? commandType = null, int? commandTimeout = null, DbConnection userConn = null)
        {
            try
            {
                int affectedRecords;

                if (userConn == null)
                    _conn = _credential == null ? new SqlConnection(_connectionString)
                        : new SqlConnection(_connectionString, _credential);

                else
                    _conn = userConn as SqlConnection;

                OpenConn(_conn);

                using (SqlCommand cmd = new SqlCommand(command, _conn))
                {
                    SetCommandProps(cmd, commandTimeout, commandType);
                    affectedRecords = cmd.ExecuteNonQuery();
                }

                return affectedRecords;
            }
            finally
            {
                if (userConn == null)
                    _conn.Dispose();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Execute a stored procedure asynchronously.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="commandType"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="userConn"></param>
        /// <returns>Number of affected records.</returns>
        public override async Task<int> ExecuteNonQueryAsync(string command, CommandType? commandType = null, int? commandTimeout = null, DbConnection userConn = null)
        {
            try
            {
                int affectedRecords;

                if (userConn == null)
                    _conn = _credential == null ? new SqlConnection(_connectionString)
                        : new SqlConnection(_connectionString, _credential);

                else
                    _conn = userConn as SqlConnection;

                await OpenConnAsync(_conn);

                using (SqlCommand cmd = new SqlCommand(command, _conn))
                {
                    SetCommandProps(cmd, commandTimeout, commandType);
                    affectedRecords = await cmd.ExecuteNonQueryAsync();
                }

                return affectedRecords;
            }
            finally
            {
                if (userConn == null)
                    _conn.Dispose();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="commandType"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="userConn"></param>
        /// <returns>First column of the first row in the result set.</returns>
        public override T ExecuteScalar<T>(string command, CommandType? commandType = null, int? commandTimeout = null, DbConnection userConn = null)
        {
            try
            {
                T obj;

                if (userConn == null)
                    _conn = _credential == null ? new SqlConnection(_connectionString)
                        : new SqlConnection(_connectionString, _credential);

                else
                    _conn = userConn as SqlConnection;

                OpenConn(_conn);

                using (SqlCommand cmd = new SqlCommand(command, _conn))
                {
                    SetCommandProps(cmd, commandTimeout, commandType);
                    obj = (T)cmd.ExecuteScalar();
                }

                return obj;
            }

            finally
            {
                if (userConn == null)
                    _conn.Dispose();
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="commandType"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="userConn"></param>
        /// <returns>First column of the first row in the result set.</returns>
        public override async Task<T> ExecuteScalarAsync<T>(string command, CommandType? commandType = null, int? commandTimeout = null, DbConnection userConn = null)
        {
            try
            {
                T obj;

                if (userConn == null)
                    _conn = _credential == null ? new SqlConnection(_connectionString)
                        : new SqlConnection(_connectionString, _credential);

                else
                    _conn = userConn as SqlConnection;

                await OpenConnAsync(_conn);

                using (SqlCommand cmd = new SqlCommand(command, _conn))
                {
                    SetCommandProps(cmd, commandTimeout, commandType);
                    obj = (T)await cmd.ExecuteScalarAsync();
                }

                return obj;
            }

            finally
            {
                if (userConn == null)
                    _conn.Dispose();
            }

        }
    }
}
