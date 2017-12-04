using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Instant_Chat
{
    /// <summary>
    /// Interaction logic for DashBoard.xaml
    /// </summary>
    public partial class DashBoard : Window
    {
        closeSocketDelegate closeSocket;
        public static showLoginForm showLogin;
        public static sendRequest sendData;
        private static System.Timers.Timer getUpdate;
        public static string id;
        List<friends> deserializedFriends;
        //List<Messages> deserializedMessages;
        List<Messages> deserializedMessages;
        bool loadMessages = false;
        public DashBoard(closeSocketDelegate d, showLoginForm x, sendRequest y, string idpassed)
        {
            InitializeComponent();
            deserializedMessages = new List<Messages>();
            message_container.ItemsSource = deserializedMessages;
            closeSocket = d;
            showLogin = x;
            //List <friends> items = new List<friends>();
            //friends a = new friends() { friendImage = "http://localhost/images/cs_fest_2016.png", friendUsername = "Fahomid Hassan", friendStatus = "/Icons/offline.png" };
            //friends b = new friends() { friendImage = "/Icons/close.png", friendUsername = "Toto Mia", friendStatus = "/Icons/offline.png" };
            //friends c = new friends() { friendImage = "/Icons/close.png", friendUsername = "Mr Toto", friendStatus = "/Icons/offline.png" };
            //items.Add(a);
            //items.Add(b);
            //listView.ItemsSource = items;
            //items.Add(c);
            id = idpassed;
            sendData = y;
            getFriends();
            Thread friendDataUpdate = new Thread(SetTimer);
            friendDataUpdate.IsBackground = true;
            friendDataUpdate.Start();
        }

        public static void getFriends()
        {
            dynamic aObj = new JObject();
            aObj.request_type = "friendList";
            aObj.id = id;
            string json = JsonConvert.SerializeObject(aObj);
            sendData(json);
        }

        public static void getMessages(string from, string to,int offset, int limit)
        {
            dynamic aObj = new JObject();
            aObj.request_type = "get_messages";
            aObj.message_from = from;
            aObj.message_to = to;
            aObj.offset = offset;
            aObj.limit = limit;
            string json = JsonConvert.SerializeObject(aObj);
            sendData(json);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closeSocket();
            Application.Current.Shutdown();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            closeSocket();
            Application.Current.Shutdown();
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            closeSocket();
            this.Hide();
            showLogin();
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = listView.SelectedItem as friends;
            if (item != null)
            {
                rightMainContent.Visibility = Visibility.Visible;
                rightAlertContent.Visibility = Visibility.Hidden;
                //DateTime dt = Convert.ToDateTime(item.last_seen);
                //last_seen.Content = dt.ToString();
                //friend_status.Text = item.status;
                //name.Content = item.full_name;
                //BitmapImage empImage = new BitmapImage(new Uri(item.profile_image));
                //profile_image_box.ImageSource = empImage;
                //if (item.status == "1")
                //{
                //    last_seen_container.Visibility = Visibility.Collapsed;
                //    now_online.Visibility = Visibility.Visible;
                //}
                //else
                //{
                //    last_seen_container.Visibility = Visibility.Visible;
                //    now_online.Visibility = Visibility.Collapsed;
                //}
                deserializedMessages.Clear();
                loadMessages = false;
                getMessages(id, item.id, 0, 10);
            }
        }

        public void setProfileImageNId(string l, string ll)
        {
            l = l.Trim('\'');
            my_image.Source = new BitmapImage(new Uri(l, UriKind.RelativeOrAbsolute));
            my_id.Text = ll;
        }

        public void buildFrindList(string data)
        {
            if(deserializedFriends == null)
            {
                deserializedFriends = JsonConvert.DeserializeObject<List<friends>>(data);
                listView.ItemsSource = deserializedFriends;
            }
            else
            {
                List<friends> tempList = JsonConvert.DeserializeObject<List<friends>>(data);

                for(int i = 0; i < deserializedFriends.Count && i < tempList.Count; i++)
                {
                    for(int j = 0; j <= i; j++)
                    {
                        if (deserializedFriends[i].id == tempList[j].id)
                        {
                            deserializedFriends[i].username = tempList[j].username;
                            deserializedFriends[i].status = tempList[j].status;
                            deserializedFriends[i].profile_image = tempList[j].profile_image;
                            deserializedFriends[i].last_seen = tempList[j].last_seen;
                        }
                    }
                }
                listView.Items.Refresh();

                //for (int i = 0; i < deserializedFriends.Count && i < tempList.Count; i++)
                //{

                //}
            }
        }

        public void buildMessages(string data)
        {
            //deserializedMessages = JsonConvert.DeserializeObject<List<Messages>>(data);
            //message_container.ItemsSource = deserializedMessages;
            //message_container.Items.Refresh();
            //if(message_container.Items.Count > 0)
            //{
            //    message_container.ScrollIntoView(message_container.Items[message_container.Items.Count - 1]);
            //}
            List<Messages> newMessages = JsonConvert.DeserializeObject<List<Messages>>(data);
            //deserializedMessages.AddRange(newMessages);
            if (message_container.Items.Count > 0)
            {
                var i = message_container.Items.GetItemAt(0);
                deserializedMessages.InsertRange(0, newMessages);
                message_container.Items.Refresh();
                message_container.ScrollIntoView(message_container.Items[newMessages.Count]);
                loadMessages = true;
            }
            else
            {
                deserializedMessages.AddRange(newMessages);
                message_container.Items.Refresh();
                if (message_container.Items.Count > 0)
                {
                    message_container.ScrollIntoView(message_container.Items[message_container.Items.Count - 1]);
                }
                loadMessages = true;
            }
        }

        public void addNewMessage(string data)
        {
            //Messages obj = new Messages();
            //dynamic jObj = JsonConvert.DeserializeObject<dynamic>(data);
            ////obj.message = jObj.message;
            ////obj.message_to = jObj.message_to;
            ////obj.message_from = jObj.message_from;
            ////obj.time = jObj.time;
            //deserializedMessages.Add(jObj);
            //message_container.Items.Refresh();
            try
            {
                List<Messages> newMessages = JsonConvert.DeserializeObject<List<Messages>>(data);
                if(newMessages.Count > 0)
                {
                    if(friend_id.Text.Equals(newMessages[0].message_from) || friend_id.Text.Equals(newMessages[0].message_to))
                    {
                        deserializedMessages.AddRange(newMessages);
                        message_container.Items.Refresh();
                        if (message_container.Items.Count > 0)
                        {
                            message_container.ScrollIntoView(message_container.Items[message_container.Items.Count - 1]);
                        }
                    }
                }
            }
            catch(Exception rrr)
            {
                MessageBox.Show(rrr.Message);
            }
        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            getUpdate = new System.Timers.Timer(3000);
            // Hook up the Elapsed event for the timer. 
            getUpdate.Elapsed += getUpdateDetails;
            getUpdate.AutoReset = true;
            getUpdate.Enabled = true;
        }
        private static void getUpdateDetails(Object source, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                getFriends();
            }));
        }

        private void SendMessageBtnClick(object sender, RoutedEventArgs e)
        {
            if(!message_box.Text.Equals(""))
            {
                dynamic aObj = new JObject();
                aObj.request_type = "send_message";
                aObj.message_from = my_id.Text;
                aObj.message_to = friend_id.Text;
                aObj.message = message_box.Text;
                string json = JsonConvert.SerializeObject(aObj);
                sendData(json);
                message_box.Clear();
            }
        }

        private void messageScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //MessageBox.Show("Scrolled!");
            ScrollViewer s = sender as ScrollViewer;
            message_container.Items.Refresh();
            if (s.VerticalOffset == 0 && loadMessages && message_container.Items.Count > 0)
            {
                var item = listView.SelectedItem as friends;
                if (item != null)
                {
                    loadMessages = false;
                    getMessages(id, item.id, message_container.Items.Count, 10);
                }
            }
        }
    }
}
