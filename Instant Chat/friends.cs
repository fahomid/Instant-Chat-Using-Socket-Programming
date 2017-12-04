using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant_Chat
{
    class friends : INotifyPropertyChanged
    {
        private string _username;
        private string _status;
        private string _full_name;
        private string _profile_image;
        private string _last_seen;
        private string _friend_id;
        public string id
        {
            get
            {
                return _friend_id;
            }
            set
            {
                _friend_id = value;
                RaisePropertyChanged("friend_id");
            }
        }
        public string username
        {
            get { return _username; }
            set
            {
                _username = value;
                RaisePropertyChanged("username");
            }
        }
        public string status
        {
            get { return _status; }
            set
            {
                _status = value;
                RaisePropertyChanged("status");
            }
        }

        public string full_name
        {
            get { return _full_name; }
            set
            {
                _full_name = value;
                RaisePropertyChanged("full_name");
            }
        }

        public string profile_image
        {
            get { return _profile_image; }
            set
            {
                _profile_image = value;
                RaisePropertyChanged("profile_image");
            }
        }

        public string last_seen
        {
            get { return _last_seen; }
            set
            {
                _last_seen = value;
                RaisePropertyChanged("last_seen");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
