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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OSC.NET;
using OSC.NET.Implementations.TUIO;

namespace TUIOLib
{

    public struct AccelerometerData
    {
        public double X, Y, Z;
    }

    public enum TuioCursorCommand
    {
        Set,
        Alive,
        FrameSequence
    }

    public class CursorUpdateEventArgs : EventArgs
    {
        public TuioCursorCommand Command { get; set; }
        public TUIOData CursorData { get; set; }
    }
    public class AccelerometerUpdateEventArgs : EventArgs
    {
        public AccelerometerData AccelerometerData { get; set; }
    }


    public class TUIOReceiver : TUIOListener
    {
        public event EventHandler<CursorUpdateEventArgs> CursorUpdate;
        public event EventHandler<AccelerometerUpdateEventArgs> AccelerometerUpdate;

        // Cursor2dUpdateEventArgs
        // AccelerometerUpdateEventArgs

        private int currentFrame = 0;
        private int lastFrame = 0;
        private TUIOClient tuioClient = null;
        private bool accelerationEnable = false;

        public TUIOReceiver()
        {
            tuioClient = new TUIOClient(3333);
            tuioClient.addListener(this);
        }

        public TUIOReceiver(int port)
        {
            tuioClient = new TUIOClient(port);
            tuioClient.addListener(this);
        }

        public bool processMessage(OSCMessage message)
        {
            string address = message.Address;
            ArrayList args = message.Values;


            //Console.WriteLine(address + " > " + " , " + args[0] + " , " + args[1] + " , " + args[2] + " , " + args[3]);


            if (address == "/acceleration/xyz")
            {
                if (!accelerationEnable)
                    return false;

                float x = (float)args[0];
                float y = (float)args[1];
                float z = (float)args[2];

                if (AccelerometerUpdate != null)
                {
                    AccelerometerUpdateEventArgs eventargs = new AccelerometerUpdateEventArgs() { AccelerometerData = new AccelerometerData() { X = x, Y = y, Z = z } };
                    AccelerometerUpdate(this, eventargs);
                }

                return true;
            }
            else if (address == "/tuio/2Dobj")
            {

                // TODO: FIDUCIALS TRACKING NOT TESTED, comment out and test =)
                /*           
                string command = (string)args[0];

                if ((command == "set") && (_currentframe >= _lastframe))
                {
                    int s_id = (int)args[1];
                    int f_id = (int)args[2];
                    float x = (float)args[3];
                    float y = (float)args[4];
                    float a = (float)args[5];
                    float X = (float)args[6];
                    float Y = (float)args[7];
                    float A = (float)args[8];
                    float m = (float)args[9];
                    float r = (float)args[10];

                    if (!_objects.ContainsKey(args[1]))
                    {
                        TUIOData t = new TUIOData(f_id, x, y, a);
                        _objects.Add(s_id, t);
                        addTuioObj(s_id, f_id);
                        updateTuioObj(s_id, f_id, x, y, a, X, Y, A, m, r);
                    }
                    else
                    {
                        TUIOData t = (TUIOData)_objects[s_id];
                        if ((t.X != x) || (t.Y != y) || (t.Angle != a))
                        {
                            updateTuioObj(s_id, f_id, x, y, a, X, Y, A, m, r);
                            t.update(x, y, a);
                            _objects[s_id] = t;
                        }
                    }
                }
                else if ((command == "alive") && (_currentframe >= _lastframe))
                {

                    for (int i = 1; i < args.Count; i++)
                    {
                        // get the message content
                        _newobjects.Add((int)args[i]);
                        // reduce the object list to the lost objects
                        if (_aliveobjects.Contains(args[i]))
                            _aliveobjects.Remove(args[i]);
                    }

                    // remove the remaining objects
                    for (int i = 0; i < _aliveobjects.Count; i++)
                    {
                        int s_id = (int)_aliveobjects[i];
                        int f_id = ((TUIOData)_objects[_aliveobjects[i]]).f_id;
                        _objects.Remove(_aliveobjects[i]);
                        removeTuioObj(s_id, f_id);
                    }


                    ArrayList buffer = _aliveobjects;
                    _aliveobjects = _newobjects;

                    // recycling of the ArrayList
                    _newobjects = buffer;
                    _newobjects.Clear();
                }
                else if (command == "fseq")
                {
                    _lastframe = _currentframe;
                    _currentframe = (int)args[1];
                    if (_currentframe == -1) _currentframe = _lastframe;

                    if (_currentframe >= _lastframe)
                    {
                        refresh();
                    }
                }

//                _plugincontrol.UpdateMonitor(_cursors, _acceleration);

                return true; */
            }
            else if (address == "/tuio/2Dcur")
            {
                string command = (string)args[0];

                if ((command == "set") && (currentFrame >= lastFrame))
                {
                    int s_id = (int)args[1];
                    float x = (float)args[2];
                    float y = (float)args[3];
                    float X = (float)args[4];
                    float Y = (float)args[5];
                    float m = (float)args[6];

                    if (CursorUpdate != null)
                    {
                        CursorUpdateEventArgs eventargs = new CursorUpdateEventArgs() { Command = TuioCursorCommand.Set, CursorData = new TUIOData(s_id, x, y, X, Y, m) };
                        CursorUpdate(this, eventargs);
                    }

                }
                else if ((command == "alive") && (currentFrame >= lastFrame))
                {
                    CursorUpdateEventArgs eventargs = new CursorUpdateEventArgs() { Command = TuioCursorCommand.Alive, CursorData = new TUIOData(args) };
                    CursorUpdate(this, eventargs);
                }

                return true;
            }
            //
            // else... unknown/unsupported command =/
            //
            return false;
        }
    }
}
