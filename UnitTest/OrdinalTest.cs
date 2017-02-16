﻿using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SprocMapperLibrary;
using System.Linq;
using Model;
using Moq;

namespace UnitTest
{
    [TestClass]
    public class OrdinalTest
    {

        [TestMethod]
        public void TestGetObject()
        {          
            var moq = new Mock<IDataReader>();

            moq.Setup(x => x[4]).Returns(5);
            moq.Setup(x => x[1]).Returns("Donald");
            moq.Setup(x => x[2]).Returns("Trump");
            moq.Setup(x => x[0]).Returns(1);
            moq.Setup(x => x[5]).Returns(true);

            var objectMap = PropertyMapper.MapObject<President>()
                .AddAllColumns()
                .GetMap();

            var dataTable = GetTestSchemaTwoTables();

            SprocMapper.SetOrdinal(dataTable, new List<ISprocObjectMap>() {objectMap});

            var result = SprocMapper.GetObject<President>(objectMap, moq.Object);

            Assert.AreEqual(5, result.Fans);
            Assert.AreEqual("Donald", result.FirstName);
            Assert.IsTrue(result.IsHonest);
        }

        [TestMethod]
        public void TestOrdinal()
        {
            DataTable schemaTable = GetTestSchemaTwoTables();

            List<ISprocObjectMap> list = new List<ISprocObjectMap>();

            var presidentObjectMap = PropertyMapper.MapObject<President>()
                .AddAllColumns()
                .GetMap();

            var assPresidentObjectMap = PropertyMapper.MapObject<PresidentAssistant>()
                .CustomColumnMapping(x => x.LastName, "Assistant Last Name")
                .CustomColumnMapping(x => x.FirstName, "Assistant First Name")
                .IgnoreColumn(x => x.Id)
                .AddAllColumns()
                .GetMap();

            list.Add(presidentObjectMap);
            list.Add(assPresidentObjectMap);

            SprocMapper.SetOrdinal(schemaTable, list);

            Assert.AreEqual(6, list.ElementAt(1).ColumnOrdinalDic["PresidentId"]);
        }

        [TestMethod]
        public void TestOrdinalForId()
        {
            DataTable schemaTable = GetTestSchemaTwoTables();

            List<ISprocObjectMap> list = new List<ISprocObjectMap>();

            var presidentObjectMap = PropertyMapper.MapObject<President>()
                .AddAllColumns()
                .GetMap();

            list.Add(presidentObjectMap);

            SprocMapper.SetOrdinal(schemaTable, list);

            Assert.AreEqual(0, list.ElementAt(0).ColumnOrdinalDic["Id"]);
        }

        [TestMethod]
        public void TestOrdinalForCustomMapping()
        {
            DataTable schemaTable = GetTestSchemaTwoTables();

            List<ISprocObjectMap> list = new List<ISprocObjectMap>();

            var presidentObjectMap = PropertyMapper.MapObject<President>()
                .AddAllColumns()
                .GetMap();

            var assPresidentObjectMap = PropertyMapper.MapObject<PresidentAssistant>()
                .CustomColumnMapping(x => x.LastName, "Assistant Last Name")
                .CustomColumnMapping(x => x.FirstName, "Assistant First Name")
                .IgnoreColumn(x => x.Id)
                .AddAllColumns()
                .GetMap();

            list.Add(presidentObjectMap);
            list.Add(assPresidentObjectMap);

            SprocMapper.SetOrdinal(schemaTable, list);

            Assert.AreEqual(8, list.ElementAt(1).ColumnOrdinalDic["Assistant Last Name"]);
        }

        private DataTable GetTestSchemaTwoTables()
        {
            DataTable tab = new DataTable("Test") { };
            tab.Columns.Add("ColumnName");
            tab.Columns.Add("ColumnOrdinal");

            tab.Rows.Add("Id", 0);
            tab.Rows.Add("FirstName", 1);
            tab.Rows.Add("LastName", 2);
            tab.Rows.Add("Last Name", 3);
            tab.Rows.Add("Fans", 4);
            tab.Rows.Add("IsHonest", 5);
            tab.Rows.Add("PresidentId", 6);
            tab.Rows.Add("Assistant First Name", 7);
            tab.Rows.Add("Assistant Last Name", 8);
            return tab;
        }
    }
}