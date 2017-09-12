﻿using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SprocMapperLibrary;
using SprocMapperLibrary.SqlServer;
using SprocMapperLibrary.TestCommon;
using SprocMapperLibrary.TestCommon.Model;

namespace IntegrationTest
{
    [TestClass]
    public class CoreProcedureTest
    {
        [TestMethod]
        public void InsertCustomerThenDelete()
        {
            SqlServerAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            Customer customer = new Customer()
            {
                City = "Auckland",
                Country = "New Zealand",
                FirstName = "Greg",
                LastName = "Taylor",
                Phone = "021222222"
            };

            int inserted = 0;


            using (SqlConnection conn = SqlConnectionFactory.GetSqlConnection())
            {

                SqlParameter idParam = new SqlParameter() { ParameterName = "@Id", DbType = DbType.Int32, Direction = ParameterDirection.Output };

                inserted = dataAccess.Sproc()
                    .AddSqlParameter(idParam)
                    .AddSqlParameter("@City", customer.City)
                    .AddSqlParameter("@Country", customer.Country)
                    .AddSqlParameter("@FirstName", customer.FirstName)
                    .AddSqlParameter("@LastName", customer.LastName)
                    .AddSqlParameter("@Phone", customer.Phone)
                    .ExecuteNonQuery("dbo.SaveCustomer", unmanagedConn: conn);

                int id = idParam.GetValueOrDefault<int>();

                if (id == default(int))
                    throw new InvalidOperationException("Id output not parsed");

                dataAccess.Sproc()
                    .AddSqlParameter("@CustomerId", id)
                    .ExecuteNonQuery("dbo.DeleteCustomer", unmanagedConn: conn);

            }

            Assert.AreEqual(1, inserted);
        }
    }
}
