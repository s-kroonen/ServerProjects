
using MQTTnet;
using MQTTnet.Protocol;
using System.Text;
namespace BeerTap.Services
{
    public class MqttService : IHostedService
    {
        private const int Timeout = 5000;
        public List<string> TapIds = new() { "1", "2" };

        private IMqttClient _mqttClient;
        private readonly TapQueueManager _tapQueueManager;
        public event Action<string, int>? OnAmountUpdated;
        public event Action<string, string>? OnStatusUpdated;

        private readonly string _topicPrefix = "beer/tap/";
        private readonly string _clientId = "TapApi";
        //private readonly string _host = "wall-e";
        //private readonly int _port = 1883;
        private readonly string _host = "kroon-en.nl";
        private readonly int _port = 8883;
        private readonly string _username = "public";
        private readonly string _password = "temp-01";
        public MqttService(TapQueueManager tapQueueManager)
        {
            _tapQueueManager = tapQueueManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ConnectAsync();
            _tapQueueManager.CurrentUserChanged += async (tapId, userId) =>
            {
                await AnnounceCurrentUser(tapId, userId);
            };


            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                var segments = topic.Split('/');
                if (segments.Length < 4) return;

                var tapId = segments[2];
                var type = segments[3];

                if (type == "amount" && int.TryParse(payload, out int amount))
                {
                    _lastAmounts[tapId] = amount;
                    OnAmountUpdated?.Invoke(tapId, amount);
                    StartAmountMonitor(tapId);
                }
                else if (type == "status" )
                {
                    _CurrentStatuses[tapId] = payload;
                    OnStatusUpdated?.Invoke(tapId, payload);
                }

                await Task.CompletedTask;
            };


            Console.WriteLine("MQTT service started");

        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Clean_Disconnect();
        }


        public async Task SubscribeToTap(string tapId)
        {
            string topic = $"{_topicPrefix}{tapId}/#";
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .Build());
            Console.WriteLine($"Subscribed to:{topic}");
        }

        public async Task PublishTapCommand(string tapId, string message)
        {
            if (_mqttClient is null || !_mqttClient.IsConnected)
            {
                Console.WriteLine("MQTT client not ready, skipping publish");
                return;
            }

            string topic = $"{_topicPrefix}{tapId}/cmd";

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            await _mqttClient.PublishAsync(mqttMessage);
            Console.WriteLine($"Published to: {topic} with message: {message}");
        }


        public async Task AnnounceCurrentUser(string tapId, string userId)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"{_topicPrefix}{tapId}/currentUser")
                .WithPayload(userId)
                .Build();

            if (_mqttClient?.IsConnected == true)
            {
                await _mqttClient.PublishAsync(message);
                Console.WriteLine($"Published current user {userId} to tap {tapId}");
                if(userId == "")
                {
                    PublishTapCommand(tapId, "reset");
                } 
            }
        }

        private readonly Dictionary<string, int> _lastAmounts = new();
        private readonly Dictionary<string, string> _CurrentStatuses= new();
        private readonly Dictionary<string, CancellationTokenSource> _watchdogTokens = new();

        private void StartAmountMonitor(string tapId)
        {
            if (_watchdogTokens.TryGetValue(tapId, out var oldToken))
            {
                oldToken.Cancel();
            }

            var cts = new CancellationTokenSource();
            _watchdogTokens[tapId] = cts;
            _ = Task.Run(async () =>
            {
                int lastAmount = _lastAmounts[tapId];
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(Timeout, cts.Token);
                    if (_lastAmounts.TryGetValue(tapId, out int current) && current == lastAmount && current != 0 && _CurrentStatuses[tapId] == "stopped")
                    {
                        Console.WriteLine($"Tap {tapId} finished pouring.");
                        await _tapQueueManager.DequeueUser(tapId);
                        await PublishTapCommand(tapId, "done");
                        break;
                    }
                    lastAmount = current;
                }
            }, cts.Token);
        }



        public async Task Clean_Disconnect()
        {
            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection).Build());

            Console.WriteLine("MQTT disconnected!");
        }
        public async Task ConnectAsync()
        {
            var mqttFactory = new MqttClientFactory();
            _mqttClient = mqttFactory.CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId(_clientId)
                .WithTcpServer(_host, _port)
                .WithCredentials(_username, _password)
                .WithCleanSession()
                .WithTlsOptions(new MqttClientTlsOptions
                {
                    UseTls = true,
                    AllowUntrustedCertificates = true, // only if you're using self-signed certs
                    CertificateValidationHandler = context => true // optionally accept all certs
                })
                .Build();

            var response = await _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            if (response.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new InvalidOperationException($"MQTT connection failed: {response.ResultCode}");
            }

            Console.WriteLine("MQTT connected!");
        }

    }
}