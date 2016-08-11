using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;

namespace WPFServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IDisposable SignalR { get; set; }
        const string ServerURI = "http://localhost:8080";

        public ObservableCollection<UserDetails> UsersListCollection;
        public MainWindow()
        {
            InitializeComponent();
            UsersListCollection = new ObservableCollection<UserDetails>();
            this.usersList.ItemsSource = UsersListCollection;
            this.sendMsgBtn.IsEnabled = false;
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            WriteToConsole("Starting server...");
            StartButton.IsEnabled = false;
            Task.Run(() => StartServer());
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            SignalR.Dispose();
            Close();
        }

        private void UsersList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count != 0)
            {
                this.sendMsgBtn.IsEnabled = true;
            }
        }

        private void SendMsgBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<MyHub>();
            if (hub != null)
            {
                hub.Clients.Client(((UserDetails)this.usersList.SelectedItem).UserConnectionId)
                .addMessage("Server", messageTB.Text);
            }
        }


        private void StartServer()
        {
            try
            {
                SignalR = WebApp.Start(ServerURI);
            }
            catch (TargetInvocationException)
            {
                WriteToConsole("A server is already running at " + ServerURI);
                this.Dispatcher.Invoke(() => StartButton.IsEnabled = true);
                return;
            }
            this.Dispatcher.Invoke(() => StopButton.IsEnabled = true);
            WriteToConsole("Server started at " + ServerURI);
        }


        ///This method adds a line to the RichTextBoxConsole control, using Dispatcher.Invoke if used
        /// from a SignalR hub thread rather than the UI thread.
        public void WriteToConsole(String message)
        {
            if (!(this.RichTextBoxConsole.CheckAccess()))
            {
                this.Dispatcher.Invoke(() =>
                    WriteToConsole(message)
                );
                return;
            }
            this.RichTextBoxConsole.AppendText(message + "\r");
        }

        private void CreateClient_OnClick(object sender, RoutedEventArgs e)
        {
            string path = @"E:\Works\SignalRChat\SignalRChat\Executables\{0}";
            string executableName = "WPFClient.exe";
            Process.Start(string.Format(path, executableName));
        }
    }

    /// <summary>
    /// Used by OWIN's startup process. 
    /// </summary>
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
    /// <summary>
    /// Echoes messages sent using the Send message by calling the
    /// addMessage method on the client. Also reports to the console
    /// when clients connect and disconnect.
    /// </summary>
    public class MyHub : Hub
    {
        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
        }

        public override Task OnConnected()
        {
            //Use Application.Current.Dispatcher to access UI thread from outside the MainWindow class
            Application.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)Application.Current.MainWindow).WriteToConsole("Client connected: " + Context.ConnectionId);
                var userDetail = new UserDetails() { UserConnectionId = Context.ConnectionId, UserName = Context.QueryString["userName"] };
                ((MainWindow)Application.Current.MainWindow).UsersListCollection.Add(userDetail);

            });

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow) Application.Current.MainWindow).WriteToConsole("Client disconnected: " +
                                                                             Context.ConnectionId);
                var userdetails = ((MainWindow) Application.Current.MainWindow).UsersListCollection.FirstOrDefault(
                    x => x.UserConnectionId.Equals(Context.ConnectionId));
                ((MainWindow) Application.Current.MainWindow).UsersListCollection.Remove(userdetails);

            });
            return base.OnDisconnected(stopCalled);
        }
    
    }
}
