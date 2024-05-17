using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Timers;
using System.Threading;

namespace WpfAppTelefon3
{
    public partial class Window1 : Window
    {
        Socket socket;
        string username;
        CancellationTokenSource cts = new CancellationTokenSource();
        List<string> users = new List<string>();

        public Window1(string username)
        {
            InitializeComponent();
            this.username = username;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.Connect("26.191.190.98", 2000);




            
            var joinMessage = $"JOIN {this.username}";
            var joinBytes = Encoding.UTF8.GetBytes(joinMessage);
            socket.Send(joinBytes);

            
            ReceiveMessage(cts.Token);
        }


        private async void SendMessage(string message, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            if (message == "/disconnect")
            {
                var disconnectMessage = $"[{this.username}] покинул чат.";
                var disconnectBytes = Encoding.UTF8.GetBytes(disconnectMessage);
                await socket.SendAsync(disconnectBytes, SocketFlags.None, token);

                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }

                Dispatcher.Invoke(() =>
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                });

                Dispatcher.Invoke(() => this.Close());
                return;
            }

            string timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            var formattedMessage = $"[{timestamp} {this.username}]: {message}";
            var bytes = Encoding.UTF8.GetBytes(formattedMessage);

            await socket.SendAsync(bytes, SocketFlags.None, token);
        }

        private async void ReceiveMessage(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    byte[] bytes = new byte[1024];
                    await socket.ReceiveAsync(bytes, SocketFlags.None, token);
                    string message = Encoding.UTF8.GetString(bytes);

                    
                    if (message.StartsWith("JOIN"))
                    {
                        
                        string newUsername = message.Substring(5).Trim();
                        users.Add(newUsername);
                        UpdateUsersList(); 
                    }
                    else if (message.StartsWith("LEAVE"))
                    {
                        
                        string leavingUsername = message.Substring(6).Trim();
                        users.Remove(leavingUsername);
                        UpdateUsersList(); 
                    }
                    else
                    {
                        
                        Dispatcher.Invoke(() => List.Items.Add(message));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Dispatcher.Invoke(() => List.Items.Add("Получение сообщений было отменено."));
            }
            catch (SocketException ex)
            {
                Dispatcher.Invoke(() => List.Items.Add($"Ошибка сокета: {ex.Message}"));
            }
        }

        
        private void UpdateUsersList()
        {
            Dispatcher.Invoke(() =>
            {
                ListUsers.Items.Clear();
                foreach (string user in users)
                {
                    ListUsers.Items.Add(user);
                }
            });
        }

        private void Button1_Click_1(object sender, RoutedEventArgs e)
        {
            SendMessage(Text1.Text, cts.Token);
        }

        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            
            var leaveMessage = $"LEAVE {this.username}";
            var leaveBytes = Encoding.UTF8.GetBytes(leaveMessage);
            await socket.SendAsync(leaveBytes, SocketFlags.None, cts.Token);

            cts.Cancel();

            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                MainWindow window216 = new MainWindow();
                window216.Show();
                this.Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

           
            DisconnectFromChat();
        }

        private async void DisconnectFromChat()
        {
            try
            {
                if (socket.Connected)
                {
                   
                    var leaveMessage = $"LEAVE {this.username}";
                    var leaveBytes = Encoding.UTF8.GetBytes(leaveMessage);
                    await socket.SendAsync(leaveBytes, SocketFlags.None, cts.Token);

                   
                    cts.Cancel();

                    
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();

                    
                    Dispatcher.Invoke(() =>
                    {
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close(); 
                    });
                }
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}