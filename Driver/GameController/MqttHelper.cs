using System.Text.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;


namespace GameController { 
public class MqttHelper
{
    private readonly MqttClient _client;
    private readonly string _clientId = Guid.NewGuid().ToString();
    private bool _connected = false;

    public MqttHelper(string broker, int port, string user, string pass)
    {
        try
        {
            _client = new MqttClient(broker, port, false, null, null, MqttSslProtocols.None);
            _client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            _client.Connect(_clientId, user, pass);
            _connected = _client.IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MQTT connection failed: {ex.Message}");
            _connected = false;
        }
    }

    public void Publish(string topic, string payload)
    {
        if (!_connected || !_client.IsConnected)
            return; // Silently skip if not connected

        _client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
    }

    public void PublishJson(string topic, object data)
    {
        string json = JsonSerializer.Serialize(data);
        Publish(topic, json);
    }

    public void Subscribe(string topic)
    {
        if (!_connected || !_client.IsConnected)
            return;

        _client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
    }

    private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string receivedTopic = e.Topic;
        string receivedMessage = System.Text.Encoding.UTF8.GetString(e.Message);
        Console.WriteLine($"Received message on topic '{receivedTopic}': {receivedMessage}");
    }

    public void Disconnect()
    {
        if (_connected && _client.IsConnected)
            _client.Disconnect();
    }
}
}