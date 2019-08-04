using PsDownloadTools.Bean;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PsDownloadTools.Helper
{
    class Aria2Manager
    {
        public delegate void ResultDelegateBoolean(Boolean result);
        public delegate void ResultDelegateString(String str);
        public delegate void ResultDelegateStringString(String str1, String str2);
        public delegate void ResultDelegateDownloadItem(DownloadItem downloadItem);
        private event ResultDelegateBoolean InitResultHandler;
        private event ResultDelegateBoolean InitOnStartHandler;
        private event ResultDelegateStringString InitOnCompleteHandler;
        private event ResultDelegateBoolean CancelAllResultHandler;
        private event ResultDelegateDownloadItem CancelSingleResultHandler;
        private event ResultDelegateString GetDownloadingInfoResultHandler;
        private static Aria2Manager _aria2Manager;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ObservableCollection<DownloadItem> _downloadItems = new ObservableCollection<DownloadItem>();
        private ClientWebSocket _webSocket;
        private Boolean _isListening = false;
        private System.Timers.Timer _timer;

        public static Aria2Manager GetInstance()
        {
            if (_aria2Manager == null)
            {
                _aria2Manager = new Aria2Manager();
            }
            return _aria2Manager;
        }

        public async Task Init(ResultDelegateBoolean initResultHandler, ResultDelegateBoolean initOnStartHandler, ResultDelegateStringString initOnCompleteHandler)
        {
            InitResultHandler = initResultHandler;
            InitOnStartHandler = initOnStartHandler;
            InitOnCompleteHandler = initOnCompleteHandler;

            if (!IsAriaOn())
            {
                await StartServer();
            }

            Boolean connection = await ConnectServer();
            if (connection)
            {
                SocketReceive();
                await GetDownloadHistory();
            }
            else
            {
                InitResultHandler?.Invoke(false);
            }
        }

        public void ClearInitHandlers()
        {
            InitResultHandler = null;
            InitOnStartHandler = null;
            InitOnCompleteHandler = null;
        }

        public void ClearProgressHandlers()
        {
            CancelAllResultHandler = null;
            CancelSingleResultHandler = null;
            GetDownloadingInfoResultHandler = null;
        }

        private async Task StartServer()
        {
            String dir = ".\\Aria2";
            String exepath = $"{dir}\\aria2c.exe";
            String sessionpath = $"{dir}\\aria2.session";
            String args = $"--enable-rpc --rpc-secret=PSD --rpc-listen-all --rpc-allow-origin-all -d \"{SettingsHelper.DownloadPath}\" -x 5 -c -i \"{sessionpath}\" --save-session=\"{sessionpath}\" --save-session-interval=60";

            if (!Directory.Exists(".\\Aria2"))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(exepath))
            {
                using (FileStream fs = File.Open(exepath, FileMode.Create))
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PsDownloadTools.Resource.Aria2.aria2c.exe"))
                    {
                        Byte[] buffer = new Byte[1024 * 1024];
                        while (true)
                        {
                            Int32 count = stream.Read(buffer, 0, buffer.Length);
                            if (count > 0)
                            {
                                fs.Write(buffer, 0, count);
                            }
                            else
                            {
                                break;
                            }
                        }
                        stream.Dispose();
                    }
                    fs.Dispose();
                }
            }

            if (!File.Exists(sessionpath))
            {
                using (FileStream fs = File.Create(sessionpath))
                {
                    fs.Dispose();
                };
            }

            using (Process p = new Process())
            {
                p.StartInfo.FileName = exepath;
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.Dispose();
            }

            while (true)
            {
                await Task.Delay(500);
                if (IsAriaOn())
                {
                    break;
                }
            }
        }

        public async Task StopServer()
        {
            if (!IsAriaOn())
            {
                return;
            }

            await ConnectServer();
            SocketReceive();
            await DisconnectServer();

            while (true)
            {
                await Task.Delay(500);
                if (!IsAriaOn())
                {
                    break;
                }
            }
        }

        private async Task<Boolean> ConnectServer()
        {
            try
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri("ws://localhost:6800/jsonrpc"), CancellationToken.None);
                return _webSocket.State == WebSocketState.Open;
            }
            catch
            {
                return false;
            }
        }

        public async Task DisconnectServer()
        {
            try
            {
                String request = new JsonRequest("DisconnectServer", "aria2.pauseAll", String.Empty).ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("DisconnectServer", e);
            }
        }

        private async Task ShutDown()
        {
            try
            {
                String request = new JsonRequest("ShutDown", "aria2.shutdown", String.Empty).ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("ShutDown", e);
            }
        }

        private void ShutDownResult(String response)
        {
            try
            {
                if (JsonRequest.GetResult(response).result.Equals("OK"))
                {
                    _isListening = false;
                    _webSocket.Abort();
                    _webSocket.Dispose();
                }
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("ShutDownResult", e);
            }
        }

        public async Task SetDownloadPath()
        {
            try
            {
                if (_webSocket.State != WebSocketState.Open)
                {
                    return;
                }

                String request = new JsonRequest("changeGlobalOption", "aria2.changeGlobalOption", $"{{\"dir\":\"{SettingsHelper.DownloadPath}\"}}").ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("SetDownloadPath", e);
            }
        }

        private async Task GetDownloadHistory()
        {
            try
            {
                String request = new JsonRequest("GetDownloadHistory", new JsonRequest.MultiCalls[] {
                    new JsonRequest.MultiCalls("aria2.tellActive", $"[\"gid\",\"files\",\"status\",\"downloadSpeed\",\"totalLength\",\"completedLength\"]"),
                    new JsonRequest.MultiCalls("aria2.tellWaiting", $"0,{Int32.MaxValue},[\"gid\",\"files\",\"status\",\"downloadSpeed\",\"totalLength\",\"completedLength\"]")}).ToJson();

                await SocketSend(request);
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("GetDownloadHistory", e);
            }
        }

        private void GetDownloadHistoryResult(String response)
        {
            try
            {
                JsonResultMultiCalls info = JsonRequest.GetResultMultiCalls(response);

                List<JsonResultMultiCalls.ResultItemA> results = new List<JsonResultMultiCalls.ResultItemA>();
                results.AddRange(JsonRequest.GetResultMultiCallsResultItemAReform(info.result[0][0]));
                results.AddRange(JsonRequest.GetResultMultiCallsResultItemAReform(info.result[1][0]));

                results.ForEach(result =>
                {
                    DownloadItem downloadItem = new DownloadItem(result.gid);
                    downloadItem.SetDownloadInfo(result.files[0].uris[0].uri, result.files[0].path, result.status, Int64.Parse(result.totalLength), Int64.Parse(result.completedLength), Int64.Parse(result.downloadSpeed));
                    _downloadItems.Add(downloadItem);
                });

                InitResultHandler?.Invoke(true);
            }
            catch (Exception e)
            {
                InitResultHandler?.Invoke(false);
                ExceptionHelper.ShowErrorMsg("GetDownloadHistoryResult", e);
            }
        }

        public async Task AddDownload(String path)
        {
            try
            {
                String request = new JsonRequest("addUri", "aria2.addUri", $"[\"{path}\"]").ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("AddDownload", e);
            }
        }

        public async Task StartAll()
        {
            try
            {
                String request = new JsonRequest("unpauseAll", "aria2.unpauseAll", String.Empty).ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("StartAll", e);
            }
        }

        public async Task PauseAll()
        {
            try
            {
                String request = new JsonRequest("pauseAll", "aria2.pauseAll", String.Empty).ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("PauseAll", e);
            }
        }

        public async Task CancelAll(ResultDelegateBoolean cancelAllResultHandler)
        {
            try
            {
                CancelAllResultHandler = cancelAllResultHandler;

                List<JsonRequest.MultiCalls> multiCalls = new List<JsonRequest.MultiCalls>();
                foreach (DownloadItem downloadItem in _downloadItems)
                {
                    multiCalls.Add(new JsonRequest.MultiCalls("aria2.remove", $"\"{downloadItem.GetGid}\""));
                }
                String request = new JsonRequest("CancelAll", multiCalls.ToArray()).ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                CancelAllResultHandler?.Invoke(false);
                ExceptionHelper.ShowErrorMsg("CancelAll", e);
            }
        }

        private void CancelAllResult(String response)
        {
            try
            {
                CancelAllResultHandler?.Invoke(JsonRequest.GetResult(response).result.Equals("OK"));
                _downloadItems.Clear();
            }
            catch (Exception e)
            {
                CancelAllResultHandler?.Invoke(false);
                ExceptionHelper.ShowErrorMsg("CancelAllResult", e);
            }
        }

        public async Task StartSingle(String gid)
        {
            try
            {
                String request = new JsonRequest("unpause", "aria2.unpause", $"\"{gid}\"").ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("StartSingle", e);
            }
        }

        public async Task PauseSingle(String gid)
        {
            try
            {
                String request = new JsonRequest("pause", "aria2.pause", $"\"{gid}\"").ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                ExceptionHelper.ShowErrorMsg("PauseSingle", e);
            }
        }

        public async Task CancelSingle(String gid, ResultDelegateDownloadItem cancelSingleResultHandler)
        {
            try
            {
                CancelSingleResultHandler = cancelSingleResultHandler;

                String request = new JsonRequest("CancelSingle", "aria2.remove", $"\"{gid}\"").ToJson();
                await SocketSend(request);
            }
            catch (Exception e)
            {
                CancelSingleResultHandler?.Invoke(null);
                ExceptionHelper.ShowErrorMsg("CancelSingle", e);
            }
        }

        private void CancelSingleResult(String response)
        {
            try
            {
                String gid = JsonRequest.GetResult(response).result;
                DownloadItem downloadItem = _downloadItems.First(item => item.GetGid.Equals(gid));
                _downloadItems.Remove(downloadItem);
                CancelSingleResultHandler?.Invoke(downloadItem);
            }
            catch (Exception e)
            {
                CancelSingleResultHandler?.Invoke(null);
                ExceptionHelper.ShowErrorMsg("CancelSingleResult", e);
            }
        }

        public void StartRefreshing(ResultDelegateString getDownloadingInfoResultHandler)
        {
            GetDownloadingInfoResultHandler = getDownloadingInfoResultHandler;
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += async (s, e) =>
            {
                _timer?.Stop();
                await GetDownloadingInfo();
                _timer?.Start();
            };
            _timer.Enabled = true;
            _timer.Start();
        }

        public void StopRefreshing()
        {
            GetDownloadingInfoResultHandler = null;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            GC.Collect();
        }

        private async Task GetDownloadingInfo()
        {
            try
            {
                String request = new JsonRequest("GetDownloadingInfo", new JsonRequest.MultiCalls[] {
                    new JsonRequest.MultiCalls("aria2.tellActive", $"[\"gid\",\"files\",\"status\",\"downloadSpeed\",\"totalLength\",\"completedLength\"]"),
                    new JsonRequest.MultiCalls("aria2.tellWaiting", $"0,{Int32.MaxValue},[\"gid\",\"files\",\"status\",\"downloadSpeed\",\"totalLength\",\"completedLength\"]"),
                    new JsonRequest.MultiCalls("aria2.getGlobalStat", String.Empty)}).ToJson();

                await SocketSend(request);
            }
            catch (Exception e)
            {
                GetDownloadingInfoResultHandler?.Invoke(String.Empty);
                ExceptionHelper.ShowErrorMsg("GetDownloadingInfo", e);
            }
        }

        private void GetDownloadingInfoResult(String response)
        {
            try
            {
                JsonResultMultiCalls info = JsonRequest.GetResultMultiCalls(response);

                List<JsonResultMultiCalls.ResultItemA> results = new List<JsonResultMultiCalls.ResultItemA>();
                results.AddRange(JsonRequest.GetResultMultiCallsResultItemAReform(info.result[0][0]));
                results.AddRange(JsonRequest.GetResultMultiCallsResultItemAReform(info.result[1][0]));

                String globelResult = JsonRequest.GetResultMultiCallsResultItemBReform(info.result[2][0]).ToString();

                results.ForEach(result =>
                {
                    _downloadItems.First(downloadItem => downloadItem.GetGid.Equals(result.gid)).SetDownloadInfo(result.files[0].uris[0].uri, result.files[0].path, result.status, Int64.Parse(result.totalLength), Int64.Parse(result.completedLength), Int64.Parse(result.downloadSpeed));
                });

                GetDownloadingInfoResultHandler?.Invoke(globelResult);
            }
            catch (Exception e)
            {
                GetDownloadingInfoResultHandler?.Invoke(String.Empty);
                ExceptionHelper.ShowErrorMsg("GetDownloadHistoryInfo", e);
            }
        }

        private void OnDownloadStart(String response)
        {
            try
            {
                JsonResultEvent info = JsonRequest.GetResultEvent(response);
                String gid = info.@params[0].gid;
                DownloadItem downloadItem = new DownloadItem(gid);
                _downloadItems.Add(downloadItem);
                InitOnStartHandler?.Invoke(true);
            }
            catch (Exception e)
            {
                InitOnStartHandler?.Invoke(false);
                ExceptionHelper.ShowErrorMsg("OnDownloadStart", e);
            }
        }

        private void OnDownloadComplete(String response)
        {
            try
            {
                JsonResultEvent info = JsonRequest.GetResultEvent(response);
                String gid = info.@params[0].gid;
                DownloadItem downloadItem = _downloadItems.First(item => item.GetGid.Equals(gid));
                _downloadItems.Remove(downloadItem);
                InitOnCompleteHandler?.Invoke(downloadItem.GetPath, downloadItem.GetLocalPath.Replace("/", "\\"));
            }
            catch (Exception e)
            {
                InitOnCompleteHandler?.Invoke(String.Empty, String.Empty);
                ExceptionHelper.ShowErrorMsg("OnDownloadComplete", e);
            }
        }

        private async Task SocketSend(String request)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                await _webSocket.SendAsync(new ArraySegment<Byte>(Encoding.UTF8.GetBytes(request)), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception e)
            {
                throw new Exception($"Request: {request}\r\n", e);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async void SocketReceive()
        {
            String resultStr = String.Empty;
            _isListening = true;

            try
            {
                while (_isListening)
                {
                    WebSocketReceiveResult result;
                    ArraySegment<Byte> buffer = new ArraySegment<Byte>(new Byte[1024 * 10]);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        do
                        {
                            result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
                            stream.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                        while (!result.EndOfMessage);

                        stream.Seek(0, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            resultStr = reader.ReadToEnd();
                            reader.Dispose();
                        }
                        stream.Dispose();
                    }

                    if (resultStr.Contains("aria2.onDownloadStart"))
                    {
                        OnDownloadStart(resultStr);
                    }
                    else if (resultStr.Contains("aria2.onDownloadComplete"))
                    {
                        OnDownloadComplete(resultStr);
                    }
                    else if (resultStr.Contains("GetDownloadHistory"))
                    {
                        GetDownloadHistoryResult(resultStr);
                    }
                    else if (resultStr.Contains("CancelAll"))
                    {
                        CancelAllResult(resultStr);
                    }
                    else if (resultStr.Contains("CancelSingle"))
                    {
                        CancelSingleResult(resultStr);
                    }
                    else if (resultStr.Contains("GetDownloadingInfo"))
                    {
                        GetDownloadingInfoResult(resultStr);
                    }
                    else if (resultStr.Contains("DisconnectServer"))
                    {
                        await ShutDown();
                    }
                    else if (resultStr.Contains("ShutDown"))
                    {
                        ShutDownResult(resultStr);
                    }
                }
            }
            catch (Exception e)
            {
                new Exception($"Response: {resultStr}\r\n", e);
            }
        }

        public IList<DownloadItem> GetDownloadItems()
        {
            return _downloadItems;
        }

        public Boolean IsDownloading(String path)
        {
            return _downloadItems.Any(downloadItem => downloadItem.GetName.Equals(new Uri(path).Segments.Last()));
        }

        private Boolean IsAriaOn()
        {
            return Process.GetProcessesByName("aria2c").Count() > 0;
        }
    }
}
