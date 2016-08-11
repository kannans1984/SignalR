using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using Microsoft.AspNet.SignalR.Client;

namespace WPFClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// This name is simply added to sent messages to identify the user; this 
        /// sample does not include authentication.
        /// </summary>
        public String UserName { get; set; }
        public IHubProxy HubProxy { get; set; }
        const string ServerURI = "http://localhost:8080/signalr";
        public HubConnection Connection { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            if (Connection.State == ConnectionState.Connected)
            {
                HubProxy.Invoke("Send", UserName, TextBoxMessage.Text);
                TextBoxMessage.Text = String.Empty;
                TextBoxMessage.Focus();
            }
        }


        /// <summary>
        /// Creates and connects the hub connection and hub proxy. This method
        /// is called asynchronously from SignInButton_Click.
        /// </summary>
        private void ConnectAsync()
        {
            Connection = new HubConnection(ServerURI, new Dictionary<string, string> { { "userName", UserName } });
            Connection.Closed += Connection_Closed;
            HubProxy = Connection.CreateHubProxy("MyHub");

            //Handle incoming event from server: use Invoke to write to console from SignalR's thread

            HubProxy.On<string, string>("AddMessage", (name, message) =>
            {
                Action action = () =>
                {
                    RichTextBoxConsole.AppendText(String.Format("{0}: {1}\r", name, message));
                };
                var dispatcher = Application.Current.Dispatcher;
                dispatcher.Invoke(action);

            });

            try
            {
                Connection.Start();
            }
            catch (HttpRequestException)
            {
                StatusText.Content = "Unable to connect to server: Start server before connecting clients.";
                //No connection: Don't enable Send button or show chat UI
                return;
            }

            //Show chat UI; hide login UI
            SignInPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            clientUserName.Content = "Name : " + UserName;
            ButtonSend.IsEnabled = true;
            TextBoxMessage.Focus();
            RichTextBoxConsole.AppendText("Connected to server at " + ServerURI + "\r");
        }

        /// <summary>
        /// If the server is stopped, the connection will time out after 30 seconds (default), and the 
        /// Closed event will fire.
        /// </summary>
        void Connection_Closed()
        {
            //Hide chat UI; show login UI
            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(new Func<Visibility>(() => ChatPanel.Visibility = Visibility.Collapsed));
            dispatcher.Invoke(new Func<bool>(() => ButtonSend.IsEnabled = false));
            dispatcher.Invoke(new Func<object>(() => StatusText.Content = "You have been disconnected."));
            dispatcher.Invoke(new Func<Visibility>(() => SignInPanel.Visibility = Visibility.Visible));
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            UserName = UserNameTextBox.Text;
            //Connect to server (use async method to avoid blocking UI thread)
            if (!String.IsNullOrEmpty(UserName))
            {
                StatusText.Visibility = Visibility.Visible;
                StatusText.Content = "Connecting to server...";
                ConnectAsync();
            }
        }

        private void WPFClient_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Connection != null)
            {
                Connection.Stop();
                Connection.Dispose();
            }
        }
    }
}
