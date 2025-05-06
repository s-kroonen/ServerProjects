using BeerTap.Services;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text;

public class MqttService : IHostedService
{
    private IMqttClient _mqttClient;
    private readonly TapQueueManager _tapQueueManager;
    private readonly UserService _userService;

    private readonly string _topicPrefix = "beer/tap/";
    private readonly string _clientId = "TapApi";
    private readonly string _host = "wall-e";
    private readonly int _port = 1883;
    private readonly string _username = "public";
    private readonly string _password = "temp-01";
    public MqttService(TapQueueManager tapQueueManager, UserService userService)
    {
        _tapQueueManager = tapQueueManager;
        _userService = userService;
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

            Console.WriteLine($"Received from {topic}: {payload}");

            // Handle the message as needed...
            if (topic.EndsWith("/amount"))
            {
                var tapId = topic.Split('/')[2];
                var amount = int.Parse(payload);

                var user = _tapQueueManager.DequeueUser(tapId);
                if (user != null)
                {
                    await _userService.UpdateUserScoreAsync(user.UserId, amount);
                    Console.WriteLine($"User {user.UserId} scored {amount} ml on tap {tapId}");
                }
            }
            await Task.CompletedTask;
        };

        Console.WriteLine("MQTT service started");

    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        Clean_Disconnect();
        return Task.CompletedTask;
    }


    public async Task SubscribeToTap(string tapId)
    {
        string topic = $"{_topicPrefix}{tapId}/status";
        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .Build());
        Console.WriteLine($"Subscribed to:{topic}");
    }

    public async Task PublishTapCommand(string tapId, string message)
    {
        string topic = $"{_topicPrefix}{tapId}/cmd";

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(message)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        await _mqttClient.PublishAsync(mqttMessage);
        Console.WriteLine($"Published to:{topic} with meassage:{message}");
    }

    public async Task AnnounceCurrentUser(string tapId, string userId)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"tap/{tapId}/currentUser")
            .WithPayload(userId)
            .Build();

        if (_mqttClient?.IsConnected == true)
        {
            await _mqttClient.PublishAsync(message);
            Console.WriteLine($"Published current user {userId} to tap {tapId}");
        }
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
            .Build();

        var response = await _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        if (response.ResultCode != MqttClientConnectResultCode.Success)
        {
            throw new InvalidOperationException($"MQTT connection failed: {response.ResultCode}");
        }

        Console.WriteLine("MQTT connected!");
    }

}
