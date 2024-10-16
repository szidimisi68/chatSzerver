using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using Microsoft.VisualBasic; // For InputBox

namespace chat
{
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private string username;

        public MainWindow()
        {
            InitializeComponent();
            AskForUsername();
            StartListening();
        }

        private void AskForUsername()
        {
            username = Interaction.InputBox("Enter your username:", "Username", "User", -1, -1);
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Username cannot be empty.");
                Application.Current.Shutdown();
                return;
            }

            client = new TcpClient("192.168.5.6", 65432); // Replace with your server's IP address
            stream = client.GetStream();

            // Send the username to the server
            byte[] usernameData = Encoding.UTF8.GetBytes(username);
            stream.Write(usernameData, 0, usernameData.Length);
        }

        private void StartListening()
        {
            Thread listenerThread = new Thread(ListenForMessages);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        private void ListenForMessages()
        {
            byte[] responseData = new byte[1024];
            while (true)
            {
                try
                {
                    int bytes = stream.Read(responseData, 0, responseData.Length);
                    if (bytes == 0) break;

                    string responseMessage = Encoding.UTF8.GetString(responseData, 0, bytes);
                    Dispatcher.Invoke(() => HandleServerResponse(responseMessage));
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Listener exception: {e.Message}");
                    break;
                }
            }
        }

        private void HandleServerResponse(string response)
        {
            // Check if the response is a list of users or a message
            if (response.StartsWith("/users:"))
            {
                // This means the server is sending back the list of users
                var users = response.Substring(7).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                RefreshUserList(users);
            }
            else
            {
                // Regular chat message
                DisplayMessage(response);
            }
        }

        private void DisplayMessage(string message)
        {
            string time = DateTime.Now.ToString("HH:mm");
            lbChat.Items.Add($"({time}) {message}");
            lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]); // Auto-scroll to the bottom
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string message = tbxMessage.Text.Trim();
            if (!string.IsNullOrWhiteSpace(message))
            {
                // Prepend the username to the message
                string fullMessage = $"{username}: {message}";
                byte[] data = Encoding.UTF8.GetBytes(fullMessage);

                // Display the message immediately in the chat
                DisplayMessage(fullMessage);

                stream.Write(data, 0, data.Length);
                tbxMessage.Clear(); // Clear the textbox after sending
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // Request connected users from the server
            byte[] requestData = Encoding.UTF8.GetBytes("/users");
            stream.Write(requestData, 0, requestData.Length);
        }

        private void RefreshUserList(string[] users)
        {
            lbUsers.Items.Clear();
            foreach (var user in users)
            {
                lbUsers.Items.Add(user);
            }
        }
    }
}