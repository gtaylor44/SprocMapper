﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
namespace SprocMapperLibrary
{
    public class Select<T> : AbstractSelect
    {
        public Select(List<ISprocObjectMap> sprocObjectMapList) : base(sprocObjectMapList){ }

        public new Select<T> AddSqlParameterList(List<SqlParameter> paramList)
        {
            base.AddSqlParameterList(paramList);
            return this;
        }

        public List<T> ExecuteReader(SqlConnection conn, string cmdText, int commandTimeout = 600)
        {
            ValidateProperties();
            OpenConn(conn);

            List<T> result = new List<T>();

            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            using (SqlCommand command = new SqlCommand(cmdText, conn))
            {
                SetCommandProps(command, commandTimeout);

                var reader = command.ExecuteReader();

                if (!reader.HasRows)
                    return default(List<T>);

                while (reader.Read())
                {
                    T obj = SprocMapperHelper.GetObject<T>(SprocObjectMapList[0].Columns,
                        SprocObjectMapList[0].CustomColumnMappings, reader);
                    result.Add(obj);
                }

            }
            return result;

        }

    }

}

