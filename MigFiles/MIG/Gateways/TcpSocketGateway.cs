/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MIG.Utility;

namespace MIG.Gateways
{
    class TcpSocketGatewayConfiguration
    {
        public int Port = 0;
    }

    class TcpSocketGatewayRequest
    {
        public int ClientId;
        public byte[] Request;

        public TcpSocketGatewayRequest(int clientid, byte[] request)
        {
            this.ClientId = clientid;
            this.Request = request;
        }
    }

    class TcpSocketGateway : MIGGateway
    {
        public event Action<object> ProcessRequest;

        private TcpServerChannel server;
        private int servicePort = 4502;

        public TcpSocketGateway()
        {

        }


        public void Start()
        {
            server = NetworkConnectivity.CreateTcpServerChannel("server");
            server.ChannelClientConnected += server_ChannelClientConnected;
            server.ChannelClientDisconnected += server_ChannelClientDisconnected;
            //_server.ChannelConnected += 
            //_server.ChannelDisconnected += 
            server.DataReceived += server_DataReceived;
            //_server.DataSent += 
            //_server.ExceptionOccurred += 
            server.ExceptionOccurred += server_ExceptionOccurred;
            server.Connect(servicePort);
        }

        public void Stop()
        {
            server.ChannelClientConnected -= server_ChannelClientConnected;
            server.ChannelClientDisconnected -= server_ChannelClientDisconnected;
            //_server.ChannelConnected -= 
            //_server.ChannelDisconnected -= 
            server.DataReceived -= server_DataReceived;
            //_server.DataSent -= 
            //_server.ExceptionOccurred -= 
            server.ExceptionOccurred -= server_ExceptionOccurred;
            server.Disconnect();
        }

        public void Configure(object gwConfiguration)
        {
            var config = (TcpSocketGatewayConfiguration)gwConfiguration;
            servicePort = config.Port;
        }


        private void server_ChannelClientConnected(object sender, ServerConnectionEventArgs args)
        {
            server.Receive(256, args.ClientId);
        }

        private void server_DataReceived(object sender, ServerDataEventArgs args)
        {

            if (ProcessRequest != null)
            {
                ProcessRequest(new TcpSocketGatewayRequest(args.ClientId, args.Data)); // '\0's ending byte array
            }

            server.Receive(256, (int)args.ClientId);

        }

        private void server_ChannelClientDisconnected(object sender, ServerConnectionEventArgs args)
        {

        }

        private void server_ExceptionOccurred(object sender, System.IO.ErrorEventArgs e)
        {

        }

        /*

                internal bool _silverlightsend(string message)
                {
                    bool sent = false;
                    //lock (this)
                    {
                        //Console.WriteLine(message);
                        try
                        {
                            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                            if (message.Length < 256)
                            {
                                String sf = new String(' ', 256 - message.Length);
                                message += sf;
                            }
                            sent = _server.SendAll(encoding.GetBytes(message));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(_pluginname + " unexpected error while sending data: " + e.Message);
                        }
                    }
                    return sent;
                } 

        */


    }

}
