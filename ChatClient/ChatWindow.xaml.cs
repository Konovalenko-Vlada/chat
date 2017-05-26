using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;

namespace ChatClient
{
    /// <summary>
    /// Логика взаимодействия для ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        int ChatRowCount = 0;
        String Username;
        IPEndPoint Address;
        DispatcherTimer SyncTimer;


        public ChatWindow(String InputUsername, IPEndPoint InputAddress)
        {
            InitializeComponent();

            Username = InputUsername;
            Address = InputAddress;

            SyncTimer = new DispatcherTimer();
            SyncTimer.Tick += new EventHandler(SyncTimer_Tick);
            SyncTimer.Interval = new TimeSpan(0, 0, 10);
            SyncTimer.Start();
        }

        private String SendCommand(String Name, String Param = " ")
        {
            /*
             * SendCommand работает не так, как должна:
             * Окно чата должно держать и устанавливать содеинение,
             * а SendComma - управлять отправкой и получением, 
             * но, вместо этого, по какой-то неведомой мне причине,
             * мне приходится каждый раз устанавлюивать соединение.
             */ 
            Socket ChatSock = new Socket(Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ChatSock.Connect(Address);

            String CallString = String.Format("{0} {1}<EOF>", Name, Param);
            ChatSock.Send(Encoding.UTF8.GetBytes(CallString));

            StringBuilder Builder = new StringBuilder();
            int BytesRec = 0;
            byte[] data = new byte[1024];
            do
            {
                BytesRec = ChatSock.Receive(data, data.Length, 0);
                Builder.Append(Encoding.UTF8.GetString(data, 0, BytesRec));
            }
            while (ChatSock.Available > 0);

            ChatSock.Shutdown(SocketShutdown.Both);
            ChatSock.Close();

            return Builder.ToString();
        }

        private void SyncHistory()
        {
            int ServerRowCount = Convert.ToInt32(SendCommand("COUNT"));
            if (ChatRowCount != ServerRowCount)
            {
                ChatBox.Text = SendCommand("HISTORY", Convert.ToString(ServerRowCount - ChatRowCount));
                ChatRowCount = ServerRowCount;
            }
            
        }

        private void SyncTimer_Tick(object sender, EventArgs e)
        {
            SyncHistory();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SyncHistory();
        }

        private void ChatSend_Click(object sender, RoutedEventArgs e)
        {
            String Message = String.Format("[{0}] {1}", Username, ChatText.Text);
            String Result = SendCommand("STORE", Message);
            ChatText.Text = "";
            if (Result == "OK")
            {
                SyncHistory();
            }
        }
    }
}
