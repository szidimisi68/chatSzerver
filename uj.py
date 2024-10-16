import socket
import threading

clients = {}


def handle_client(conn, addr):
    print(f'Connected by {addr}')
    username = conn.recv(1024).decode()  # Receive the username from the client
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

            # Forward the formatted message to all other clients
            formatted_message = f"{username}: {message}"
            for client in clients:
                if client != conn:  # Don't send back to the sender
                    client.sendall(formatted_message.encode())

        except ConnectionResetError:
            break

    print(f'Disconnected from {addr}')
    del clients[conn]
    conn.close()


def list_connected_users():
    return [f"{username} ({addr[0]})" for conn, (username, addr) in clients.items()]


def start_server(host='0.0.0.0', port=65432):
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((host, port))
    server_socket.listen(5)
    print(f'Server listening on {host}:{port}...')

    while True:
        conn, addr = server_socket.accept()
        threading.Thread(target=handle_client, args=(conn, addr)).start()


if __name__ == "__main__":
    start_server()