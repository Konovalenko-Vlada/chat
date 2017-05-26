using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Net;
using System.Net.Sockets;

namespace ChatClient
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }


        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if(Properties.Settings.Default.StoredIP != "")
                ConnectIP.Text = Properties.Settings.Default.StoredIP;
            else
                Properties.Settings.Default.StoredIP = ConnectIP.Text;

            if (Properties.Settings.Default.StoredName != "")
                ConnectName.Text = Properties.Settings.Default.StoredName;
            else
                Properties.Settings.Default.StoredName = ConnectName.Text;

            Properties.Settings.Default.Save();

            if (ConnectIP.Text == "" | ConnectName.Text == "")
            {
                MessageBox.Show(
                    "Fill all fields!", "Chat",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            IPEndPoint Host = new IPEndPoint(
                IPAddress.Parse(ConnectIP.Text),
                Convert.ToInt32(ConnectPort.Text));

            this.Hide();
            ChatWindow Win = new ChatWindow(ConnectName.Text, Host);
            Win.Show();
        }
    }
}
