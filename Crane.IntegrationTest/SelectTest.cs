﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Crane;
using Crane.CacheProvider.MemoryCache;
using Crane.Interface;
using Crane.SqlServer;
using Crane.TestCommon;
using Crane.TestCommon.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlBulkTools;
using Crane.CacheProvider;

namespace IntegrationTest
{
    [TestClass]
    public class SelectTest
    {

        // Returns all products with Id and Product Name only
        // Id has an alias of 'Product Id'
        [TestMethod]
        public void GetProductsDynamic()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            var productList = dataAccess.Query().ExecuteReader("dbo.GetProducts")
                .ToList()
                .ConvertAll(x => new Product()
                {
                    Id = x.ProductId,
                    ProductName = x.ProductName
                });

            Assert.IsTrue(productList.Any());
        }

        // Returns all products with Id and Product Name only
        // Id has an alias of 'Product Id'
        [TestMethod]
        public void GetProductsDynamicWithCallback()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            List<Product> productList = new List<Product>();
            dataAccess.Query()
                .ExecuteReader("dbo.GetProducts", (x) =>
                {
                    productList.Add(new Product()
                    {
                        Id = x.ProductId,
                        ProductName = x.ProductName
                    });
                });

            Assert.IsTrue(productList.Any());
        }

        [TestMethod]
        public void GetAllOrders()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            Dictionary<int, Customer> customerDic = new Dictionary<int, Customer>();

            var result = dataAccess
                .Query()
                .ExecuteReader<Order>("select * from [dbo].[order]");

            Assert.IsTrue(result.Any());
        }

        [TestMethod]
        public void GetAllCustomersAndOrders()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            Dictionary<int, Customer> customerDic = new Dictionary<int, Customer>();

            dataAccess.Query()
                .ExecuteReader<Customer, Order>("dbo.GetAllCustomersAndOrders", (c, o) =>
                {
                    Customer customer;

                    if (!customerDic.TryGetValue(c.Id, out customer))
                    {
                        customer = c;
                        customer.CustomerOrders = new List<Order>();
                        customerDic.Add(customer.Id, customer);
                    }

                    if (o != null)
                        customer.CustomerOrders.Add(o);

                }, partitionOn: "Id|Id");

            Assert.IsTrue(customerDic.Count > 0);
        }

        // Returns all products with Id and Product Name only
        // Id has an alias of 'Product Id'
        [TestMethod]
        public void GetProducts()
        {
            var cacheProvider = new MemoryCacheProvider();

            cacheProvider.AddPolicy("GetProducts", new CraneCachePolicy()
            {
                InfiniteExpiration = true            
            });

            var options = new QueryOptions
            {
                CacheProvider = cacheProvider,
                ValidateSelectColumns = true
            };

            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString, options);

            var products = dataAccess.Query()
                .CustomColumnMapping<Product>(x => x.Id, "Product Id")
                .ExecuteReader<Product>("dbo.GetProducts", cacheKey: "GetProducts");

            var products2 = dataAccess.Query()
                .CustomColumnMapping<Product>(x => x.Id, "Product Id")
                .ExecuteReader<Product>("dbo.GetProducts", cacheKey: "GetProducts");

            Assert.IsTrue(products2.Count() > 0);
        }

        [TestMethod]
        public void GetProductsTextQuery()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            var products = dataAccess.Query()
                .AddSqlParameter("@SupplierId", 1)
                .ExecuteReader<Product>("SELECT * FROM dbo.Product WHERE SupplierId = @SupplierId");

            Assert.IsTrue(products.Count() == 3);
        }

        [TestMethod]
        public void GetProducts_WithCallback()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            List<Product> productList = new List<Product>();

            dataAccess.Query()
                .CustomColumnMapping<Product>(x => x.Id, "Product Id")
                .ExecuteReader<Product>("dbo.GetProducts", (product) =>
                {
                    // do something special with product
                    productList.Add(product);
                });

            Assert.IsTrue(productList.Count > 0);

        }

        // 1:M relationship example
        [TestMethod]
        public void SelectSingleCustomerAndOrders()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            Customer cust = null;

            dataAccess.Query()
                .AddSqlParameter("@FirstName", "Thomas")
                .AddSqlParameter("@LastName", "Hardy")
                .CustomColumnMapping<Order>(x => x.Id, "OrderId")
                .ExecuteReader<Customer, Order>("dbo.GetCustomerAndOrders", (c, o) =>
                {
                    if (cust == null)
                    {
                        cust = c;
                        cust.CustomerOrders = new List<Order>();
                    }

                    if (o.Id != default(int))
                        cust.CustomerOrders.Add(o);

                }, partitionOn: "Id|OrderId");


            Assert.AreEqual(13, cust.CustomerOrders.Count);
            Assert.IsNotNull(cust);
        }

        // 1:1 relationship example
        [TestMethod]
        public void GetProductAndSupplier()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            int productId = 62;
            Product product = null;


            product = dataAccess.Query()
                .AddSqlParameter("@Id", productId)
                .ExecuteReader<Product, Supplier>("[dbo].[GetProductAndSupplier]", (p, s) =>
                {
                    p.Supplier = s;

                }, partitionOn: "ProductName|Id")
                .FirstOrDefault();


            Assert.AreEqual("Tarte au sucre", product?.ProductName);
            Assert.AreEqual("Chantal Goulet", product?.Supplier.ContactName);

            Assert.AreNotEqual(0, product?.Supplier.Id);
        }

        [TestMethod]
        public void GetOrderAndProducts()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            int orderId = 20;

            Order order = null;


            Dictionary<int, Order> orderDic = new Dictionary<int, Order>();

            dataAccess.Query()
            .AddSqlParameter("@OrderId", orderId)
            .CustomColumnMapping<Product>(x => x.UnitPrice, "Price")
            .ExecuteReader<Order, OrderItem, Product>("dbo.GetOrder", (o, oi, p) =>
                {
                    Order ord;
                    if (!orderDic.TryGetValue(o.Id, out ord))
                    {
                        orderDic.Add(o.Id, o);
                        o.OrderItemList = new List<OrderItem>();
                    }

                    order = orderDic[o.Id];
                    oi.Product = p;
                    order.OrderItemList.Add(oi);
                }, partitionOn: "Id|unitprice|productname");


            Assert.IsNotNull(order);
        }

        [TestMethod]
        public void GetSuppliers()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            var suppliers = dataAccess.Query()
                .ExecuteReader<Supplier>("dbo.GetSuppliers");
            Assert.IsTrue(suppliers.Any());
        }

        [TestMethod]
        public void GetCustomer()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);


            var customer = dataAccess.Query()
                .AddSqlParameter("@CustomerId", 6)
                .ExecuteReader<Customer>("dbo.GetCustomer")
                .FirstOrDefault();

            Assert.AreEqual("Hanna", customer?.FirstName);
            Assert.AreEqual("Moos", customer?.LastName);
            Assert.AreEqual("Mannheim", customer?.City);
            Assert.AreEqual("Germany", customer?.Country);

        }

        [TestMethod]
        public void GetSupplierByName()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            var supplier = dataAccess.Query()
                .AddSqlParameter("@SupplierName", "Bigfoot Breweries")
                .ExecuteReader<Supplier>("dbo.GetSupplierByName")
                .FirstOrDefault();

            Assert.AreEqual("Cheryl Saylor", supplier?.ContactName);

        }

        [TestMethod]
        [MyExpectedException(typeof(CraneException), "Custom column mapping must map to a unique property. A property with the name 'ProductName' already exists.")]
        public void CustomColumnName_MustBeUniqueToClass()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            dataAccess.Query()           
                .CustomColumnMapping<Product>(x => x.Package, "ProductName")
                .ExecuteReader<Product>("dbo.GetProducts");
        }

        [TestMethod]
        [MyExpectedException(typeof(CraneException), "A cache key has been provided without a cache provider. Use the method 'RegisterCacheProvider' to register a cache provider.")]
        public void CacheKeyNotProvided_ThrowsException()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            dataAccess.Query().ExecuteReader<Product>("dbo.GetProducts", cacheKey: "test");
        }

        [TestMethod]
        public void SaveGetDataTypes()
        {
            ICraneAccess dataAccess = new SqlServerAccess(SqlConnectionFactory.SqlConnectionString);

            BulkOperations bulk = new BulkOperations();

            TestDataType dataTypeTest = new TestDataType()
            {
                IntTest = 1,
                //SmallIntTest = 3433,
                BigIntTest = 342324324324324324,
                TinyIntTest = 126,
                DateTimeTest = DateTime.UtcNow,
                DateTime2Test = new DateTime(2008, 12, 12, 10, 20, 30),
                DateTest = new DateTime(2007, 7, 5, 20, 30, 10),
                TimeTest = new TimeSpan(23, 32, 23),
                SmallDateTimeTest = new DateTime(2005, 7, 14),
                BinaryTest = new byte[] { 0, 3, 3, 2, 4, 3 },
                VarBinaryTest = new byte[] { 3, 23, 33, 243 },
                DecimalTest = 178.43M,
                MoneyTest = 24333.99M,
                SmallMoneyTest = 103.32M,
                RealTest = 32.53F,
                NumericTest = 154343.3434342M,
                FloatTest = 232.43F,
                FloatTest2 = 43243.34,
                TextTest = "This is some text.",
                GuidTest = Guid.NewGuid(),
                CharTest = "Some",
                XmlTest = "<title>The best SQL Bulk tool</title>",
                NCharTest = "SomeText",
                ImageTest = new byte[] { 3, 3, 32, 4 }
            };

            using (SqlConnection conn = new SqlConnection(SqlConnectionFactory.SqlConnectionString))
            {
                bulk.Setup<TestDataType>()
                    .ForObject(dataTypeTest)
                    .WithTable("TestDataTypes")
                    .AddAllColumns()
                    .Upsert()
                    .MatchTargetOn(x => x.IntTest)
                    .Commit(conn);

                var result = dataAccess.Query()
                    .ExecuteReader<TestDataType>("dbo.GetTestDataTypes", dbConnection: conn)
                    .SingleOrDefault();

                Assert.AreEqual(1, result?.IntTest);
            }
        }
    }
}
