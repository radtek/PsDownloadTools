using System;
using System.ComponentModel;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;

namespace PsDownloadTools.Bean
{
    public class DownloadItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly String _gid;
        private String _path = String.Empty;
        private String _localPath = String.Empty;
        private String _status = String.Empty;
        private Int64 _length = 0L;
        private Int64 _completedLength = 0L;
        private Int64 _downloadSpeed = 0L;

        public DownloadItem(String gid)
        {
            _gid = gid;
        }

        public String GetGid
        {
            get { return _gid; }
        }

        public void SetDownloadInfo(String path, String localPath, String status, Int64 length, Int64 completedLength, Int64 downloadSpeed)
        {
            _path = path;
            _localPath = localPath;
            _status = status;
            _length = length;
            _completedLength = completedLength;
            _downloadSpeed = downloadSpeed;

            OnPropertyChanged("GetPath");
            OnPropertyChanged("GetLocalPath");
            OnPropertyChanged("GetStatus");
            OnPropertyChanged("GetStatusColor");
            OnPropertyChanged("GetName");
            OnPropertyChanged("GetProgress");
            OnPropertyChanged("IsStop");
            OnPropertyChanged("IsComplete");
            OnPropertyChanged("SizeSpeedTime");
        }

        public String GetPath
        {
            get { return _path; }
        }

        public String GetLocalPath
        {
            get { return _localPath; }
        }

        public String GetStatus
        {
            get
            {
                String status = String.Empty;
                switch (_status)
                {
                    case "active":
                        status = Application.Current.TryFindResource("StrStatusDownloading") as String;
                        break;
                    case "waiting":
                        status = Application.Current.TryFindResource("StrStatusWaiting") as String;
                        break;
                    case "paused":
                        status = Application.Current.TryFindResource("StrStatusPaused") as String;
                        break;
                    case "complete":
                        status = Application.Current.TryFindResource("StrStatusComplete") as String;
                        break;
                    case "error":
                        status = Application.Current.TryFindResource("StrStatusError") as String;
                        break;
                }

                return status;
            }
        }

        public SolidColorBrush GetStatusColor
        {
            get
            {
                SolidColorBrush brush = Application.Current.TryFindResource("ColorTextDefault") as SolidColorBrush;
                switch (_status)
                {
                    case "active":
                        brush = Application.Current.TryFindResource("ColorGreen") as SolidColorBrush;
                        break;
                    case "waiting":
                        brush = Application.Current.TryFindResource("ColorYellow") as SolidColorBrush;
                        break;
                    case "paused":
                        brush = Application.Current.TryFindResource("ColorYellow") as SolidColorBrush;
                        break;
                    case "complete":
                        brush = Application.Current.TryFindResource("ColorTextDefault") as SolidColorBrush;
                        break;
                    case "error":
                        brush = Application.Current.TryFindResource("ColorRed") as SolidColorBrush;
                        break;
                }

                return brush;
            }
        }

        public String GetName
        {
            get { return String.IsNullOrEmpty(_path) ? String.Empty : new Uri(_path).Segments.Last(); }
        }

        public Double GetProgress
        {
            get { return _length == 0L ? 0D : (Double)_completedLength / (Double)_length * 100D; }
        }

        public Boolean IsStop
        {
            get { return !_status.Equals("active"); }
        }

        public Boolean IsComplete
        {
            get { return !_status.Equals("complete"); }
        }

        public String SizeSpeedTime
        {
            get
            {
                String sizeComplete, size, speed, time;
                if (_completedLength >= 1024D * 1024D * 1024D)
                {
                    sizeComplete = String.Format("{0:0.00} GB", _completedLength / (1024D * 1024D * 1024D));
                }
                else if (_completedLength >= 1024D * 1024D)
                {
                    sizeComplete = String.Format("{0:0.00} MB", _completedLength / (1024D * 1024D));
                }
                else if (_completedLength >= 1024D)
                {
                    sizeComplete = String.Format("{0:0.00} KB", _completedLength / 1024D);
                }
                else
                {
                    sizeComplete = String.Format("{0:0.00} Bytes", _completedLength);
                }

                if (_length >= 1024D * 1024D * 1024D)
                {
                    size = String.Format("{0:0.00} GB", _length / (1024D * 1024D * 1024D));
                }
                else if (_length >= 1024D * 1024D)
                {
                    size = String.Format("{0:0.00} MB", _length / (1024D * 1024D));
                }
                else if (_length >= 1024D)
                {
                    size = String.Format("{0:0.00} KB", _length / 1024D);
                }
                else
                {
                    size = String.Format("{0:0.00} Bytes", _length);
                }

                if (_downloadSpeed >= 1024D * 1024D * 1024D)
                {
                    speed = String.Format("{0:0.00} GB/S", _downloadSpeed / (1024D * 1024D * 1024D));
                }
                else if (_downloadSpeed >= 1024D * 1024D)
                {
                    speed = String.Format("{0:0.00} MB/S", _downloadSpeed / (1024D * 1024D));
                }
                else if (_downloadSpeed >= 1024D)
                {
                    speed = String.Format("{0:0.00} KB/S", _downloadSpeed / 1024D);
                }
                else
                {
                    speed = String.Format("{0:0.00} Bytes/S", _downloadSpeed);
                }

                Double etaTime = _downloadSpeed == 0D ? 0D : (_length - _completedLength) / _downloadSpeed;

                if (etaTime >= 60D * 60D)
                {
                    Double n = Math.Floor(etaTime / (60D * 60D));
                    time = String.Format("{0:00}:", n);
                    etaTime -= n * 60D * 60D;
                }
                else
                {
                    time = "00:";
                }
                if (etaTime >= 60D)
                {
                    Double n = Math.Floor(etaTime / (60D));
                    time += String.Format("{0:00}:", n);
                    etaTime -= n * 60D;
                }
                else
                {
                    time += "00:";
                }
                time += String.Format("{0:00}", etaTime);

                return $"{sizeComplete} / {size}    {speed}    {time}";
            }
        }

