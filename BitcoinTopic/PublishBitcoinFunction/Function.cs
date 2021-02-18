using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using LambdaSharp;
using LambdaSharp.Schedule;

namespace ServerlessPatterns.BitcoinTopic.PublishBitcoinFunction
{

    public sealed class Function : ALambdaScheduleFunction {

        //--- Fields ---
        private IAmazonSimpleNotificationService _snsClient;
        private string _topicArn;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // read function settings
            _topicArn = config.ReadText("Topic");

            // initialize clients
            _snsClient = new AmazonSimpleNotificationServiceClient();
        }

        public override async Task ProcessEventAsync(LambdaScheduleEvent schedule) {

            // fetch Bitcoin price from API
            var response = await HttpClient.GetAsync("https://api.coindesk.com/v1/bpi/currentprice.json");
            if(response.IsSuccessStatusCode) {
                LogInfo("Fetched price from API");

                // publish Bitcoin JSON on topic
                await _snsClient.PublishAsync(_topicArn, await response.Content.ReadAsStringAsync());
            } else {
                LogWarn("Unable to fetch price");
            }
        }
    }
}
