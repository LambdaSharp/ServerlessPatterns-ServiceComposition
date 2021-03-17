using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaSharp;
using LambdaSharp.SimpleNotificationService;

namespace ServerlessPatterns.BitcoinTable.StoreBitcoinFunction {

    public class BitcoinNotification {

        //--- Properties ---
        [JsonPropertyName("bpi")]
        public Dictionary<string, BitcoinQuote> BitcoinPriceIndex { get; set; }
    }

    public class BitcoinQuote {

        //--- Properties ---

        [JsonPropertyName("rate_float")]
        public double Rate { get; set; }
    }

    public sealed class Function : ALambdaTopicFunction<BitcoinNotification> {

        //--- Fields ---
        private string _tableName;
        private int _priceRetentionInMinutes;
        private IAmazonDynamoDB _dynamoClient;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read function settings
            _tableName = config.ReadDynamoDBTableName("Table");
            _priceRetentionInMinutes = config.ReadInt("PriceRetentionInMinutes");

            // initialize clients
            _dynamoClient = new AmazonDynamoDBClient();
        }

        public override async Task ProcessMessageAsync(BitcoinNotification message) {

            // store Bitcoin price in a new DynamoDB row
            var attributes = new Dictionary<string, AttributeValue> {
                ["PK"] = new AttributeValue("BITCOINPRICE"),
                ["SK"] = new AttributeValue($"TIME#{DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString("000000000000")}"),
                ["Expire"] = new AttributeValue {
                    N = DateTimeOffset.UtcNow.AddMinutes(_priceRetentionInMinutes).ToUnixTimeSeconds().ToString()
                }
            };
            foreach(var (symbol,price) in message.BitcoinPriceIndex) {
                attributes[symbol] = new AttributeValue {
                    N = price.Rate.ToString()
                };
            }
            await _dynamoClient.PutItemAsync(_tableName, attributes);
        }
    }
}
