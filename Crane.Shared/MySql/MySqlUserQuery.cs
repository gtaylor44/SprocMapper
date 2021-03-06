﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Crane.CacheProvider;
using MySql.Data.MySqlClient;

namespace Crane.MySql
{
    /// <inheritdoc />
    public class MySqlUserQuery : BaseQuery
    {
        private MySqlConnection _mySqlConn;
        private readonly string _connectionString;

        /// <inheritdoc />
        public MySqlUserQuery(string connectionString, QueryOptions queryOptions) : base(queryOptions)
        {
            _connectionString = connectionString;
        }

        /// <inheritdoc />
        protected override IEnumerable<TResult> ExecuteReaderImpl<TResult>(
            Action<DbDataReader, List<TResult>> getObjectDel,
            string query, int? commandTimeout, string[] partitionOnArr, bool validateSelectColumns,
            DbConnection dbConnection, DbTransaction transaction,
            string cacheKey, Action saveCacheDel, bool valueOrStringType = false)
        {
            var userProvidedConnection = false;

            try
            {
                query = GetCleanSqlCommand(query);

                userProvidedConnection = dbConnection != null;

                // Try open connection if not already open.
                if (!userProvidedConnection)
                    _mySqlConn = new MySqlConnection(_connectionString);

                else
                    _mySqlConn = dbConnection as MySqlConnection;

                OpenConn(_mySqlConn);

                List<TResult> result = new List<TResult>();
                using (MySqlCommand command = new MySqlCommand(query, _mySqlConn))
                {
                    // Set common SqlCommand properties
                    SetCommandProps(command, transaction, commandTimeout, query);

                    using (var reader = command.ExecuteReader())
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
                                    CraneHelper.GetOrdinalPartition(rowList, partitionOnArr, SprocObjectMapList.Count);

                            CraneHelper.SetOrdinal(rowList, SprocObjectMapList, partitionOnOrdinal);

                            if (validateSelectColumns)
                                CraneHelper.ValidateSelectColumns(rowList, SprocObjectMapList, partitionOnOrdinal,
                                    query);

                            CraneHelper.ValidateSchema(schema, SprocObjectMapList, partitionOnOrdinal);
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
                    _mySqlConn.Dispose();
            }
        }

        /// <inheritdoc />
        protected override IEnumerable<dynamic> ExecuteDynamicReaderImpl(Action<dynamic, List<dynamic>> getObjectDel,
            string query, int? commandTimeout, DbConnection userConn, DbTransaction transaction, string cacheKey, Action saveCacheDel)
        {
            var userProvidedConnection = false;
            try
            {
                query = GetCleanSqlCommand(query);

                userProvidedConnection = userConn != null;

                // Try open connection if not already open.
                if (!userProvidedConnection)
                    _mySqlConn = new MySqlConnection(_connectionString);

                else
                    _mySqlConn = userConn as MySqlConnection;

                OpenConn(_mySqlConn);

                var result = new List<dynamic>();

                using (var command = new MySqlCommand(query, _mySqlConn))
                {
                    // Set common SqlCommand properties
                    SetCommandProps(command, transaction, commandTimeout, query);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return new List<dynamic>();

                        DataTable schema = reader.GetSchemaTable();

                        var dynamicColumnDic = CraneHelper.GetColumnsForDynamicQuery(schema);

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
                    _mySqlConn.Dispose();
            }
        }

        /// <inheritdoc />
        protected override async Task<IEnumerable<dynamic>> ExecuteDynamicReaderImplAsync(
            Action<dynamic, List<dynamic>> getObjectDel,
            string query, int? commandTimeout, DbConnection userConn, DbTransaction transaction, string cacheKey, Action saveCacheDel)
        {
            var userProvidedConnection = false;
            try
            {
                query = GetCleanSqlCommand(query);

                userProvidedConnection = userConn != null;

                // Try open connection if not already open.
                if (!userProvidedConnection)
                    _mySqlConn = new MySqlConnection(_connectionString);

                else
                    _mySqlConn = userConn as MySqlConnection;

                await OpenConnAsync(_mySqlConn);

                var result = new List<dynamic>();

                using (var command = new MySqlCommand(query, _mySqlConn))
                {
                    // Set common SqlCommand properties
                    SetCommandProps(command, transaction, commandTimeout, query);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                            return new List<dynamic>();

                        var schema = reader.GetSchemaTable();

                        var dynamicColumnDic = CraneHelper.GetColumnsForDynamicQuery(schema);

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
                    _mySqlConn.Dispose();
            }
        }

        /// <inheritdoc />
        protected override async Task<IEnumerable<TResult>> ExecuteReaderAsyncImpl<TResult>(
            Action<DbDataReader, List<TResult>> getObjectDel,
            string query, int? commandTimeout, string[] partitionOnArr, bool validateSelectColumns,
            DbConnection dbConnection, DbTransaction transaction,
            string cacheKey, Action saveCacheDel, bool valueOrStringType = false)
        {
            var userProvidedConnection = false;

            try
            {
                query = GetCleanSqlCommand(query);

                userProvidedConnection = dbConnection != null;

                // Try open connection if not already open.
                if (!userProvidedConnection)
                    _mySqlConn = new MySqlConnection(_connectionString);

                else
                    _mySqlConn = dbConnection as MySqlConnection;

                await OpenConnAsync(_mySqlConn);

                var result = new List<TResult>();

                using (MySqlCommand command = new MySqlCommand(query, _mySqlConn))
                {
                    // Set common SqlCommand properties
                    SetCommandProps(command, transaction, commandTimeout, query);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                            return (List<TResult>) Activator.CreateInstance(typeof(List<TResult>));

                        if (!valueOrStringType)
                        {
                            var schema = reader.GetSchemaTable();
                            var rowList = schema?.Rows.Cast<DataRow>().ToList();

                            int[] partitionOnOrdinal = null;

                            if (partitionOnArr != null)
                                partitionOnOrdinal =
                                    CraneHelper.GetOrdinalPartition(rowList, partitionOnArr, SprocObjectMapList.Count);

                            CraneHelper.SetOrdinal(rowList, SprocObjectMapList, partitionOnOrdinal);

                            if (validateSelectColumns)
                                CraneHelper.ValidateSelectColumns(rowList, SprocObjectMapList, partitionOnOrdinal,
                                    query);

                            CraneHelper.ValidateSchema(schema, SprocObjectMapList, partitionOnOrdinal);
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
                    _mySqlConn.Dispose();
            }
        }

        /// <inheritdoc />
        public override T ExecuteScalar<T>(string query, int? commandTimeout = null, DbConnection dbConnection = null, DbTransaction transaction = null)
        {
            try
            {
                query = GetCleanSqlCommand(query);

                T obj;

                // Try open connection if not already open.
                if (dbConnection == null)
                    _mySqlConn = new MySqlConnection(_connectionString);

                else
                    _mySqlConn = dbConnection as MySqlConnection;

                OpenConn(_mySqlConn);

                using (MySqlCommand command = new MySqlCommand(query, _mySqlConn))
                {
                    SetCommandProps(command, transaction, commandTimeout, query);
                    obj = (T)command.ExecuteScalar();
                }

                return obj;
            }
            finally
            {
                if (dbConnection == null)
                    _mySqlConn.Dispose();
            }

        }

        /// <inheritdoc />
        public override async Task<T> ExecuteScalarAsync<T>(string query, int? commandTimeout = null, DbConnection dbConnection = null, DbTransaction transaction = null)
        {
            try
            {
                query = GetCleanSqlCommand(query);

                T obj;

                // Try open connection if not already open.
                if (dbConnection == null)
                    _mySqlConn = new MySqlConnection(_connectionString);

                else
                    _mySqlConn = dbConnection as MySqlConnection;

                await OpenConnAsync(_mySqlConn);

                using (MySqlCommand command = new MySqlCommand(query, _mySqlConn))
                {
                    SetCommandProps(command, transaction, commandTimeout, query);
                    obj = (T)await command.ExecuteScalarAsync();
                }

                return obj;
            }

            finally
            {
                if (dbConnection == null)
                    _mySqlConn.Dispose();
            }
        }
    }
}
