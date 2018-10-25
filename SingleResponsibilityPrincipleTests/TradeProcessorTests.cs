﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SingleResponsibilityPrinciple;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SingleResponsibilityPrinciple.Tests
{
    [TestClass()]
    public class TradeProcessorTests
    {
        /// <summary>
        /// Counts database records. To be used by tests
        /// </summary>
        /// <returns></returns>
        private int CountDbRecords()
        {
            using (var connection = new System.Data.SqlClient.SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\okrueger\source\repos\cis-3285-asg-8-Owen-Krueger\tradedatabase.mdf;Integrated Security=True;Connect Timeout=30;"))
            {
                connection.Open();
                string myScalarQuery = "SELECT COUNT(*) FROM trade";
                SqlCommand myCommand = new SqlCommand(myScalarQuery, connection);
                myCommand.Connection.Open();
                int count = (int)myCommand.ExecuteScalar();
                connection.Close();
                return count;
            }
        }

        [TestMethod()]
        public void ProcessTradesTest()
        {
            //Arrange
            var tradeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SingleResponsibilityPrincipleTests.goodtrades.txt");

            var tradeProcessor = new TradeProcessor();

            //Act
            tradeProcessor.ProcessTrades(tradeStream);

            //Assert
            int count = CountDbRecords();
            Console.WriteLine(count);
            Assert.AreEqual(count, 4);
        }
    }
}