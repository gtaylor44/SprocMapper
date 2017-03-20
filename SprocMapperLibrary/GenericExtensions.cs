﻿using System;
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace SprocMapperLibrary
{
    /// <summary>
    /// 
    /// </summary>
    public static class GenericExtensions
    {

        /// <summary>
        /// Extension method for safely getting output parameters from SqlParameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlParameter"></param>
        /// <returns></returns>
        public static T GetValueOrDefault<T>(this SqlParameter sqlParameter)
        {
            if (sqlParameter.Value == DBNull.Value)
            {
                if (typeof(T).IsValueType)
                    return (T)Activator.CreateInstance(typeof(T));

                return (default(T));
            }

            return (T)sqlParameter.Value;
        }
    }
}