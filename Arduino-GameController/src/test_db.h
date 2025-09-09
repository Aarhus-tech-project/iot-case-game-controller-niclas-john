#include <ArduinoJson.h>
#include <WiFi.h>
#include <PubSubClient.h>

// WiFi + MQTT Config
const char *ssid = "h4prog";
const char *password = "1234567890";
const char *mqtt_server = "192.168.113.12";
const int mqtt_port = 1883;
const char *mqtt_user = "postgres";
const char *mqtt_pass = "Datait2025!";

WiFiClient wifiClient;
PubSubClient client(wifiClient);

// WiFi Setup
void setup_wifi()
{
  Serial.print(F("Connecting to "));
  Serial.println(ssid);

  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED)
  {
    delay(1000);
    Serial.print(".");
  }

  delay(1000);
  Serial.println();
  Serial.println(F("WiFi connected"));
  Serial.println(WiFi.localIP());
}

// MQTT Reconnect
void reconnect_mqtt()
{
  while (!client.connected())
  {
    Serial.print(F("Connecting to MQTT broker... "));
    if (client.connect("JoystickClient", mqtt_user, mqtt_pass))
    {
      Serial.println(F("connected!"));
    }
    else
    {
      Serial.print(F("failed, rc="));
      Serial.print(F(client.state()));
      Serial.println(F(" trying again in 5 seconds"));
      delay(5000);
    }
  }
}

// Converts a CSV string to a JSON string with fixed keys
String csvToJson(const String &csv)
{
  StaticJsonDocument<512> doc;
  const char *keys[] = {
      "X", "O", "Firkant", "Trekant", "DU", "DD", "DL", "DR",
      "joyStickBtnLeft", "joyStickBtnRight",
      "rotRX", "rotRY",
      "gameBtn", "startBtn", "left_l1", "left_l2", "right_r1", "right_r2"};
  // Indices of button fields in the CSV (first 10 and last 6)
  int buttonIndices[] = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 14, 15, 16, 17, 18, 19};
  int p = 0, q = 0, i = 0, j = 0;
  for (; i < 20 && p < csv.length(); i++)
  {
    // Skip rotLX and rotLY (positions 10 and 11)
    if (i == 10 || i == 11)
    {
      q = csv.indexOf(',', p);
      p = (q == -1 ? csv.length() : q + 1);
      continue;
    }
    q = csv.indexOf(',', p);
    String v = csv.substring(p, q == -1 ? csv.length() : q);
    // Only map button fields to "pressed" or "0"
    bool isButton = false;
    for (int k = 0; k < 16; k++)
    {
      if (i == buttonIndices[k])
      {
        isButton = true;
        break;
      }
    }
    if (isButton)
    {
      doc[keys[j]] = (v == "1") ? "pressed" : "0";
    }
    else
    {
      doc[keys[j]] = v;
    }
    p = (q == -1 ? csv.length() : q + 1);
    j++;
  }
  String json;
  serializeJson(doc, json);
  return json;
}