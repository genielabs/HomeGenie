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

using GLabs.Logging;

using HomeGenie.Service;

/*! \mainpage Extend, customize, create!
 *
 * \section docs HomeGenie Documentation
 *
 * <a href="http://genielabs.github.io/HomeGenie">http://genielabs.github.io/HomeGenie</a>
 *
 */

namespace HomeGenie.Automation.Scripting
{
    [Serializable]
    public class ScriptingHost
    {
        private HomeGenieService hgService;
        private static Logger _log = LogManager.GetCurrentClassLogger();


        public void SetHost(HomeGenieService hg, int programId)
        {
            hgService = hg;
            Net = new NetHelper(hgService);
            Program = new ProgramHelper(hgService, programId);
            Data = new DataHelper(hgService, programId);
            Api = new ApiHelper(hgService, programId);
            When = new EventsHelper(hgService, programId);
            SerialPort = new SerialPortHelper();
            TcpClient = new TcpClientHelper();
            UdpClient = new UdpClientHelper();
            MqttClient = new MqttClientHelper();
            KnxClient = new KnxClientHelper();
            Scheduler = new SchedulerHelper(hgService);
        }

        public ModulesManager Modules
        {
            get
            {
                return new ModulesManager(hgService);
            }
        }

        public SettingsHelper Settings
        {
            get
            {
                return new SettingsHelper(hgService);
            }
        }

        public NetHelper Net { get; private set; }

        public ProgramHelper Program { get; private set; }

        public DataHelper Data { get; private set; }

        public ApiHelper Api { get; private set; }

        // TODO: deprecate this alias
        public EventsHelper Events
        {
            get
            {
                return When;
            }
        }

        public EventsHelper When { get; private set; }

        public SerialPortHelper SerialPort { get; private set; }

        public TcpClientHelper TcpClient { get; private set; }

        public UdpClientHelper UdpClient { get; private set; }

        public MqttClientHelper MqttClient { get; private set; }

        public KnxClientHelper KnxClient { get; private set; }

        public SchedulerHelper Scheduler { get; private set; }

        public void Pause(double seconds)
        {
            System.Threading.Thread.Sleep((int)(seconds * 1000));
        }

        public void Delay(double seconds)
        {
            Pause(seconds);
        }

        public void Reset()
        {
            try
            {
                SerialPort.Reset();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error in resetting SerialPort: " + ex.Message);
            }

            try
            {
                TcpClient.Reset();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error in resetting TcpClient: " + ex.Message);
            }

            try
            {
                UdpClient.Reset();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error in resetting UdpClient: " + ex.Message);
            }

            try
            {
                Net.Reset();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error in resetting Net: " + ex.Message);
            }

            try
            {
                MqttClient.Reset();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error in resetting MqttClient: " + ex.Message);
            }

            try
            {
                KnxClient.Reset();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error in resetting KnxClient: " + ex.Message);
            }

            try
            {
                Program.Reset();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error in resetting Program: " + ex.Message);
            }
        }

    }

}
