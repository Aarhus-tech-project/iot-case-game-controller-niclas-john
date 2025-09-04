// #include <WiFiS3.h>
// #include <PubSubClient.h>
// #include <Wire.h>
// #include <ArduinoJson.h>

// // -------------------
// // WiFi + MQTT Config
// // -------------------
// const char *ssid = "h4prog";
// const char *password = "1234567890";
// const char *mqtt_server = "192.168.113.12";
// const int mqtt_port = 1883;
// const char *mqtt_user = "postgres";
// const char *mqtt_pass = "Datait2025!";

// WiFiClient wifiClient;
// PubSubClient client(wifiClient);

// // -------------------
// // Joystick Pins
// // -------------------
// const int JOY_X = A0;
// const int JOY_Y = A1;
// const int BUTTON_PIN = 2; // using pull-up

// // -------------------
// // Helper: Normalize raw joystick value (0–700) to -1.0 – 1.0
// // -------------------
// float normalize(int rawValue, int maxValue = 700)
// {
//   float mid = maxValue / 2.0;
//   return (rawValue - mid) / mid;
// }

// // -------------------
// // WiFi Setup
// // -------------------
// void setup_wifi()
// {
//   Serial.print("Connecting to ");
//   Serial.println(ssid);

//   WiFi.begin(ssid, password);
//   while (WiFi.status() != WL_CONNECTED)
//   {
//     delay(1000);
//     Serial.print(".");
//   }

//   delay(1000);
//   Serial.println();
//   Serial.println("WiFi connected");
//   Serial.println(WiFi.localIP());
// }

// // -------------------
// // MQTT Reconnect
// // -------------------
// void reconnect_mqtt()
// {
//   while (!client.connected())
//   {
//     Serial.print("Connecting to MQTT broker... ");
//     if (client.connect("JoystickClient", mqtt_user, mqtt_pass))
//     {
//       Serial.println("connected!");
//     }
//     else
//     {
//       Serial.print("failed, rc=");
//       Serial.print(client.state());
//       Serial.println(" trying again in 5 seconds");
//       delay(5000);
//     }
//   }
// }

// // -------------------
// // Setup
// // -------------------
// void setup()
// {
//   Serial.begin(9600);
//   delay(100);

//   setup_wifi();
//   client.setServer(mqtt_server, mqtt_port);

//   pinMode(BUTTON_PIN, INPUT_PULLUP);
// }

// // -------------------
// // Main Loop
// // -------------------
// void loop()
// {
//   if (!client.connected())
//   {
//     reconnect_mqtt();
//   }
//   client.loop();

//   // Read joystick values
//   int rawX = analogRead(JOY_X);
//   int rawY = analogRead(JOY_Y);
//   int btnState = digitalRead(BUTTON_PIN);

//   // Normalize joystick values
//   float xNorm = normalize(rawX);
//   float yNorm = normalize(rawY);

//   // Create JSON payload
//   StaticJsonDocument<200> doc;
//   doc["x"] = xNorm;
//   doc["y"] = yNorm;
//   doc["button"] = (btnState == LOW) ? "pressed" : "released";

//   char buffer[200];
//   size_t n = serializeJson(doc, buffer);

//   // Debug print
//   Serial.println(buffer);

//   // Publish to MQTT
//   client.publish("joystick/data", buffer, n);

//   delay(1000);
// }
