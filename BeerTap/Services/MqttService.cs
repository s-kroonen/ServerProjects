using MQTTnet;
using MQTTnet.Protocol;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BeerTap.Services
{
    public class MqttService : IHostedService
    {
        public List<string> TapIds = new() { "1", "2" };

        private IMqttClient _mqttClient;
        private readonly TapQueueManager _tapQueueManager;
        private readonly ILogger<MqttService> _logger;

        public event Action<string, float>? OnAmountUpdated;
        public event Action<string, string>? OnStatusUpdated;

        private readonly Dictionary<string, float> _lastAmounts = new();
        private readonly Dictionary<string, string> _CurrentStatuses = new();
        private readonly Dictionary<string, CancellationTokenSource> _amountWatchdogTokens = new();
        private readonly Dictionary<string, CancellationTokenSource> _statusWatchdogTokens = new();


        private const int amountTimeout = 5000;
        private const int statusTimeout = 1000;


        private readonly string _topicPrefix = "beer/tap/";
        private readonly string _clientId = "TapApi";
        private readonly string _host = "kroon-en.nl";
        private readonly int _port = 8883;
        private readonly string _username = "public";
        private readonly string _password = "temp-01";

        public MqttService(TapQueueManager tapQueueManager, ILogger<MqttService> logger)
        {
            _tapQueueManager = tapQueueManager;
            _logger = logger;
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
                _logger.LogDebug("Received MQTT message: Topic={Topic}, Payload={Payload}", topic, payload);

                var segments = topic.Split('/');
                if (segments.Length < 4) return;

                var tapId = segments[2];
                var type = segments[3];

                if (type == "amount" && float.TryParse(payload, out float amount))
                {
                    _lastAmounts[tapId] = amount;
                    OnAmountUpdated?.Invoke(tapId, amount);
                    StartAmountMonitor(tapId);
                }
                else if (type == "status")
                {
                    _CurrentStatuses[tapId] = payload;
                    OnStatusUpdated?.Invoke(tapId, payload);
                    StartStatusMonitor(tapId);
                }

                await Task.CompletedTask;
            };

            _logger.LogInformation("MQTT service started");
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
            _logger.LogInformation("Subscribed to: {Topic}", topic);
        }

        public async Task PublishTapCommand(string tapId, string message)
        {
            if (_mqttClient is null || !_mqttClient.IsConnected)
            {
                _logger.LogWarning("MQTT client not ready, skipping publish");
                return;
            }

            string topic = $"{_topicPrefix}{tapId}/cmd";

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            await _mqttClient.PublishAsync(mqttMessage);
            _logger.LogInformation("Published to: {Topic} with message: {Message}", topic, message);
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
                _logger.LogInformation("Published current user {UserId} to tap {TapId}", userId, tapId);

                if (userId == "")
                {
                    await PublishTapCommand(tapId, "reset");
                }
            }
        }

        private void StartAmountMonitor(string tapId)
        {
            if (_amountWatchdogTokens.TryGetValue(tapId, out var oldToken))
            {
                oldToken.Cancel();
            }

            var cts = new CancellationTokenSource();
            _amountWatchdogTokens[tapId] = cts;
            _ = Task.Run(async () =>
            {
                float lastAmount = _lastAmounts[tapId];
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(amountTimeout, cts.Token);
                    if (_lastAmounts.TryGetValue(tapId, out float current) && current == lastAmount && current != 0 && _CurrentStatuses[tapId] == "stopped")
                    {
                        _logger.LogInformation("Tap {TapId} finished pouring.", tapId);
                        await _tapQueueManager.DequeueUser(tapId);
                        await PublishTapCommand(tapId, "done");
                        break;
                    }
                    lastAmount = current;
                }
            }, cts.Token);
        }

        private void StartStatusMonitor(string tapId)
        {
            if (_statusWatchdogTokens.TryGetValue(tapId, out var oldToken))
            {
                oldToken.Cancel();
            }

            var cts = new CancellationTokenSource();
            _statusWatchdogTokens[tapId] = cts;
            _ = Task.Run(async () =>
            {
                string lastStatus = _CurrentStatuses[tapId];
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(statusTimeout, cts.Token);
                    if (_CurrentStatuses.TryGetValue(tapId, out string current) && current == lastStatus && current == "done")
                    {
                        _logger.LogInformation("Tap {TapId} reset to idle.", tapId);
                        await _tapQueueManager.DequeueUser(tapId);
                        await PublishTapCommand(tapId, "reset");
                        break;
                    }
                    lastStatus = current;
                }
            }, cts.Token);
        }

        public async Task Clean_Disconnect()
        {
            await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder()
                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                .Build());

            _logger.LogInformation("MQTT disconnected!");
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
                    AllowUntrustedCertificates = true,
                    CertificateValidationHandler = context => true
                })
                .Build();

            var response = await _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            if (response.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new InvalidOperationException($"MQTT connection failed: {response.ResultCode}");
            }

            _logger.LogInformation("MQTT connected!");
        }
    }
}
