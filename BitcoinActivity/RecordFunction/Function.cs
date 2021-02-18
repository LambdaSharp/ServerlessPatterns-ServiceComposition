using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LambdaSharp;
using LambdaSharp.ApiGateway;

namespace ServerlessDotNetPatterns.BitcoinActivity.RecordFunction {

    public class ActivityRequest {

        //--- Properties ---

        [Required]
        public string UserId { get; set; }

        [Range(double.Epsilon, double.MaxValue)]
        public double Quantity { get; set; }
    }

    public class ActivityRecord {

        //--- Properties ---
        public string UserId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Activity { get; set; }
        public double Quantity { get; set; }
        public double Price { get; set; }

    }

    public sealed class Function : ALambdaApiGatewayFunction {

        //--- Fields ---
        private string _activityTableName;
        private string _bitcoinPriceTableName;
        private IAmazonDynamoDB _dynamoClient;

        //--- Constructors ---
        public Function() : base(new LambdaSharp.Serialization.LambdaSystemTextJsonSerializer()) { }

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read function settings
            _activityTableName = config.ReadDynamoDBTableName("Table");
            _bitcoinPriceTableName = config.ReadDynamoDBTableName("BitcoinPriceTable");

            // initialize clients
            _dynamoClient = new AmazonDynamoDBClient();
        }

        public async Task<ActivityRecord> BuyAsync(ActivityRequest activity) {

            // fetch latest price
            var latestPrice = await GetLatestBitcoinPriceAsync();
            LogInfo($"latest price is: {latestPrice}");

            // create and store record
            var record = new ActivityRecord {
                UserId = activity.UserId,
                Timestamp = DateTimeOffset.UtcNow,
                Activity = "BUY",
                Price = latestPrice,
                Quantity = activity.Quantity
            };
            await StoreRecordAsync(record);
            return record;
        }

        public async Task<ActivityRecord> SellAsync(ActivityRequest activity) {

            // fetch latest price
            var latestPrice = await GetLatestBitcoinPriceAsync();
            LogInfo($"latest price is: {latestPrice}");

            // create and store record
            var record = new ActivityRecord {
                UserId = activity.UserId,
                Timestamp = DateTimeOffset.UtcNow,
                Activity = "SELL",
                Price = latestPrice,
                Quantity = activity.Quantity
            };
            await StoreRecordAsync(record);
            return record;
        }

        private async Task<double> GetLatestBitcoinPriceAsync() {
            var result = await _dynamoClient.QueryAsync(new QueryRequest {
                TableName = _bitcoinPriceTableName,
                KeyConditionExpression = "PK = :v_PK and begins_with(SK, :v_SK)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    [":v_PK"] = new AttributeValue("BITCOINPRICE"),
                    [":v_SK"] = new AttributeValue("TIME#")
                },
                Limit = 1,
                ScanIndexForward = false
            });
            var attributes = result.Items.FirstOrDefault();
            if(attributes == null) {
                throw new InvalidOperationException("no price rows returned");
            }
            if(!attributes.TryGetValue("USD", out var usdPriceAttribute)) {
                throw new InvalidOperationException("missing USD price");
            }
            if(!double.TryParse(usdPriceAttribute.N, out var usdPrice)) {
                throw new InvalidOperationException("invalid USD price");
            }
            return usdPrice;
        }

        private Task StoreRecordAsync(ActivityRecord record) {

            // store activity in a new DynamoDB row
            return _dynamoClient.PutItemAsync(_activityTableName, new Dictionary<string, AttributeValue> {
                ["PK"] = new AttributeValue($"USER#{record.UserId}"),
                ["SK"] = new AttributeValue($"TIME#{record.Timestamp.ToUnixTimeSeconds().ToString("000000000000")}"),
                ["Activity"] = new AttributeValue(record.Activity),
                ["Quantity"] = new AttributeValue {
                    N = record.Quantity.ToString()
                },
                ["Price"] = new AttributeValue {
                    N = record.Price.ToString()
                }
            });
        }
    }
}
