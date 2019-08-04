using PsDownloadTools.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace PsDownloadTools.Model
{
    public delegate void UpdateRequestDelegate(String psnPath, String localPath, Boolean isDownloading);

    class HttpRequest : IDisposable
    {
        public delegate void DestroyDelegate(HttpRequest request);
        private readonly UpdateRequestDelegate _updateRequest;
        private readonly DestroyDelegate _destroyer;
        private readonly Byte[] _clientBuffer = new Byte[4096];
        private readonly Byte[] _serverBuffer = new Byte[1024];
        private readonly Dictionary<String, String> _headerFields = new Dictionary<String, String>();
        private Socket _clientSocket;
        private Socket _serverSocket;
        private FileStream _fileStream;
        private String _requestQuery;
        private String _requestMethod;
        private String _requestUrl;
        private String _requestHttpVersion;
        private String _requestedPath;
        private String _requestPostBody;

        public HttpRequest(Socket clientSocket, DestroyDelegate destroyer, UpdateRequestDelegate updateRequest)
        {
            _clientSocket = clientSocket;
            _destroyer = destroyer;
            _updateRequest = updateRequest;
        }

        public void StartHandshake()
        {
            try
            {
                if (_clientSocket != null)
                {
                    _clientSocket.BeginReceive(_clientBuffer, 0, _clientBuffer.Length, SocketFlags.None, OnReceiveQuery, _clientSocket);
                }
            }
            catch
            {
                Dispose();
            }
        }

        private void OnReceiveQuery(IAsyncResult ar)
        {
            Int32 count = -1;
            try
            {
                if (_clientSocket != null)
                {
                    count = _clientSocket.EndReceive(ar);
                }
            }
            catch
            {
                count = -1;
            }

            if (count <= 0)
            {
                Dispose();
                return;
            }

            _requestQuery += Encoding.ASCII.GetString(_clientBuffer, 0, count);
            if (IsQueryValid(_requestQuery))
            {
                HandleQuery(_requestQuery);
            }
            else
            {
                try
                {
                    if (_clientSocket != null)
                    {
                        _clientSocket.BeginReceive(_clientBuffer, 0, _clientBuffer.Length, SocketFlags.None, OnReceiveQuery, _clientSocket);
                    }
                }
                catch
                {
                    Dispose();
                }
            }
        }

        private Boolean IsQueryValid(String query)
        {
            if (String.IsNullOrEmpty(query))
            {
                return false;
            }

            _headerFields.Clear();
            String[] array = query.Replace("\r\n", "\n").Split('\n');
            if (array.Length > 0)
            {
                String[] parts = array[0].Trim().Split(' ');
                if (parts.Length == 3)
                {
                    _requestMethod = parts[0];
                    _requestUrl = parts[1];
                    _requestHttpVersion = parts[2];
                }
                else
                {
                    return false;
                }

                if (_requestUrl.ToLower().StartsWith("http"))
                {
                    _requestedPath = new Uri(_requestUrl).AbsolutePath;
                }
                else
                {
                    _requestedPath = _requestUrl;
                }
            }
            else
            {
                return false;
            }

            for (Int32 i = 1; i < array.Length; i++)
            {
                Int32 position = array[i].IndexOf(":");
                if (position > 0)
                {
                    String header = array[i].Substring(0, position).Trim();
                    String value = array[i].Substring(position + 1).Trim();
                    if (!_headerFields.ContainsKey(header))
                    {
                        _headerFields.Add(header, value);
                    }
                }
            }

            if (_requestMethod.ToUpper().Equals("POST"))
            {
                return !_headerFields.ContainsKey("Content-Length");
            }
            return true;
        }

        private void HandleQuery(String query)
        {
            if (_headerFields.Count == 0 || !_headerFields.ContainsKey("Host"))
            {
                SendBadRequest();
                return;
            }

            String host;
            Int32 port;
            if (_requestMethod.ToUpper().Equals("CONNECT"))
            {
                String[] parts = _requestedPath.Split(new Char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    host = parts[0];
                    port = Int32.Parse(parts[1]);
                }
                else if (parts.Length == 1)
                {
                    host = parts[0];
                    port = 443;
                }
                else
                {
                    host = _requestedPath;
                    port = 80;
                }
            }
            else
            {
                String[] parts = _headerFields["Host"].Split(new Char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    host = parts[0];
                    port = Int32.Parse(parts[1]);
                }
                else
                {
                    host = parts[0];
                    port = 80;
                }

                if (_requestMethod.ToUpper().Equals("POST"))
                {
                    Int32 position = query.IndexOf("\r\n\r\n");
                    _requestPostBody = query.Substring(position + 4);
                }
            }

            String psnPath = _requestUrl;
            String localPath = String.Empty;
            Boolean isDownloading = false;
            Boolean isPackageFile = NetworkHelper.IsMatchExt(psnPath);
            if (isPackageFile)
            {
                localPath = HistoryReocrds.GetLocalPath(psnPath);
                isDownloading = Aria2Manager.GetInstance().IsDownloading(psnPath);
            }

            if (!_requestMethod.ToUpper().Equals("CONNECT") && localPath != String.Empty)
            {
                SendLocalFile(localPath, _headerFields.ContainsKey("Range") ? _headerFields["Range"] : null, _headerFields.ContainsKey("Proxy-Connection") ? _headerFields["Proxy-Connection"] : null);
                isDownloading = true;
                _updateRequest(psnPath, localPath, isDownloading);
            }
            else
            {
                try
                {
                    _serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    if (_headerFields.ContainsKey("Proxy-Connection") && _headerFields["Proxy-Connection"].ToLower().Equals("keep-alive"))
                    {
                        _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                    }
                    _serverSocket.BeginConnect(host, port, OnConnected, _serverSocket);

                    if (isPackageFile)
                    {
                        _updateRequest(psnPath, localPath, isDownloading);
                    }
                }
                catch
                {
                    SendBadRequest();
                }
            }
        }

        private void SendBadRequest()
        {
            try
            {
                if (_clientSocket != null)
                {
                    String requestStr = "HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Type: text/html\r\n\r\nBad Request";
                    _clientSocket.BeginSend(Encoding.ASCII.GetBytes(requestStr), 0, requestStr.Length, SocketFlags.None, OnErrorSent, _clientSocket);
                }
            }
            catch
            {
                Dispose();
            }
        }

        private void OnErrorSent(IAsyncResult ar)
        {
            try
            {
                if (_clientSocket != null)
                {
                    _clientSocket.EndSend(ar);
                }
            }
            catch
            {
                Dispose();
            }
        }

        private void SendLocalFile(String localPath, String requestRange, String connection)
        {
            _fileStream = File.OpenRead(localPath);

            String codeStatus = "200 OK";
            Int64 rangeStart = 0L;
            Int64 rangeEnd = _fileStream.Length - 1;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("HTTP/1.1 {0}");
            sb.AppendLine("Server: Apache");
            sb.AppendLine("Accept-Ranges: bytes");
            sb.AppendLine("Cache-Control: max-age=3600");
            sb.AppendLine("Content-Type: application/octet-stream");
            sb.AppendLine("Date: {1}");
            sb.AppendLine("Last-Modified: {2}");
            sb.AppendLine("Content-Length: {3}");
            if (!String.IsNullOrEmpty(requestRange))
            {
                codeStatus = "206 Partial Content";
                String rangesStr = requestRange.Split('=')[1].Trim();
                List<String> ranges = rangesStr.Split('-').Select(range => range.Trim()).ToList();
                if (!String.IsNullOrEmpty(ranges[0]))
                {
                    rangeStart = Int64.Parse(ranges[0]);
                }
                if (!String.IsNullOrEmpty(ranges[1]))
                {
                    rangeEnd = Int64.Parse(ranges[1]);
                }
                else
                {
                    rangesStr += rangeEnd;
                }
                rangesStr += $"/{_fileStream.Length}";
                sb.AppendLine($"Content-Range: bytes {rangesStr}");
            }
            if (String.IsNullOrEmpty(connection))
            {
                connection = "close";
            }
            sb.AppendLine($"Connection: {connection}");
            sb.AppendLine();
            String response = String.Format(sb.ToString(), codeStatus, DateTime.Now.ToUniversalTime().ToString("r"), File.GetLastWriteTime(localPath).ToUniversalTime().ToString("r"), rangeEnd + 1 - rangeStart);
            _fileStream.Seek(rangeStart, SeekOrigin.Begin);

            try
            {
                _clientSocket.BeginSend(Encoding.ASCII.GetBytes(response), 0, response.Length, SocketFlags.None, OnLocalFileSent, _clientSocket);
            }
            catch
            {
                _fileStream.Close();
                Dispose();
            }
        }

        private void OnLocalFileSent(IAsyncResult ar)
        {
            try
            {
                if (_fileStream.Position < _fileStream.Length)
                {
                    Byte[] buffer = new Byte[4096];
                    _fileStream.Read(buffer, 0, buffer.Length);
                    _clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnLocalFileSent, _clientSocket);
                }
                else
                {
                    _clientSocket.EndSend(ar);
                    _fileStream.Close();
                }
            }
            catch
            {
                _fileStream.Close();
                Dispose();
            }
        }

        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                if (_serverSocket != null)
                {
                    _serverSocket.EndConnect(ar);
                    if (_requestMethod.ToUpper().Equals("CONNECT"))
                    {
                        if (_clientSocket != null)
                        {
                            String response = _requestHttpVersion + " 200 Connection established\r\n\r\n";
                            _clientSocket.BeginSend(Encoding.ASCII.GetBytes(response), 0, response.Length, SocketFlags.None, OnConnectSucceed, _clientSocket);
                        }
                    }
                    else
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"{_requestMethod} {_requestedPath} {_requestHttpVersion}");
                        foreach (KeyValuePair<String, String> header in _headerFields)
                        {
                            sb.AppendLine($"{header.Key.Replace("Proxy-", "")}: {header.Value}");
                        }
                        sb.AppendLine();
                        if (_requestPostBody != null)
                        {
                            sb.AppendLine(_requestPostBody);
                        }
                        String response = sb.ToString();
                        _serverSocket.BeginSend(Encoding.ASCII.GetBytes(response), 0, response.Length, SocketFlags.None, OnQuerySent, _serverSocket);
                    }
                }
            }
            catch
            {
                Dispose();
            }
        }

        private void OnConnectSucceed(IAsyncResult ar)
        {
            try
            {
                if (_clientSocket != null && _clientSocket.EndSend(ar) == -1)
                {
                    Dispose();
                }
                else
                {
                    StartRelay();
                }
            }
            catch
            {
                Dispose();
            }
        }

        public void StartRelay()
        {
            try
            {
                if (_clientSocket != null)
                {
                    _clientSocket.BeginReceive(_clientBuffer, 0, _clientBuffer.Length, SocketFlags.None, OnClientReceive, _clientSocket);
                    _serverSocket.BeginReceive(_serverBuffer, 0, _serverBuffer.Length, SocketFlags.None, OnServerReceive, _serverSocket);
                }
            }
            catch
            {
                Dispose();
            }
        }

        public void OnClientReceive(IAsyncResult ar)
        {
            try
            {
                if (_clientSocket != null)
                {
                    Int32 count = _clientSocket.EndReceive(ar);
                    if (count > 0 && _serverSocket != null)
                    {
                        _serverSocket.BeginSend(_clientBuffer, 0, count, SocketFlags.None, OnServerSent, _serverSocket);
                    }
                }
            }
            catch
            {
                Dispose();
            }
        }

        public void OnServerReceive(IAsyncResult ar)
        {
            try
            {
                if (_serverSocket != null)
                {
                    Int32 count = _serverSocket.EndReceive(ar);
                    if (count > 0 && _clientSocket != null)
                    {
                        _clientSocket.BeginSend(_serverBuffer, 0, count, SocketFlags.None, OnClientSent, _clientSocket);
                    }
                }
            }
            catch
            {
                Dispose();
            }
        }

        public void OnClientSent(IAsyncResult ar)
        {
            try
            {
                if (_clientSocket != null && _clientSocket.EndSend(ar) > 0)
                {
                    _serverSocket.BeginReceive(_serverBuffer, 0, _serverBuffer.Length, SocketFlags.None, OnServerReceive, _serverSocket);
                }
            }
            catch
            {
                Dispose();
            }
        }

        public void OnServerSent(IAsyncResult ar)
        {
            try
            {
                if (_serverSocket.EndSend(ar) > 0 && _clientSocket != null)
                {
                    _clientSocket.BeginReceive(_clientBuffer, 0, _clientBuffer.Length, SocketFlags.None, OnClientReceive, _clientSocket);
                }
            }
            catch
            {
                Dispose();
            }
        }

        private void OnQuerySent(IAsyncResult ar)
        {
            try
            {
                if (_serverSocket != null && _serverSocket.EndSend(ar) == -1)
                {
                    Dispose();
                }
                else
                {
                    StartRelay();
                }
            }
            catch
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            try
            {
                if (_clientSocket != null)
                {
                    _clientSocket.Shutdown(SocketShutdown.Both);
                }
            }
            catch
            {
            }
            try
            {
                if (_serverSocket != null)
                {
                    _serverSocket.Shutdown(SocketShutdown.Both);
                }
            }
            catch
            {
            }

            if (_clientSocket != null)
            {
                _clientSocket.Close();
            }
            if (_serverSocket != null)
            {
                _serverSocket.Close();
            }

            _clientSocket = null;
            _serverSocket = null;
            _destroyer(this);
        }
    }
}
