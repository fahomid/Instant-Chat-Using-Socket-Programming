using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Security;
using System.Diagnostics;

namespace Instant_Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public delegate void getResponse(string response);
    public delegate void sendRequest(string data);
    public delegate void closeSocketDelegate();
    public delegate void showLoginForm();
    public delegate void socketDisconnected();
    public partial class MainWindow : Window
    {
        static getResponse receiveData;
        static sendRequest sendReq;
        static SocketHandler socket;
        static closeSocketDelegate closeSocketDel;
        static showLoginForm showLogin;
        static socketDisconnected socketStatus;
        DashBoard user;
        public MainWindow()
        {
            InitializeComponent();
            socket = new SocketHandler();
            receiveData = new getResponse(dataReceived);
            sendReq = new sendRequest(dataSend);
            showLogin = new showLoginForm(showLoginFormWindow);
            socketStatus = new socketDisconnected(socketClosed);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            string password = passwordBox.Password;
            this.error_box.Visibility = Visibility.Collapsed;
            if (this.username.Text.Equals(""))
            {
                this.error_box.Text = "You must enter your username!";
                this.error_box.Visibility = Visibility.Visible;
            }
            else if(password.ToString().Equals(""))
            {
                this.error_box.Text = "You must enter your password!";
                this.error_box.Visibility = Visibility.Visible;
            }
            else
            {
                //MessageBox.Show(password.ToString());
                //creating user object to pass it to server
                dynamic aObj = new JObject();
                aObj.username = this.username.Text;
                aObj.request_type = "auth";
                aObj.password = socket.sha256_hash(password.ToString());
                string json = JsonConvert.SerializeObject(aObj);

                //running socket connection in new thread
                Thread thread = new Thread(() => socket.initSocket("128.199.47.47", 1000, json, receiveData, socketStatus));
                thread.IsBackground = true;
                thread.Start();

                //socket.initSocket("127.0.0.1", 1000, json, receiveData, socketStatus)
            }
        }

        //this method will be called everytime new data available from chat server
        public void dataReceived(string data)
        {
            //MessageBox.Show(data);
            //Debug.WriteLine(data);
            Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    dynamic obj = JsonConvert.DeserializeObject<dynamic>(data);
                    string response_type = obj.response_type;
                    switch (response_type)
                    {
                        case "auth":
                            if (obj.response.ToString().Equals("error"))
                            {
                                this.error_box.Text = obj.message.ToString();
                                this.error_box.Visibility = Visibility.Visible;
                                socket.closeSocket();
                            }
                            else if (obj.response.ToString().Equals("success"))
                            {
                                //MessageBox.Show(obj.profile.ToString());

                                closeSocketDel = new closeSocketDelegate(socket.closeSocket);
                                user = new DashBoard(closeSocketDel, showLogin, sendReq, obj.id.ToString());
                                user.Title = this.username.Text + " - Dashboard";
                                user.setProfileImageNId(obj.profile.ToString(), obj.id.ToString());
                                this.Hide();
                                user.Show();
                            }
                            else
                            {
                                this.error_box.Text = obj.message.ToString();
                                this.error_box.Visibility = Visibility.Visible;
                                socket.closeSocket();
                            }
                            break;

                        case "friendList":
                            if (obj.response.ToString().Equals("error"))
                            {
                                MessageBox.Show(obj.message);
                            }
                            else if (obj.response.ToString().Equals("success"))
                            {
                                //friends[] friendsObj = JsonConvert.DeserializeObject<friends>(obj.friends.ToString());
                                user.buildFrindList(obj.friends.ToString());
                            }
                            break;

                        case "get_messages":
                            if (obj.response.ToString().Equals("error"))
                            {
                                MessageBox.Show(obj.message);
                            }
                            else if (obj.response.ToString().Equals("success"))
                            {
                                //user.buildFrindList(obj.friends.ToString());
                                user.buildMessages(obj.messages.ToString());
                            }
                            break;

                        case "message":
                            if (obj.response.ToString().Equals("error"))
                            {
                                MessageBox.Show(obj.messages);
                            }
                            else if (obj.response.ToString().Equals("success"))
                            {
                                //user.buildFrindList(obj.friends.ToString());
                                user.addNewMessage(obj.messages.ToString());
                                //MessageBox.Show(obj.ToString());

                                //MessageBox.Show("Successful!");
                            }
                            break;
                    }


                    //MessageBox.Show(response);
                }
                catch (Exception e)
                {
                    //MessageBox.Show(data);
                    MessageBox.Show(e.Message);
                }
            }));
        }

        public void dataSend(string data)
        {
            socket.sendData(data);
        }

        public void showLoginFormWindow()
        {
            this.Show();
        }

        public void socketClosed()
        {
            if(user != null && user.IsVisible)
            {
                MessageBox.Show("Connection closed unexpectedly by server!");
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    user.Hide();
                    this.Show();
                }));
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
