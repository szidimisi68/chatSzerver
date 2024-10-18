import socket
import threading

clients = {}

def handle_client(conn, addr):
    print(f'Connected by {addr}')
    username = conn.recv(1024).decode()
    clients[conn] = (username, addr)

    while True:
        try:
            data = conn.recv(1024)
            if not data:
                break

            message = data.decode()
            print(f'Received from {username}: {message}')

            # Handle user list request
            if message == "/users":
                user_list = list_connected_users()
                response = "/users:\n" + "\n".join(user_list)
                conn.sendall(response.encode())
                continue

            # Check for private messages
            if message.startswith('@'):
                target_username, msg = message.split(': ', 1)[0][1:], message.split(': ', 1)[1]
                send_private_message(target_username, f"{username} (private): {msg}")
            else:
                # Broadcast the message to all other clients
                formatted_message = f"{username}: {message}"
                broadcast(formatted_message, conn)

        except ConnectionResetError:
            break

    print(f'Disconnected from {addr}')
    del clients[conn]
    conn.close()

def list_connected_users():
    return [username for username, (username, _) in clients.items()]

def send_private_message(target_username, message):
    for client, (username, _) in clients.items():
        if username == target_username:
            client.sendall(message.encode())
            break

def broadcast(message, sender_conn):
    for client in clients:
        if client != sender_conn:
            client.sendall(message.encode())

def start_server(host='0.0.0.0', port=8080):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((host, port))
    server_socket.listen(5)
    print(f'Server listening on {host}:{port}...')

    while True:
        conn, addr = server_socket.accept()
        threading.Thread(target=handle_client, args=(conn, addr)).start()

if __name__ == "__main__":
    start_server()  # Start the server on port 8080
