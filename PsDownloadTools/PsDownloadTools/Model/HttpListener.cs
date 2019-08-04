using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace PsDownloadTools.Model
{
    class HttpListener : IDisposable
    {
        private readonly UpdateRequestDelegate _updateRequest;
        private readonly List<HttpRequest> _requestList = new List<HttpRequest>();
        private Boolean _isDisposed = false;
        private Socket _listenSocket;

        public HttpListener(UpdateRequestDelegate updateRequest)
        {
            _updateRequest = updateRequest;
        }

        public void Start(IPAddress ip, Int32 port)
        {
            try
            {
                _listenSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _listenSocket.Bind(new IPEndPoint(ip, port));
                _listenSocket.Listen(50);
                _listenSocket.BeginAccept(OnReceive, _listenSocket);
            }
            catch
            {
                _listenSocket = null;
                throw new SocketException();
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                Socket socket = _listenSocket.EndAccept(ar);
                if (socket != null)
                {
                    HttpRequest httpRequest = new HttpRequest(socket, OnRemoveRequest, _updateRequest);
                    AddRequest(httpRequest);
                    httpRequest.StartHandshake();
                }
            }
            catch
            {
            }

            try
            {
                _listenSocket.BeginAccept(OnReceive, _listenSocket);
            }
            catch
            {
                Dispose();
            }
        }

        private void OnRemoveRequest(HttpRequest request)
        {
            if (request != null && _requestList.Contains(request))
            {
                _requestList.Remove(request);
            }
        }

        private void AddRequest(HttpRequest request)
        {
            if (!_requestList.Contains(request))
            {
                _requestList.Add(request);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                for (Int32 i = _requestList.Count - 1; i > 0; i--)
                {
                    _requestList[i].Dispose();
                }

                try
                {
                    _listenSocket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
                finally
                {
                    if (_listenSocket != null)
                    {
                        _listenSocket.Close();
                    }
                    _isDisposed = true;
                }
            }
        }
    }
}
