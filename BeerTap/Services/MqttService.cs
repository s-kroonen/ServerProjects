using MQTTnet;
using MQTTnet.Protocol;
using System.Text;
using Microsoft.Extensions.Logging;
using BeerTap.Models;
using Microsoft.EntityFrameworkCore;
using BeerTap.Data;

namespace BeerTap.Services
{
    public class MqttService : IHostedService
    {
        //public List<string> TapIds = new() { "1", "2" };

        private IMqttClient _mqttClient;
        private readonly TapQueueManager _tapQueueManager;
        private readonly ILogger<MqttService> _logger;
        private readonly BeerTapContext _context;
        private readonly IServiceScopeFactory _scopeFactory;


        public event Action<Guid, float>? OnAmountUpdated;
        public event Action<Guid, string>? OnStatusUpdated;

        //tap id to other info
        private readonly Dictionary<Guid, CancellationTokenSource> _amountWatchdogTokens = new();
        private readonly Dictionary<Guid, CancellationTokenSource> _statusWatchdogTokens = new();
        private readonly Dictionary<Guid, Guid> _activeSessions = new();
        private readonly Dictionary<Guid, float> _lastAmounts = new();
        private readonly Dictionary<Guid, string> _CurrentStatuses = new();
        private readonly Dictionary<Guid, Guid> _CurrentUsers = new();

        private readonly Dictionary<string, Guid> _topicToTapId = new();
        private readonly Dictionary<Guid, string> _tapIdToTopic = new();


        private const int amountTimeout = 5000;
        private const int statusTimeout = 1000;


        private readonly string _topicPrefix = "beer/tap/";
        private readonly string _clientId = "TapApi";
        private readonly string _host = "kroon-en.nl";
        private readonly int _port = 8883;
        private readonly string _username = "public";
        private readonly string _password = "temp-01";

        public MqttService(TapQueueManager tapQueueManager, ILogger<MqttService> logger, IServiceScopeFactory scopeFactory)
        {
            _tapQueueManager = tapQueueManager;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BeerTapContext>();


            await ConnectAsync();
            _tapQueueManager.CurrentUserChanged += async (tapId, user) =>
            {
                await AnnounceCurrentUser(tapId, user);
                if (user == null)
                    _CurrentUsers.Remove(tapId);
                else
                    _CurrentUsers[tapId] = user.ID;
            };
            _tapQueueManager.StopTapSession += async (tapId) =>
            {
                await PublishTapCommand(_tapIdToTopic[tapId], "done");
            };
            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                using var scope = _scopeFactory.CreateScope();
                var _context = scope.ServiceProvider.GetRequiredService<BeerTapContext>();

                var taps = await context.Taps.ToListAsync();
                foreach (var tap in taps)
                {
                    if (!string.IsNullOrWhiteSpace(tap.Topic))
                    {
                        _topicToTapId[tap.Topic] = tap.Id;
                        _tapIdToTopic[tap.Id] = tap.Topic;
                        await SubscribeToTap(tap.Topic); // uses the topic string (e.g. "1", "abc123")
                    }
                }

                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                _logger.LogDebug("Received MQTT message: Topic={Topic}, Payload={Payload}", topic, payload);

                var segments = topic.Split('/');
                if (segments.Length < 4) return;

                var tapTopic = segments[2]; // string like "1"
                var type = segments[3];

                // Translate topic to tapId (Guid)
                if (!_topicToTapId.TryGetValue(tapTopic, out var tapId))
                {
                    _logger.LogWarning("Unknown tap topic: {TapTopic}", tapTopic);
                    return;
                }

                if (type == "amount" && float.TryParse(payload, out float amount))
                {
                    OnAmountUpdated?.Invoke(tapId, amount);
                    StartAmountMonitor(tapId);

                    // Save TapEvent only if a session is active
                    if (_activeSessions.TryGetValue(tapId, out var sessionId))
                    {
                        var tapEvent = new TapEvent
                        {
                            Id = Guid.NewGuid(),
                            SessionId = sessionId,
                            Timestamp = DateTime.UtcNow,
                            Amount = amount
                        };

                        _context.TapEvents.Add(tapEvent);
                        await _context.SaveChangesAsync();
                    }
                    _lastAmounts[tapId] = amount;
                }
                else if (type == "status")
                {
                    _CurrentStatuses[tapId] = payload;
                    OnStatusUpdated?.Invoke(tapId, payload);
                    StartStatusMonitor(tapId);

                    if (payload == "pouring" || payload == "stopped")
                    {
                        if (!_activeSessions.ContainsKey(tapId))
                        {
                            // Start new session
                            var newSession = new TapSession
                            {
                                Id = Guid.NewGuid(),
                                TapId = tapId,
                                StartTime = DateTime.UtcNow,
                                StopTime = DateTime.MinValue, // will be updated
                                TotalAmount = 0,
                                UserId = _CurrentUsers[tapId]
                            };

                            _context.TapSessions.Add(newSession);
                            await _context.SaveChangesAsync();

                            _activeSessions[tapId] = newSession.Id;
                        }
                    }
                    else if (payload == "done")
                    {
                        if (_activeSessions.TryGetValue(tapId, out var sessionId))
                        {
                            var session = await _context.TapSessions.FindAsync(sessionId);
                            if (session != null)
                            {
                                session.StopTime = DateTime.UtcNow;

                                session.TotalAmount = await _context.TapEvents
                                    .Where(e => e.SessionId == sessionId)
                                    .OrderByDescending(e => e.Timestamp)
                                    .Select(e => e.Amount)
                                    .FirstOrDefaultAsync();
                                await _context.SaveChangesAsync();
                            }


                            _activeSessions.Remove(tapId);
                        }
                    }
                }

                await Task.CompletedTask;
            };


            _logger.LogInformation("MQTT service started");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Clean_Disconnect();
        }

        public async Task SubscribeToTap(string tapTopic)
        {
            string topic = $"{_topicPrefix}{tapTopic}/#";
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .Build());
            _logger.LogInformation("Subscribed to: {Topic}", topic);
        }

        public async Task PublishTapCommand(string tapTopic, string message)
        {
            if (_mqttClient is null || !_mqttClient.IsConnected)
            {
                _logger.LogWarning("MQTT client not ready, skipping publish");
                return;
            }

            string topic = $"{_topicPrefix}{tapTopic}/cmd";

            var mqttMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            await _mqttClient.PublishAsync(mqttMessage);
            _logger.LogInformation("Published to: {Topic} with message: {Message}", topic, message);
        }

        public async Task AnnounceCurrentUser(Guid tapId, User? user)
        {

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"{_topicPrefix}{tapId}/currentUser")
                .WithPayload(user?.UserId.ToString() ?? string.Empty)
                .Build();

            if (_mqttClient?.IsConnected == true)
            {
                await _mqttClient.PublishAsync(message);
                _logger.LogInformation("Published current user {UserId} to tap {TapId}", user, tapId);

                if (user == null)
                {
                    await PublishTapCommand(_tapIdToTopic[tapId], "reset");
                }
            }
        }

        private void StartAmountMonitor(Guid tapId)
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
                        await PublishTapCommand(_tapIdToTopic[tapId], "done");
                        await _tapQueueManager.DequeueUser(tapId);
                        break;
                    }
                    lastAmount = current;
                }
            }, cts.Token);
        }

        private void StartStatusMonitor(Guid tapId)
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
                        await PublishTapCommand(_tapIdToTopic[tapId], "reset");
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
