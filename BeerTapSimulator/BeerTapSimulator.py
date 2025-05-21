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

# Simulated tapped weights
tapped_amounts = [2.3, 3.1, 1.2, 4.7]  # in grams

# MQTT callbacks
def on_connect(client, userdata, flags, rc):
    print("Connected:", rc)
    client.subscribe(cmd_topic)

def on_message(client, userdata, msg):
    global current_state
    message = msg.payload.decode()
    print(f"Received command: {message}")

    if message == "start" and current_state == IDLE:
        current_state = POURING
        client.publish(status_topic, POURING)
        simulate_pour()
    elif message == "done":
        current_state = DONE
        client.publish(status_topic, DONE)
        client.publish(amount_topic, "0")
    elif message == "reset":
        current_state = IDLE
        client.publish(status_topic, IDLE)
        client.publish(amount_topic, "0")
    elif message.startswith("display:"):
        print("Display:", message[8:])
    else:
        print("Unknown command:", message)

def simulate_pour():
    global current_state
    total = 0
    #client.loop_start()
    for amount in tapped_amounts:
        total += amount
        client.publish(amount_topic, str(total))
        client.loop()
        print(f"Poured: {total:.2f} g")
        time.sleep(1.5)

    current_state = STOPPED
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

# Start loop
try:
    client.loop_forever()
    #client.loop()
    #time.sleep(0.1)
except KeyboardInterrupt:
    client.disconnect()

