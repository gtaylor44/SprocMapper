﻿using System;
using System.Data.SqlClient;
using SprocMapperLibrary.Base;

namespace SprocMapperLibrary.SqlServer
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlServerAccess
    {
        private AbstractCacheProvider _cacheProvider;
        private readonly SqlConnection _conn;     
        private const string InvalidConnMsg = "Please ensure that valid Sql Server Credentials have been passed in.";
        private const string InvalidCacheMsg = "Cache provider already registered.";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        public SqlServerAccess(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentException(InvalidConnMsg);

            _conn = new SqlConnection(connectionString);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="credential"></param>
        public SqlServerAccess(string connectionString, SqlCredential credential)
        {
            if (connectionString == null || credential == null)
                throw new ArgumentException(InvalidConnMsg);

            _conn = new SqlConnection(connectionString, credential);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public SqlServerSproc Sproc()
        {
            return new SqlServerSproc(_conn, _cacheProvider);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheProvider"></param>
        public void RegisterCacheProvider(AbstractCacheProvider cacheProvider)
        {
            if (_cacheProvider != null)
                throw new InvalidOperationException(InvalidCacheMsg);

            _cacheProvider = cacheProvider;
        }
    }
}
