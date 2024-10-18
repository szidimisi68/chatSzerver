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
        private string selectedUser;

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

            // Ask for a port number
            string portInput = Interaction.InputBox("Enter a port number:", "Port", "8080", -1, -1);
            if (!int.TryParse(portInput, out int port))
            {
                MessageBox.Show("Invalid port number.");
                Application.Current.Shutdown();
                return;
            }

            client = new TcpClient("127.0.0.1", port); // Use localhost
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
            if (response.StartsWith("/users:"))
            {
                var users = response.Substring(7).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                RefreshUserList(users);
            }
            else
            {
                DisplayMessage(response);
            }
        }

        private void DisplayMessage(string message)
        {
            string time = DateTime.Now.ToString("HH:mm");
            lbChat.Items.Add($"({time}) {message}");
            lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]);
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string message = tbxMessage.Text.Trim();
            if (!string.IsNullOrWhiteSpace(message))
            {
                string fullMessage;
                if (!string.IsNullOrEmpty(selectedUser))
                {
                    // Send a private message
                    fullMessage = $"@{selectedUser}: {message}";
                }
                else
                {
                    // Public message
                    fullMessage = $"{username}: {message}";
                }

                byte[] data = Encoding.UTF8.GetBytes(fullMessage);
                DisplayMessage(fullMessage);
                stream.Write(data, 0, data.Length);
                tbxMessage.Clear();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
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

        private void lbUsers_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lbUsers.SelectedItem != null)
            {
                selectedUser = lbUsers.SelectedItem.ToString().Split(' ')[0]; // Get username from selected item
            }
        }
    }
}