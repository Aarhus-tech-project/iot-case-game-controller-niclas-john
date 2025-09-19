using System;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text.Json;

namespace GameController
{
    public class MqttHelper
    {
        private readonly MqttClient _client;
        private readonly string _clientId = Guid.NewGuid().ToString();

        public MqttHelper(string broker, int port, string user, string pass)
        {
            _client = new MqttClient(broker, port, false, null, null, MqttSslProtocols.None);
            _client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            _client.Connect(_clientId, user, pass);
        }

        public void Publish(string topic, string payload)
        {
            if (!_client.IsConnected)
                throw new Exception("MQTT client is not connected.");

            _client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
        }

        public void PublishJson(string topic, object data)
        {
            string json = JsonSerializer.Serialize(data);
            Publish(topic, json);
        }

        public void Subscribe(string topic)
        {
            if (!_client.IsConnected)
                throw new Exception("MQTT client is not connected.");

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
            if (_client.IsConnected)
                _client.Disconnect();
        }
    }
}