import paho.mqtt.client as mqtt
import psycopg2
import json

# -------------------
# Database setup
# -------------------
DB_CONFIG = {
    "dbname": "postgres",
    "user": "postgres",
    "password": "Datait2025!",
    "host": "192.168.113.12",
    "port": 5432
}

def init_db():
    conn = psycopg2.connect(**DB_CONFIG)
    cursor = conn.cursor()
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS joystick_data (
            id BIGSERIAL PRIMARY KEY,
            topic TEXT,
            x_axis NUMERIC(5,3),
            y_axis NUMERIC(5,3),
            button TEXT,
            timestamp TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
        )
    """)
    conn.commit()
    cursor.close()
    conn.close()

def insert_joystick_data(topic, x, y, button):
    conn = psycopg2.connect(**DB_CONFIG)
    cursor = conn.cursor()
    cursor.execute(
        "INSERT INTO joystick_data (topic, x_axis, y_axis, button) VALUES (%s, %s, %s, %s)",
        (topic, x, y, button)
    )
    conn.commit()
    cursor.close()
    conn.close()

# -------------------
# MQTT setup
# -------------------
BROKER = "192.168.113.12" 
PORT = 1883
TOPIC = "joystick/data" 

def on_connect(client, userdata, flags, rc):
    print(f"Connected with result code {rc}")
    client.subscribe(TOPIC)

def on_message(client, userdata, msg):
    try:
        payload_str = msg.payload.decode("utf-8")
        payload = json.loads(payload_str)

        # Extract joystick fields
        x = payload.get("x")
        y = payload.get("y")
        button = payload.get("button")

        insert_joystick_data(msg.topic, x, y, button)
        print(f"Saved: {msg.topic} -> x={x}, y={y}, button={button}")

    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    init_db()
    client = mqtt.Client()
    client.on_connect = on_connect
    client.on_message = on_message

    client.connect(BROKER, PORT, 60)
    client.loop_forever()
