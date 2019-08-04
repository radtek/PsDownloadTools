using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace PsDownloadTools.Bean
{
    class ObjectRequest : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private String _psn;
        private String _local;
        private Boolean _isDownloading = false;
        private readonly DateTime _dateTime;

        public ObjectRequest(String psn, String local, Boolean isDownloading, DateTime dateTime)
        {
            _psn = psn;
            _local = local;
            _isDownloading = isDownloading;
            _dateTime = dateTime;
        }

        public String PsnPath
        {
            get { return _psn; }
        }

        public String LocalPath
        {
            get { return _local; }
            set
            {
                _local = value;
                OnPropertyChanged("LocalPath");
                OnPropertyChanged("LocalName");
                OnPropertyChanged("NameColor");
            }
        }

        public Boolean IsDownloading
        {
            get { return !_isDownloading; }
            set
            {
                _isDownloading = value;
                OnPropertyChanged("IsDownloading");
            }
        }

        public String DateTime
        {
            get { return _dateTime.ToString("yyyy-MM-dd HH:mm:ss"); }
        }

        public String PsnName
        {
            get { return new Uri(_psn).Segments.Last(); }
        }

        public String LocalName
        {
            get { return Path.GetFileName(_local); }
        }

        public Boolean HasLocalPath
        {
            get { return !String.IsNullOrEmpty(_local); }
        }

        public void OnPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}