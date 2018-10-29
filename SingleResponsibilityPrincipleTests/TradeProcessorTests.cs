using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using SingleResponsibilityPrinciple;
using System;
using System.Collections.Generic;
using System.Data;
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
                if (myCommand.Connection.State == ConnectionState.Closed)
                {
                    myCommand.Connection.Open();
                }
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

            int startCount = CountDbRecords();

            var tradeProcessor = new TradeProcessor();

            //Act
            tradeProcessor.ProcessTrades(tradeStream);

            //Assert
            int count = CountDbRecords();
            Assert.AreEqual(4, count - startCount);
        }

        //Testing bad tests
        [TestMethod()]
        public void ProcessBadTradesTest()
        {
            //Arrange
            var tradeStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SingleResponsibilityPrincipleTests.badtrades.txt");

            int startCount = CountDbRecords();

            var tradeProcessor = new TradeProcessor();

            //Act
            tradeProcessor.ProcessTrades(tradeStream);

            //Assert
            int count = CountDbRecords();
            Assert.AreEqual(2, count - startCount); //Two records shouldn't insert correctly
        }

        //Price has symbol in front of it
        [TestMethod()]
        public void PriceContainsSymbolTest()
        {
            //Arrange
            List<String> tradeData = new List<String>();
            tradeData.Add("GDBCAD,1000,$1.51");
            int startCount = CountDbRecords();
            var tradeProcessor = new TradeProcessor();

            //Act
            var trades = tradeProcessor.ParseTrades(tradeData);
            tradeProcessor.StoreTrades(trades);

            //Assert
            Assert.AreEqual(1, CountDbRecords() - startCount);
        }

        //User has lot size in decimal instead of integer
        [TestMethod()]
        public void LotSizeIsDecimalTest()
        {
            //Arrange
            List<String> tradeData = new List<String>();
            tradeData.Add("GDBCAD,1000.1,1.51");
            int startCount = CountDbRecords();
            var tradeProcessor = new TradeProcessor();

            //Act
            var trades = tradeProcessor.ParseTrades(tradeData);
            tradeProcessor.StoreTrades(trades);

            //Assert
            Assert.AreEqual(1, CountDbRecords() - startCount);
        }

        //User accidentally puts space between currencies in first field
        [TestMethod()]
        public void SpaceInTradeCurrencies()
        {
            //Arrange
            List<String> tradeData = new List<String>();
            tradeData.Add("GDB CAD,1000,1.51");
            int startCount = CountDbRecords();
            var tradeProcessor = new TradeProcessor();

            //Act
            var trades = tradeProcessor.ParseTrades(tradeData);
            tradeProcessor.StoreTrades(trades);

            //Assert
            Assert.AreEqual(1, CountDbRecords() - startCount);
        }

        //User accidentally puts currency in first two fields instead of just in first one
        [TestMethod()]
        public void CurrenciesTwoFieldsTest()
        {
            //Arrange
            List<String> tradeData = new List<String>();
            tradeData.Add("GDB,CAD,1000,1.51");
            int startCount = CountDbRecords();
            var tradeProcessor = new TradeProcessor();

            //Act
            var trades = tradeProcessor.ParseTrades(tradeData);
            tradeProcessor.StoreTrades(trades);

            //Assert
            Assert.AreEqual(1, CountDbRecords() - startCount);
        }
    }
}