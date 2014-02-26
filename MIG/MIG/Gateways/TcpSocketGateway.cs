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
        public int Port;
    }

    class TcpSocketGateyRequest
    {
        public int ClientId;
        public byte[] Request;

        public TcpSocketGateyRequest(int clientid, byte[] request)
        {
            this.ClientId = clientid;
            this.Request = request;
        }
    }

    class TcpSocketGateway : MIGGateway
    {
        public event Action<object> ProcessRequest;

        private TCPServerChannel _server;
        private int _serviceport = 4502;

        public TcpSocketGateway()
        {

        }


        public void Start()
        {
            _server = NetworkConnectivity.CreateTCPServerChannel("server");
            _server.ChannelClientConnected += new ServerConnectionEventHandler(_server_ChannelClientConnected);
            _server.ChannelClientDisconnected += new ServerConnectionEventHandler(_server_ChannelClientDisconnected);
            //_server.ChannelConnected += 
            //_server.ChannelDisconnected += 
            _server.DataReceived += new ServerDataEventHandler(_server_DataReceived);
            //_server.DataSent += 
            //_server.ExceptionOccurred += 
            _server.ExceptionOccurred += new System.IO.ErrorEventHandler(_server_ExceptionOccurred);
            _server.Connect(_serviceport);
        }

        public void Configure(object gwconfiguration)
        {
            TcpSocketGatewayConfiguration cnf = (TcpSocketGatewayConfiguration)gwconfiguration;
            _serviceport = cnf.Port;
        }


        private void _server_ChannelClientConnected(object sender, ServerConnectionEventArgs args)
        {
            _server.Receive(256, args.ClientId);
        }

        private void _server_DataReceived(object sender, ServerDataEventArgs args)
        {

            if (ProcessRequest != null)
            {
                ProcessRequest(new TcpSocketGateyRequest(args.ClientId, args.Data)); // '\0's ending byte array
            }

            _server.Receive(256, (int)args.ClientId);

        }

        private void _server_ChannelClientDisconnected(object sender, ServerConnectionEventArgs args)
        {

        }

        private void _server_ExceptionOccurred(object sender, System.IO.ErrorEventArgs e)
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
