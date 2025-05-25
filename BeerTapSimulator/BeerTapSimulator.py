import paho.mqtt.client as mqtt
import ssl
import time

# MQTT Configuration
broker = "kroon-en.nl"
port = 8883
username = "public"
password = "temp-01"

id = "1"
cmd_topic = f"beer/tap/{id}/cmd"
status_topic = f"beer/tap/{id}/status"
amount_topic = f"beer/tap/{id}/amount"

# Tap states
IDLE, POURING, STOPPED, DONE = "idle", "pouring", "stopped", "done"
current_state = IDLE

# Pouring state machine
tapped_amounts = [2.3, 3.1, 1.2, 4.7]
pouring_index = 0
pouring_total = 0
last_pour_time = 0
pouring_active = False
pouring_interval = 0.5  # seconds between pours

def on_connect(client, userdata, flags, rc):
    print("Connected:", rc)
    client.subscribe(cmd_topic)

def on_message(client, userdata, msg):
    global current_state, pouring_active, pouring_index, pouring_total
    message = msg.payload.decode()
    print(f"Received command: {message}")

    if message == "start" and current_state == IDLE:
        current_state = POURING
        pouring_active = True
        pouring_index = 0
        pouring_total = 0
        client.publish(status_topic, POURING)
        print("Started pouring")
    elif message == "done":
        current_state = DONE
        pouring_active = False
        client.publish(status_topic, DONE)
        client.publish(amount_topic, "0")
    elif message == "reset":
        current_state = IDLE
        pouring_active = False
        client.publish(status_topic, IDLE)
        client.publish(amount_topic, "0")
    elif message.startswith("display:"):
        print("Display:", message[8:])
    else:
        print("Unknown command:", message)

def simulate_pour_step():
    global pouring_index, pouring_total, pouring_active, current_state, last_pour_time
    now = time.time()
    if not pouring_active or pouring_index >= len(tapped_amounts):
        return

    if now - last_pour_time >= pouring_interval:
        pouring_total += tapped_amounts[pouring_index]
        client.publish(amount_topic, str(pouring_total))
        print(f"Poured: {pouring_total:.2f} g")
        pouring_index += 1
        last_pour_time = now

        if pouring_index >= len(tapped_amounts):
            current_state = STOPPED
            pouring_active = False
            client.publish(status_topic, STOPPED)
            print("Pouring stopped")

# MQTT client setup
client = mqtt.Client()
client.username_pw_set(username, password)
client.tls_set(cert_reqs=ssl.CERT_NONE)
client.tls_insecure_set(True)

client.on_connect = on_connect
client.on_message = on_message

client.connect(broker, port, 60)

# Custom loop
try:
    while True:
        client.loop(timeout=0.1)
        simulate_pour_step()
        time.sleep(0.05)  # small sleep to avoid high CPU usage
except KeyboardInterrupt:
    client.disconnect()
