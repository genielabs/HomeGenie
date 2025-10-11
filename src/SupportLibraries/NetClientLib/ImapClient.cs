/*
   Copyright 2012-2025 G-Labs (https://github.com/genielabs)

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU Affero General Public License as
   published by the Free Software Foundation, either version 3 of the
   License, or (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Affero General Public License for more details.

   You should have received a copy of the GNU Affero General Public License
   along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

/*
*     Author: Generoso Martello <gene@homegenie.it>
*     Project Homepage: https://homegenie.it
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;

namespace NetClientLib
{
    public class ImapClient
    {
        protected System.Net.Sockets.TcpClient _tcpClient;
        protected StreamReader _reader;
        protected StreamWriter _writer;

        protected string _selectedFolder = string.Empty;
        protected int _prefix = 1;

        public ImapClient(string host, int port, bool ssl = false)
        {
            try
            {
                _tcpClient = new System.Net.Sockets.TcpClient(host, port);

                if (ssl)
                {
                    var stream = new SslStream(_tcpClient.GetStream());
                    stream.AuthenticateAsClient(host);

                    _reader = new StreamReader(stream);
                    _writer = new StreamWriter(stream);
                }
                else
                {
                    var stream = _tcpClient.GetStream();
                    _reader = new StreamReader(stream);
                    _writer = new StreamWriter(stream);
                }
                string greeting = _reader.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void Authenticate(string user, string pass)
        {
            this.SendCommand(string.Format("LOGIN {0} {1}", user, pass));
            string response = this.GetResponse();
        }

        public List<string> GetFolders()
        {
            this.SendCommand("LIST \"\" *");
            string response = this.GetResponse();

            string[] lines = response.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            List<string> folders = new List<string>();

            foreach (string line in lines)
            {
                MatchCollection m = Regex.Matches(line, "\\\"(.*?)\\\"");

                if (m.Count > 1)
                {
                    string folderName = m[m.Count - 1].ToString().Trim(new char[] { '"' });
                    folders.Add(folderName);
                }
            }

            return folders;
        }

        public void SelectFolder(string folderName)
        {
            this._selectedFolder = folderName;
            this.SendCommand("SELECT " + folderName);
            string response = this.GetResponse();
        }

        public int GetMessageCount()
        {
            this.SendCommand("STATUS " + this._selectedFolder + " (messages)");
            string response = this.GetResponse();
            Match m = Regex.Match(response, "[0-9]*[0-9]");
            return Convert.ToInt32(m.ToString());
        }

        public int GetUnseenMessageCount()
        {
            this.SendCommand("STATUS " + this._selectedFolder + " (unseen)");
            string response = this.GetResponse();
            Match m = Regex.Match(response, "[0-9]*[0-9]");
            return Convert.ToInt32(m.ToString());
        }

        public string GetMessage(string uid, string section)
        {
            this.SendCommand("UID FETCH " + uid + " BODY[" + section + "]");
            return this._GetMessage();
        }

        public string GetMessage(int index, string section)
        {
            this.SendCommand("FETCH " + index + " BODY[" + section + "]");
            return this._GetMessage();
        }

        protected string _GetMessage()
        {
            string line = _reader.ReadLine();
            MatchCollection m = Regex.Matches(line, "\\{(.*?)\\}");

            if (m.Count > 0)
            {
                int length = Convert.ToInt32(m[0].ToString().Trim(new char[] { '{', '}' }));

                char[] buffer = new char[length];
                int read = (length < 128) ? length : 128;
                int remaining = length;
                int offset = 0;
                while (true)
                {
                    read = _reader.Read(buffer, offset, read);
                    remaining -= read;
                    offset += read;
                    read = (remaining >= 128) ? 128 : remaining;

                    if (remaining == 0)
                    {
                        break;
                    }
                }
                return new String(buffer);
            }
            return "";
        }

        protected void SendCommand(string cmd)
        {
            _writer.WriteLine("A" + _prefix.ToString() + " " + cmd);
            _writer.Flush();
            _prefix++;
        }

        protected string GetResponse()
        {
            string response = string.Empty;

            while (true)
            {
                string line = _reader.ReadLine();
                string[] tags = line.Split(new char[] { ' ' });
                response += line + Environment.NewLine;
                if (tags[0].Substring(0, 1) == "A" && tags[1].Trim() == "OK" || tags[1].Trim() == "BAD" || tags[1].Trim() == "NO")
                {
                    break;
                }

            }

            return response;
        }
    }

}