        private void OnPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class JsonResultEvent
    {
        public String jsonrpc;
        public String method;
        public Param[] @params;

        public class Param
        {
            public String gid;
        }
    }

    public class JsonResult
    {
        public String jsonrpc;
        public String id;
        public String result;
    }

    public class JsonResultMultiCalls
    {
        public String jsonrpc;
        public String id;
        public Object[][] result;

        public class ResultItemA
        {
            public String gid;
            public String totalLength;
            public String completedLength;
            public String status;
            public String downloadSpeed;
            public FileItem[] files;

            public class FileItem
            {
                public String path;
                public UriItem[] uris;

                public class UriItem
                {
                    public String uri;
                }
            }
        }

        public class ResultItemB
        {
            public String downloadSpeed;
            public String uploadSpeed;
            public String numActive;
            public String numWaiting;
            public String numStoppedTotal;

            public override String ToString()
            {
                Double speed = Double.Parse(downloadSpeed);
                string speedStr;
                if (speed >= 1024D * 1024D * 1024D)
                {
                    speedStr = String.Format("{0:0.00} GB/S", speed / (1024D * 1024D * 1024D));
                }
                else if (speed >= 1024D * 1024D)
                {
                    speedStr = String.Format("{0:0.00} MB/S", speed / (1024D * 1024D));
                }
                else if (speed >= 1024D)
                {
                    speedStr = String.Format("{0:0.00} KB/S", speed / 1024D);
                }
                else
                {
                    speedStr = String.Format("{0:0.00} Bytes/S", speed);
                }
                return $"{numActive} / {Int32.Parse(numActive) + Int32.Parse(numWaiting)}\r\n{speedStr}";
            }
        }
    }

    public class JsonRequest
    {
        private readonly String _id;
        private readonly String _method;
        private readonly String _paramsList;

        public JsonRequest(String id, String method, String paramsList)
        {
            _id = id;
            _method = method;
            _paramsList = paramsList;
        }

        public JsonRequest(String id, MultiCalls[] methods)
        {
            _id = id;
            _method = "system.multicall";
            _paramsList = String.Join(",", methods.Select(multiCall => multiCall.ToString()));
        }

        public String ToJson()
        {
            if (_method.Equals("system.multicall"))
            {
                return $"{{\"id\":\"{_id}\",\"jsonrpc\":\"2.0\",\"method\":\"{_method}\",\"params\":[[{_paramsList}]]}}";
            }
            else
            {
                return $"{{\"id\":\"{_id}\",\"jsonrpc\":\"2.0\",\"method\":\"{_method}\",\"params\":[\"token:PSD\"{(String.IsNullOrEmpty(_paramsList) ? "" : ",")}{_paramsList}]}}";
            }
        }

        public static JsonResultEvent GetResultEvent(String response)
        {
            return new JavaScriptSerializer().Deserialize<JsonResultEvent>(response) as JsonResultEvent;
        }

        public static JsonResult GetResult(String response)
        {
            return new JavaScriptSerializer().Deserialize<JsonResult>(response) as JsonResult;
        }

        public static JsonResultMultiCalls GetResultMultiCalls(String response)
        {
            return new JavaScriptSerializer().Deserialize<JsonResultMultiCalls>(response) as JsonResultMultiCalls;
        }

        public static JsonResultMultiCalls.ResultItemA[] GetResultMultiCallsResultItemAReform(Object input)
        {
            String json = new JavaScriptSerializer().Serialize(input);
            return new JavaScriptSerializer().Deserialize<JsonResultMultiCalls.ResultItemA[]>(json) as JsonResultMultiCalls.ResultItemA[];
        }

        public static JsonResultMultiCalls.ResultItemB GetResultMultiCallsResultItemBReform(Object input)
        {
            String json = new JavaScriptSerializer().Serialize(input);
            return new JavaScriptSerializer().Deserialize<JsonResultMultiCalls.ResultItemB>(json) as JsonResultMultiCalls.ResultItemB;
        }

        public class MultiCalls
        {
            private readonly String _method;
            private readonly String _paramsList;

            public MultiCalls(String method, String paramsList)
            {
                _method = method;
                _paramsList = paramsList;
            }

            public override String ToString()
            {
                String str = $"{{\"methodName\":\"{_method}\",\"params\":[\"token:PSD\"";
                if (!String.IsNullOrEmpty(_paramsList))
                {
                    str += $",{_paramsList}";
                }
                str += "]}";
                return str;
            }
        }
    }
}
