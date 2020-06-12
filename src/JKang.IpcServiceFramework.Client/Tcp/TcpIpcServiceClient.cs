﻿using System;
using JKang.IpcServiceFramework.Services;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace JKang.IpcServiceFramework.Tcp
{
    internal class TcpIpcServiceClient<TInterface> : IpcServiceClient<TInterface>
        where TInterface : class
    {
        private readonly IPAddress _serverIp;
        private readonly Int32 _serverPort;

        public TcpIpcServiceClient(IIpcMessageSerializer serializer, IValueConverter converter, IPAddress serverIp, int serverPort)
            : base(serializer, converter)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        protected override async Task<Stream> ConnectToServerAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var client = new TcpClient();
            IAsyncResult result = client.BeginConnect(_serverIp, _serverPort, null, null);

            await Task.Run(() =>
            {
                // poll every 100ms to check cancellation request
                while (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(100), false))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        client.EndConnect(result);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            });

            cancellationToken.Register(() =>
            {
                client.Close();
            });

            return client.GetStream();
        }
    }
}
