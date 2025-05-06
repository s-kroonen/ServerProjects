using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System.Text;

public class MqttService : IHostedService
{
    private IMqttClient _mqttClient;
    private readonly string _topicPrefix = "beer/tap/";
    private readonly string _clientId = "Taps";
    private readonly string _host = "wall-e";
    private readonly int _port = 1883;
    private readonly string _username = "public";
    private readonly string _password = "temp-01";

    public async Task SubscribeToTap(string tapId)
    {
        string topic = $"{_topicPrefix}{tapId}/status";
        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
            .WithTopic(topic)
            .Build());
    }

    public async Task PublishCommand(string tapId, string message)
    {
        string topic = $"{_topicPrefix}{tapId}/cmd";

        var mqttMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(message)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        await _mqttClient.PublishAsync(mqttMessage);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConnectAsync();

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            Console.WriteLine($"Received from {topic}: {payload}");

            // Handle the message as needed...
            await Task.CompletedTask;
        };

        await SubscribeToTap("1");

        Console.WriteLine("MQTT service started and subscribed.");

    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        Clean_Disconnect();
        return Task.CompletedTask;
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
