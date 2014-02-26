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

using TUIOLib;

namespace MIG.Interfaces.MultiTouch
{
    public class TUIO : MIGInterface
    {
        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        public TUIO()
        {
            TUIOReceiver tuioreceiver = new TUIOReceiver();
            tuioreceiver.CursorUpdate += new EventHandler<CursorUpdateEventArgs>(tuioreceiver_CursorUpdate);
        }


        public string Domain
        {
            get
            {
                string ifacedomain = this.GetType().Namespace.ToString();
                ifacedomain = ifacedomain.Substring(ifacedomain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return ifacedomain;
            }
        }

        public bool Connect()
        {
            return true;
        }
        public void Disconnect()
        {

        }
        public bool IsDevicePresent()
        {
            return true;
        }
        public bool IsConnected
        {
            get { return true; }
        }

        public void WaitOnPending()
        {

        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            return "";
        }



        void tuioreceiver_CursorUpdate(object sender, CursorUpdateEventArgs e)
        {
            //Console.WriteLine("TUIO: " + e.Command.ToString() + " " + e.CursorData.f_id + ") " + e.CursorData.X + "," + e.CursorData.Y + "," + e.CursorData.Angle);

            InterfacePropertyChangedAction intact = new InterfacePropertyChangedAction();
            intact.Domain = this.Domain;
            intact.Path = e.Command.ToString();
            intact.Value = e.CursorData;
            intact.SourceId = e.CursorData.f_id.ToString();
            intact.SourceType = "TUIO.2dCursor"; // TUIO.2dObject
            //
            if (InterfacePropertyChangedAction != null)
            {
                InterfacePropertyChangedAction(intact);
            }

        }


    }
}

