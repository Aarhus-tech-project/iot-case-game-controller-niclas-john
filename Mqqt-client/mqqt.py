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
        CREATE TABLE IF NOT EXISTS controller_data (
            id BIGSERIAL PRIMARY KEY,
            topic TEXT,
            "A" BOOL,
            "B" BOOL,
            "X" BOOL,
            "Y" BOOL,
            "DU" BOOL,
            "DD" BOOL,
            "DL" BOOL,
            "DR" BOOL,
            "JoyLBtnState" BOOL,
            "JoyRBtnState" BOOL,
            "JoyLX" INT,
            "JoyLY" INT,
            "JoyRX" INT,
            "JoyRY" INT,
            "GameButton" BOOL,
            "Start" BOOL,
            "L1" BOOL,
            "L2" BOOL,
            "R1" BOOL,
            "R2" BOOL,
            "rumble" INT,
            timestamp TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
        )
    """)
    conn.commit()
    cursor.close()
    conn.close()

def insert_controller_data(topic, payload):
    conn = psycopg2.connect(**DB_CONFIG)
    cursor = conn.cursor()
    fields = [
    "A", "B", "X", "Y", "DU", "DD", "DL", "DR",
    "JoyLBtnState", "JoyRBtnState",
    "JoyLX", "JoyLY", "JoyRX", "JoyRY",
    "GameButton", "Start", "L1", "L2", "R1", "R2", "rumble"
]
    int_fields = {"JoyLX", "JoyLY", "JoyRX", "JoyRY", "rumble"}
    bool_fields = {"A", "B", "X", "Y", "DU", "DD", "DL", "DR", "JoyLBtnState", "JoyRBtnState", "GameButton", "Start", "L1", "L2", "R1", "R2"}
    values = []
    for f in fields:
        if f in int_fields:
            try:
                values.append(int(payload.get(f, 0)))
            except (ValueError, TypeError):
                values.append(0)
        elif f in bool_fields:
            v = payload.get(f, False)
            if isinstance(v, str):
                v = v.lower() in ("true", "1", "yes", "on")
            values.append(bool(v))
        else:
            values.append(None)
    quoted_fields = [f'"{f}"' for f in fields]
    cursor.execute(
        f"INSERT INTO controller_data (topic, {', '.join(quoted_fields)}) VALUES (%s, {', '.join(['%s']*len(fields))})",
        [topic] + values
    )
    conn.commit()
    cursor.close()
    conn.close()

# -------------------
# MQTT setup
# -------------------

BROKER = "192.168.113.12" 
PORT = 1883
TOPIC = "controller/data" 

def on_connect(client, userdata, flags, rc):
    print(f"Connected with result code {rc}")
    client.subscribe(TOPIC)

def on_message(client, userdata, msg):
    try:
        payload_str = msg.payload.decode("utf-8")
        payload = json.loads(payload_str)
        insert_controller_data(msg.topic, payload)
        print(f"Saved: {msg.topic} -> {payload}")
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    init_db()
    client = mqtt.Client()
    client.on_connect = on_connect
    client.on_message = on_message

    client.connect(BROKER, PORT, 60)
    client.loop_forever()