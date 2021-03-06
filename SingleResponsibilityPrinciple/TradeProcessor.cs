﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleResponsibilityPrinciple
{
    public class TradeProcessor
    {
        public IEnumerable<string> ReadTradeData(Stream stream)
        {
            var tradeData = new List<string>();
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    tradeData.Add(line);
                }
            }
            return tradeData;
        }

        public IEnumerable<TradeRecord> ParseTrades(IEnumerable<string> tradeData)
        {
            var trades = new List<TradeRecord>();
            var lineCount = 1;
            foreach (var line in tradeData)
            {
                var fields = line.Split(new char[] { ',' });

                if (!ValidateTradeData(fields, lineCount))
                {
                    continue;
                }

                var trade = MapTradeDataToTradeRecord(fields);

                trades.Add(trade);

                lineCount++;
            }

            return trades;
        }

        private bool ValidateTradeData(string[] fields, int currentLine)
        {
            if (fields.Length != 3)
            {
                //If user accidentally has the currencies split into two fields
                if (fields[0].Length == 3 && fields[1].Length == 3)
                {
                    fields[0] = fields[0] + fields[1]; //Merge first two fields
                    fields[1] = fields[2]; //Move left one
                    fields[2] = fields[3]; //Move left one
                }
                else
                {
                    LogMessage("WARN: Line {0} malformed. Only {1} field(s) found.", currentLine, fields.Length);
                    return false;
                }
            }

            if (fields[0].Length != 6)
            {
                //If user has space in between currencies
                if (fields[0].Substring(3, 1).Equals(" "))
                {
                    //Remove space from currencies
                    fields[0] = fields[0].Substring(0, 3) + fields[0].Substring(4, 3);
                }
                else
                {
                    LogMessage("WARN: Trade currencies on line {0} malformed: '{1}'", currentLine, fields[0]);
                    return false;
                }
            }

            //Checks if value is not parse-able or if the amount is less than 0
            if (!int.TryParse(fields[1], out int tradeAmount) || tradeAmount < 0)
            {
                //If lot size is a decimal
                if (decimal.TryParse(fields[1], out decimal tradeAmountDecimal) && tradeAmountDecimal > 0) //Also checks if value is greater than 0
                {
                    //Round down lot size and parse to int
                    int.TryParse(Math.Floor(tradeAmountDecimal).ToString(), out tradeAmount);
                    fields[1] = Math.Floor(tradeAmountDecimal).ToString();
                }
                else
                {
                    LogMessage("WARN: Trade amount on line {0} not a valid integer: '{1}'", currentLine, fields[1]);
                    return false;
                }
            }

            decimal tradePrice;
            if (!decimal.TryParse(fields[2], out tradePrice) || tradePrice < 0)
            {
                //Check if tradePrice is negative
                if (fields[2].Substring(0,1).Equals("-"))
                {
                    LogMessage("WARN: Trade price on line {0} cannot be negative: '{1}'", currentLine, fields[2]);
                    return false;
                }
                //If user puts symbol in front of price ($100)
                else if (decimal.TryParse(fields[2].Substring(1), out tradePrice))
                {
                    //Field is now all but first character
                    fields[2] = fields[2].Substring(1);
                }
                else
                {
                    LogMessage("WARN: Trade price on line {0} not a valid decimal: '{1}'", currentLine, fields[2]);
                    return false;
                }
            }
            return true;
        }

        private void LogMessage(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }

        private TradeRecord MapTradeDataToTradeRecord(string[] fields)
        {
            float LotSize = 100000f;

            var sourceCurrencyCode = fields[0].Substring(0, 3);
            var destinationCurrencyCode = fields[0].Substring(3, 3);
            var tradeAmount = int.Parse(fields[1]);
            var tradePrice = decimal.Parse(fields[2]);

            var trade = new TradeRecord
            {
                SourceCurrency = sourceCurrencyCode,
                DestinationCurrency = destinationCurrencyCode,
                Lots = tradeAmount / LotSize,
                Price = tradePrice
            };

            return trade;
        }

        public void StoreTrades(IEnumerable<TradeRecord> trades)
        {
            LogMessage("INFO: Connecting to database");
            // The first connection string uses |DataDirectory| 
            //    and assumes the tradedatabase.mdf file is stored in 
            //    SingleResponsibilityPrinciple\bin\Debug 
            //    using (var connection = new System.Data.SqlClient.SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\tradedatabase.mdf;Integrated Security=True;Connect Timeout=30;"))
            // Template for connection string from database connection file
            //    The @ sign allows for back slashes
            //    Watch for double quotes which must be escaped using "" 
            //    Watch for extra spaces after C: and avoid paths with - hyphens -
            using (var connection = new System.Data.SqlClient.SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\okrueger\source\repos\cis-3285-asg-8-Owen-Krueger\tradedatabase.mdf;Integrated Security=True;Connect Timeout=30;"))
            //using (var connection = new System.Data.SqlClient.SqlConnection("Data Source=(local);Initial Catalog=TradeDatabase;Integrated Security=True;"))
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var trade in trades)
                    {
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.CommandText = "dbo.insert_trade";
                        command.Parameters.AddWithValue("@sourceCurrency", trade.SourceCurrency);
                        command.Parameters.AddWithValue("@destinationCurrency", trade.DestinationCurrency);
                        command.Parameters.AddWithValue("@lots", trade.Lots);
                        command.Parameters.AddWithValue("@price", trade.Price);

                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                connection.Close();
            }

            LogMessage("INFO: {0} trades processed", trades.Count());
        }

        public void ProcessTrades(Stream stream)
        {
            var lines = ReadTradeData(stream);
            var trades = ParseTrades(lines);
            StoreTrades(trades);
        }

    }
}
