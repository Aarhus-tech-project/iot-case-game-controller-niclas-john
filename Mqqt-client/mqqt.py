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
            X TEXT,
            O TEXT,
            Firkant TEXT,
            Trekant TEXT,
            DU TEXT,
            DD TEXT,
            DL TEXT,
            DR TEXT,
            joyStickBtnLeft TEXT,
            joyStickBtnRight TEXT,
            rotRX TEXT,
            rotRY TEXT,
            gameBtn TEXT,
            startBtn TEXT,
            left_l1 TEXT,
            left_l2 TEXT,
            right_r1 TEXT,
            right_r2 TEXT,
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
        "X", "O", "Firkant", "Trekant", "DU", "DD", "DL", "DR",
        "joyStickBtnLeft", "joyStickBtnRight",
        "rotRX", "rotRY",
        "gameBtn", "startBtn", "left_l1", "left_l2", "right_r1", "right_r2"
    ]
    values = [payload.get(f) for f in fields]
    cursor.execute(
        f"INSERT INTO controller_data (topic, {', '.join(fields)}) VALUES (%s, {', '.join(['%s']*len(fields))})",
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
