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

using OpenSource.UPnP;
using OpenSource.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MIG.Interfaces.Protocols
{
    public class UPnP : MIGInterface
    {


        #region Implemented MIG Commands

        // typesafe enum
        public sealed class Command : GatewayCommand
        {

            public static Dictionary<int, string> CommandsList = new Dictionary<int, string>() {
                { 701, "Control.On" },
                { 702, "Control.Off" },
                { 705, "Control.Level" },
                { 706, "Control.Toggle" },

                { 801, "AvMedia.Browse" },
                { 802, "AvMedia.GetUri" },
                { 803, "AvMedia.SetUri" },
                { 804, "AvMedia.GetTransportInfo" },
                { 805, "AvMedia.GetMediaInfo" },
                { 806, "AvMedia.GetPositionInfo" },

                { 807, "AvMedia.Play" },
                { 808, "AvMedia.Pause" },
                { 809, "AvMedia.Stop" },

                { 810, "AvMedia.Previous" },
                { 811, "AvMedia.Next" },
                { 812, "AvMedia.SetNext" },

                { 813, "AvMedia.GetMute" },
                { 814, "AvMedia.SetMute" },
                { 815, "AvMedia.GetVolume" },
                { 816, "AvMedia.SetVolume" },

            };

            // <context>.<command> enum   -   eg. Control.On where <context> :== "Control" and <command> :== "On"
            public static readonly Command CONTROL_ON = new Command(701);
            public static readonly Command CONTROL_OFF = new Command(702);
            public static readonly Command CONTROL_LEVEL = new Command(705);
            public static readonly Command CONTROL_TOGGLE = new Command(706);

            public static readonly Command AVMEDIA_BROWSE = new Command(801);
            public static readonly Command AVMEDIA_GETURI = new Command(802);
            public static readonly Command AVMEDIA_SETURI = new Command(803);
            public static readonly Command AVMEDIA_GETTRANSPORTINFO = new Command(804);
            public static readonly Command AVMEDIA_GETMEDIAINFO = new Command(805);
            public static readonly Command AVMEDIA_GETPOSITIONINFO = new Command(806);

            public static readonly Command AVMEDIA_PLAY = new Command(807);
            public static readonly Command AVMEDIA_PAUSE = new Command(808);
            public static readonly Command AVMEDIA_STOP = new Command(809);

            public static readonly Command AVMEDIA_PREVIOUS = new Command(810);
            public static readonly Command AVMEDIA_NEXT = new Command(811);
            public static readonly Command AVMEDIA_SETNEXT = new Command(812);

            public static readonly Command AVMEDIA_GETMUTE = new Command(813);
            public static readonly Command AVMEDIA_SETMUTE = new Command(814);
            public static readonly Command AVMEDIA_GETVOLUME = new Command(815);
            public static readonly Command AVMEDIA_SETVOLUME = new Command(816);

            private readonly String name;
            private readonly int value;

            private Command(int value)
            {
                this.name = CommandsList[value];
                this.value = value;
            }

            public Dictionary<int, string> ListCommands()
            {
                return Command.CommandsList;
            }

            public int Value
            {
                get { return this.value; }
            }

            public override String ToString()
            {
                return name;
            }

            public static implicit operator String(Command a)
            {
                return a.ToString();
            }

            public static explicit operator Command(int idx)
            {
                return new Command(idx);
            }

            public static explicit operator Command(string str)
            {
                if (CommandsList.ContainsValue(str))
                {
                    var cmd = from c in CommandsList
                                             where c.Value == str
                                             select c.Key;
                    return new Command(cmd.First());
                }
                else
                {
                    throw new InvalidCastException();
                }
            }

            public static bool operator ==(Command a, Command b)
            {
                return a.value == b.value;
            }

            public static bool operator !=(Command a, Command b)
            {
                return a.value != b.value;
            }
        }

        #endregion


        private UpnpSmartControlPoint controPoint;
        private bool isConnected = false;
        private UPnPDevice localDevice;

        public UPnP()
        {

        }

        #region MIG Interface members

        public event Action<InterfaceModulesChangedAction> InterfaceModulesChangedAction;
        public event Action<InterfacePropertyChangedAction> InterfacePropertyChangedAction;

        public string Domain
        {
            get
            {
                string ifacedomain = this.GetType().Namespace.ToString();
                ifacedomain = ifacedomain.Substring(ifacedomain.LastIndexOf(".") + 1) + "." + this.GetType().Name.ToString();
                return ifacedomain;
            }
        }

        public List<MIGServiceConfiguration.Interface.Option> Options { get; set; }

        public List<InterfaceModule> GetModules()
        {
            List<InterfaceModule> modules = new List<InterfaceModule>();

            for (int d = 0; d < this.UpnpControlPoint.DeviceTable.Count; d++)
            {
                var device = (UPnPDevice)(this.UpnpControlPoint.DeviceTable[d]);
                InterfaceModule module = new InterfaceModule();
                module.Domain = this.Domain;
                module.Address = device.UniqueDeviceName;
                module.Description = device.FriendlyName + " (" + device.ModelName + ")";
                module.ModuleType = MIG.ModuleTypes.Sensor;
                if (device.StandardDeviceType == "MediaRenderer")
                {
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = device.UniqueDeviceName,
                        SourceType = "UPnP " + device.FriendlyName,
                        Path = "UPnP.DeviceType",
                        Value = device.StandardDeviceType
                    });
                    module.ModuleType = MIG.ModuleTypes.MediaReceiver;
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = device.UniqueDeviceName,
                        SourceType = "UPnP " + device.FriendlyName,
                        Path = "Widget.DisplayModule",
                        Value = "homegenie/generic/mediareceiver"
                    });
                }
                else if (device.StandardDeviceType == "MediaServer")
                {
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = device.UniqueDeviceName,
                        SourceType = "UPnP " + device.FriendlyName,
                        Path = "Widget.DisplayModule",
                        Value = "homegenie/generic/mediaserver"
                    });
                }
                else if (device.StandardDeviceType == "SwitchPower")
                {
                    module.ModuleType = MIG.ModuleTypes.Switch;
                }
                else if (device.StandardDeviceType == "BinaryLight")
                {
                    module.ModuleType = MIG.ModuleTypes.Light;
                }
                else if (device.StandardDeviceType == "DimmableLight")
                {
                    module.ModuleType = MIG.ModuleTypes.Dimmer;
                }
                else if (device.HasPresentation)
                {
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = device.UniqueDeviceName,
                        SourceType = "UPnP " + device.FriendlyName,
                        Path = "Widget.DisplayModule",
                        Value = "homegenie/generic/link"
                    });
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = device.UniqueDeviceName,
                        SourceType = "UPnP " + device.FriendlyName,
                        Path = "FavouritesLink.Url",
                        Value = device.PresentationURL
                    });
                }

                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.DeviceURN",
                    Value = device.DeviceURN
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.DeviceURN_Prefix",
                    Value = device.DeviceURN_Prefix
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.FriendlyName",
                    Value = device.FriendlyName
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.LocationURL",
                    Value = device.LocationURL
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.Version",
                    Value = device.Major + "." + device.Minor
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.ModelName",
                    Value = device.ModelName
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.ModelNumber",
                    Value = device.ModelNumber
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.ModelDescription",
                    Value = device.ModelDescription
                });

                if (device.ModelURL != null)
                {
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = device.UniqueDeviceName,
                        SourceType = "UPnP " + device.FriendlyName,
                        Path = "UPnP.ModelURL",
                        Value = device.ModelURL.ToString()
                    });
                }

                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.Manufacturer",
                    Value = device.Manufacturer
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.ManufacturerURL",
                    Value = device.ManufacturerURL
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.PresentationURL",
                    Value = device.PresentationURL
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.UniqueDeviceName",
                    Value = device.UniqueDeviceName
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.SerialNumber",
                    Value = device.SerialNumber
                });
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.StandardDeviceType",
                    Value = device.StandardDeviceType
                });

                modules.Add(module);
            }

            return modules;
        }

        public bool IsConnected
        {
            get { return isConnected; }
        }

        public bool Connect()
        {
            if (controPoint == null)
            {
                controPoint = new UpnpSmartControlPoint();
                controPoint.OnAddedDevice += controPoint_OnAddedDevice;
                isConnected = true;
            }
            if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction() { Domain = this.Domain });
            return true;

        }

        public void Disconnect()
        {
            if (localDevice != null)
            {
                localDevice.StopDevice();
                localDevice = null;
            }
            if (controPoint != null)
            {
                controPoint.OnAddedDevice -= controPoint_OnAddedDevice;
                controPoint = null;
            }
            isConnected = false;
        }

        public bool IsDevicePresent()
        {
            return true;
        }

        public object InterfaceControl(MIGInterfaceCommand request)
        {
            string returnValue = "";
            bool raisePropertyChanged = false;
            string parameterPath = "Status.Unhandled";
            string raiseParameter = "";
            //
            var device = GetUpnpDevice(request.NodeId);

            //////////////////// Commands: SwitchPower, Dimming
            if (request.Command == Command.CONTROL_ON || request.Command == Command.CONTROL_OFF)
            {
                bool commandValue = (request.Command == Command.CONTROL_ON ? true : false);
                var newValue = new UPnPArgument("newTargetValue", commandValue);
                var args = new UPnPArgument[] { 
                    newValue
                };
                InvokeUpnpDeviceService(device, "SwitchPower", "SetTarget", args);
                //
                raisePropertyChanged = true;
                parameterPath = "Status.Level";
                raiseParameter = (commandValue ? "1" : "0");
            }
            else if (request.Command == Command.CONTROL_LEVEL)
            {
                var newvalue = new UPnPArgument("NewLoadLevelTarget", (byte)uint.Parse(request.GetOption(0)));
                var args = new UPnPArgument[] { 
                    newvalue
                };
                InvokeUpnpDeviceService(device, "Dimming", "SetLoadLevelTarget", args);
                //
                raisePropertyChanged = true;
                parameterPath = "Status.Level";
                raiseParameter = (double.Parse(request.GetOption(0)) / 100d).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (request.Command == Command.CONTROL_TOGGLE)
            {
            }
            //////////////////// Commands: Browse, AVTransport
            else if (request.Command == Command.AVMEDIA_GETURI)
            {
                string deviceId = request.NodeId;
                string id = request.GetOption(0);
                //
                var objectId = new UPnPArgument("ObjectID", id);
                var flags = new UPnPArgument("BrowseFlag", "BrowseMetadata");
                var filter = new UPnPArgument(
                                 "Filter",
                                 "upnp:album,upnp:artist,upnp:genre,upnp:title,res@size,res@duration,res@bitrate,res@sampleFrequency,res@bitsPerSample,res@nrAudioChannels,res@protocolInfo,res@protection,res@importUri"
                             );
                var startIndex = new UPnPArgument("StartingIndex", (uint)0);
                var requestedCount = new UPnPArgument("RequestedCount", (uint)1);
                var sortCriteria = new UPnPArgument("SortCriteria", "");
                //
                var result = new UPnPArgument("Result", "");
                var returnedNumber = new UPnPArgument("NumberReturned", "");
                var totalMatches = new UPnPArgument("TotalMatches", "");
                var updateId = new UPnPArgument("UpdateID", "");
                //
                InvokeUpnpDeviceService(device, "ContentDirectory", "Browse", new UPnPArgument[] { 
                    objectId,
                    flags,
                    filter,
                    startIndex,
                    requestedCount,
                    sortCriteria,
                    result,
                    returnedNumber,
                    totalMatches,
                    updateId
                });
                //
                try
                {
                    string ss = result.DataValue.ToString();
                    var item = XDocument.Parse(ss, LoadOptions.SetBaseUri).Descendants().Where(ii => ii.Name.LocalName == "item").First();
                    //
                    foreach (var i in item.Elements())
                    {
                        var protocolUri = i.Attribute("protocolInfo");
                        if (protocolUri != null)
                        {
                            returnValue = i.Value;
                            break;
                        }
                    }
                }
                catch
                {
                }

            }
            else if (request.Command == Command.AVMEDIA_BROWSE)
            {
                string deviceId = request.NodeId;
                string id = request.GetOption(0);
                //
                var objectId = new UPnPArgument("ObjectID", id);
                var flags = new UPnPArgument("BrowseFlag", "BrowseDirectChildren");
                var filter = new UPnPArgument(
                                 "Filter",
                                 "upnp:album,upnp:artist,upnp:genre,upnp:title,res@size,res@duration,res@bitrate,res@sampleFrequency,res@bitsPerSample,res@nrAudioChannels,res@protocolInfo,res@protection,res@importUri"
                             );
                var startIndex = new UPnPArgument("StartingIndex", (uint)0);
                var requestedCount = new UPnPArgument("RequestedCount", (uint)0);
                var sortCriteria = new UPnPArgument("SortCriteria", "");
                //
                var result = new UPnPArgument("Result", "");
                var returnedNumber = new UPnPArgument("NumberReturned", "");
                var totalMatches = new UPnPArgument("TotalMatches", "");
                var updateId = new UPnPArgument("UpdateID", "");
                //
                InvokeUpnpDeviceService(device, "ContentDirectory", "Browse", new UPnPArgument[] { 
                    objectId,
                    flags,
                    filter,
                    startIndex,
                    requestedCount,
                    sortCriteria,
                    result,
                    returnedNumber,
                    totalMatches,
                    updateId
                });
                //
                try
                {
                    string ss = result.DataValue.ToString();
                    var root = XDocument.Parse(ss, LoadOptions.SetBaseUri).Elements();
                    //
                    string jsonres = "[";
                    foreach (var i in root.Elements())
                    {
                        string itemId = i.Attribute("id").Value;
                        string itemTitle = i.Descendants().Where(n => n.Name.LocalName == "title").First().Value;
                        string itemClass = i.Descendants().Where(n => n.Name.LocalName == "class").First().Value;
                        jsonres += "{ \"Id\" : \"" + itemId + "\", \"Title\" : \"" + itemTitle + "\", \"Class\" : \"" + itemClass + "\" },\n";
                    }
                    jsonres = jsonres.TrimEnd(',', '\n') + "]";
                    //
                    returnValue = jsonres;
                }
                catch
                {
                }

            }
            else if (request.Command == Command.AVMEDIA_GETTRANSPORTINFO)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var transportState = new UPnPArgument("CurrentTransportState", "");
                var transportStatus = new UPnPArgument("CurrentTransportStatus", "");
                var currentSpeed = new UPnPArgument("CurrentSpeed", "");
                var args = new UPnPArgument[] { 
                    instanceId,
                    transportState,
                    transportStatus,
                    currentSpeed
                };
                InvokeUpnpDeviceService(device, "AVTransport", "GetTransportInfo", args);
                //
                string jsonres = "[{ ";
                jsonres += "\"CurrentTransportState\" : \"" + transportState.DataValue + "\", ";
                jsonres += "\"CurrentTransportStatus\" : \"" + transportStatus.DataValue + "\", ";
                jsonres += "\"CurrentSpeed\" : \"" + currentSpeed.DataValue + "\"";
                jsonres += " }]";
                //
                returnValue = jsonres;
            }
            else if (request.Command == Command.AVMEDIA_GETMEDIAINFO)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var nrTracks = new UPnPArgument("NrTracks", (uint)0);
                var mediaDuration = new UPnPArgument("MediaDuration", "");
                var currentUri = new UPnPArgument("CurrentURI", "");
                var currentUriMetadata = new UPnPArgument("CurrentURIMetaData", "");
                var nextUri = new UPnPArgument("NextURI", "");
                var nextUriMetadata = new UPnPArgument("NextURIMetaData", "");
                var playMedium = new UPnPArgument("PlayMedium", "");
                var recordMedium = new UPnPArgument("RecordMedium", "");
                var writeStatus = new UPnPArgument("WriteStatus", "");
                var args = new UPnPArgument[] { 
                    instanceId,
                    nrTracks,
                    mediaDuration,
                    currentUri,
                    currentUriMetadata,
                    nextUri,
                    nextUriMetadata,
                    playMedium,
                    recordMedium,
                    writeStatus
                };
                InvokeUpnpDeviceService(device, "AVTransport", "GetMediaInfo", args);
                //
                string jsonres = "[{ ";
                jsonres += "\"NrTracks\" : \"" + nrTracks.DataValue + "\", ";
                jsonres += "\"MediaDuration\" : \"" + mediaDuration.DataValue + "\", ";
                jsonres += "\"CurrentURI\" : \"" + currentUri.DataValue + "\", ";
                jsonres += "\"CurrentURIMetaData\" : \"" + currentUriMetadata.DataValue + "\", ";
                jsonres += "\"NextURI\" : \"" + nextUri.DataValue + "\", ";
                jsonres += "\"NextURIMetaData\" : \"" + nextUriMetadata.DataValue + "\", ";
                jsonres += "\"PlayMedium\" : \"" + playMedium.DataValue + "\", ";
                jsonres += "\"RecordMedium\" : \"" + recordMedium.DataValue + "\", ";
                jsonres += "\"WriteStatus\" : \"" + writeStatus.DataValue + "\"";
                jsonres += " }]";
                //
                returnValue = jsonres;
            }
            else if (request.Command == Command.AVMEDIA_GETPOSITIONINFO)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var currentTrack = new UPnPArgument("Track", (uint)0);
                var trackDuration = new UPnPArgument("TrackDuration", "");
                var trackMetadata = new UPnPArgument("TrackMetaData", "");
                var trackUri = new UPnPArgument("TrackURI", "");
                var relativeTime = new UPnPArgument("RelTime", "");
                var absoluteTime = new UPnPArgument("AbsTime", "");
                var relativeCount = new UPnPArgument("RelCount", (uint)0);
                var absoluteCount = new UPnPArgument("AbsCount", (uint)0);
                var args = new UPnPArgument[] { 
                    instanceId,
                    currentTrack,
                    trackDuration,
                    trackMetadata,
                    trackUri,
                    relativeTime,
                    absoluteTime,
                    relativeCount,
                    absoluteCount
                };
                InvokeUpnpDeviceService(device, "AVTransport", "GetPositionInfo", args);
                //
                string jsonres = "[{";
                jsonres += "\"Track\" : \"" + currentTrack.DataValue + "\",";
                jsonres += "\"TrackDuration\" : \"" + trackDuration.DataValue + "\",";
                jsonres += "\"TrackMetaData\" : \"" + trackMetadata.DataValue + "\",";
                jsonres += "\"TrackURI\" : \"" + trackUri.DataValue + "\",";
                jsonres += "\"RelTime\" : \"" + relativeTime.DataValue + "\",";
                jsonres += "\"AbsTime\" : \"" + absoluteTime.DataValue + "\",";
                jsonres += "\"RelCount\" : \"" + relativeCount.DataValue + "\",";
                jsonres += "\"AbsCount\" : \"" + absoluteCount.DataValue + "\"";
                jsonres += "}]";
                //
                returnValue = jsonres;
            }
            else if (request.Command == Command.AVMEDIA_SETURI)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var currentUri = new UPnPArgument("CurrentURI", request.GetOption(0));
                var uriMetadata = new UPnPArgument("CurrentURIMetaData", "");
                var args = new UPnPArgument[] { 
                    instanceId,
                    currentUri,
                    uriMetadata
                };
                InvokeUpnpDeviceService(device, "AVTransport", "SetAVTransportURI", args);
            }
            else if (request.Command == Command.AVMEDIA_PLAY)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var speed = new UPnPArgument("Speed", "1");
                var args = new UPnPArgument[] { 
                    instanceId,
                    speed
                };
                InvokeUpnpDeviceService(device, "AVTransport", "Play", args);
            }
            else if (request.Command == Command.AVMEDIA_PAUSE)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var args = new UPnPArgument[] { 
                    instanceId
                };
                InvokeUpnpDeviceService(device, "AVTransport", "Pause", args);
            }
            else if (request.Command == Command.AVMEDIA_STOP)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var args = new UPnPArgument[] { 
                    instanceId
                };
                InvokeUpnpDeviceService(device, "AVTransport", "Stop", args);
            }
            else if (request.Command == Command.AVMEDIA_PREVIOUS)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var args = new UPnPArgument[] { 
                    instanceId
                };
                InvokeUpnpDeviceService(device, "AVTransport", "Previous", args);
            }
            else if (request.Command == Command.AVMEDIA_NEXT)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var args = new UPnPArgument[] { 
                    instanceId
                };
                InvokeUpnpDeviceService(device, "AVTransport", "Next", args);
            }
            else if (request.Command == Command.AVMEDIA_SETNEXT)
            {
            }
            else if (request.Command == Command.AVMEDIA_GETMUTE)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var channel = new UPnPArgument("Channel", "Master");
                var currentMute = new UPnPArgument("CurrentMute", "");
                var args = new UPnPArgument[] { 
                    instanceId,
                    channel,
                    currentMute
                };
                InvokeUpnpDeviceService(device, "RenderingControl", "GetMute", args);
                returnValue = currentMute.DataValue.ToString();
            }
            else if (request.Command == Command.AVMEDIA_SETMUTE)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var channel = new UPnPArgument("Channel", "Master");
                var mute = new UPnPArgument("DesiredMute", request.GetOption(0) == "1" ? true : false);
                var args = new UPnPArgument[] { 
                    instanceId,
                    channel,
                    mute
                };
                InvokeUpnpDeviceService(device, "RenderingControl", "SetMute", args);
            }
            else if (request.Command == Command.AVMEDIA_GETVOLUME)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var channel = new UPnPArgument("Channel", "Master");
                var currentVolume = new UPnPArgument("CurrentVolume", "");
                var args = new UPnPArgument[] { 
                    instanceId,
                    channel,
                    currentVolume
                };
                InvokeUpnpDeviceService(device, "RenderingControl", "GetVolume", args);
                returnValue = currentVolume.DataValue.ToString();
            }
            else if (request.Command == Command.AVMEDIA_SETVOLUME)
            {
                var instanceId = new UPnPArgument("InstanceID", (uint)0);
                var channel = new UPnPArgument("Channel", "Master");
                var volume = new UPnPArgument("DesiredVolume", UInt16.Parse(request.GetOption(0)));
                var args = new UPnPArgument[] { 
                    instanceId,
                    channel,
                    volume
                };
                InvokeUpnpDeviceService(device, "RenderingControl", "SetVolume", args);
            }


            // signal event
            if (raisePropertyChanged && InterfacePropertyChangedAction != null)
            {
                try
                {
                    InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                        Domain = this.Domain,
                        SourceId = device.UniqueDeviceName,
                        SourceType = "UPnP " + (device != null ? device.StandardDeviceType : "device"),
                        Path = parameterPath,
                        Value = raiseParameter
                    });
                }
                catch
                {
                }
            }


            return returnValue;
        }

        #endregion



        #region non-MIGInterface public members

        public UpnpSmartControlPoint UpnpControlPoint
        {
            get { return controPoint; }
        }

        public void CreateLocalDevice(
            string deviceGuid,
            string deviceType,
            string presentationUrl,
            string rootDirectory,
            string modelName,
            string modelDescription,
            string modelUrl,
            string modelNumber,
            string manufacturer,
            string manufacturerUrl
        )
        {
            if (localDevice != null)
            {
                localDevice.StopDevice();
                localDevice = null;
            }
            //
            IPHostEntry host;
            //string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    //localIP = ip.ToString();
                    break;
                }
            }
            localDevice = UPnPDevice.CreateRootDevice(900, 1, rootDirectory);
            //hgdevice.Icon = null;
            if (presentationUrl != "")
            {
                localDevice.HasPresentation = true;
                localDevice.PresentationURL = presentationUrl;
            }
            localDevice.FriendlyName = modelName + ": " + Environment.MachineName;
            localDevice.Manufacturer = manufacturer;
            localDevice.ManufacturerURL = manufacturerUrl;
            localDevice.ModelName = modelName;
            localDevice.ModelDescription = modelDescription;
            if (Uri.IsWellFormedUriString(manufacturerUrl, UriKind.Absolute))
            {
                localDevice.ModelURL = new Uri(manufacturerUrl);
            }
            localDevice.ModelNumber = modelNumber;
            localDevice.StandardDeviceType = deviceType;
            localDevice.UniqueDeviceName = deviceGuid;
            localDevice.StartDevice();

        }

        #endregion




        private UPnPDevice GetUpnpDevice(string deviceId)
        {
            UPnPDevice device = null;
            foreach (UPnPDevice d in controPoint.DeviceTable)
            {
                if (d.UniqueDeviceName == deviceId)
                {
                    device = d;
                    break;
                }
            }
            return device;
        }

        private void InvokeUpnpDeviceService(
            UPnPDevice device,
            string serviceId,
            string methodName,
            params UPnPArgument[] args
        )
        {
            foreach (UPnPService s in device.Services)
            {
                if (s.ServiceID.StartsWith("urn:upnp-org:serviceId:" + serviceId))
                {
                    s.InvokeSync(methodName, args);
                }
            }
        }

        private void controPoint_OnAddedDevice(UpnpSmartControlPoint sender, UPnPDevice device)
        {
            if (InterfacePropertyChangedAction != null)
            {
                //foreach (UPnPService s in device.Services)
                //{
                //    s.Subscribe(1000, new UPnPService.UPnPEventSubscribeHandler(_subscribe_sink));
                //}
                InterfacePropertyChangedAction(new InterfacePropertyChangedAction() {
                    Domain = this.Domain,
                    SourceId = device.UniqueDeviceName,
                    SourceType = "UPnP " + device.FriendlyName,
                    Path = "UPnP.DeviceType",
                    Value = device.StandardDeviceType
                });
            }
            if (InterfaceModulesChangedAction != null) InterfaceModulesChangedAction(new InterfaceModulesChangedAction() { Domain = this.Domain });
        }

        //        private void _subscribe_sink(UPnPService sender, bool SubscribeOK)
        //        {
        //Console.WriteLine("\n\n\n" + sender.ServiceURN + "\n\n\n");
        //            if (SubscribeOK)
        //            {
        //                sender.OnUPnPEvent += sender_OnUPnPEvent;
        //            }
        //        }

        //        void sender_OnUPnPEvent(UPnPService sender, long SEQ)
        //        {
        //Console.WriteLine("\n\n\n" + sender.ServiceURN + " - " + SEQ + "\n\n\n");
        //        }


    }













    // original code from
    // https://code.google.com/p/phanfare-tools/
    // http://phanfare-tools.googlecode.com/svn/trunk/Phanfare.MediaServer/UPnP/Intel/UPNP/UPnPInternalSmartControlPoint.cs

    public sealed class UpnpSmartControlPoint
    {
        private ArrayList activeDeviceList = ArrayList.Synchronized(new ArrayList());
        private UPnPDeviceFactory deviceFactory = new UPnPDeviceFactory();
        private LifeTimeMonitor deviceLifeTimeClock = new LifeTimeMonitor();
        private Hashtable deviceTable = new Hashtable();
        private object deviceTableLock = new object();
        private LifeTimeMonitor deviceUpdateClock = new LifeTimeMonitor();
        private UPnPControlPoint genericControlPoint;
        private NetworkInfo hostNetworkInfo;
        private WeakEvent OnAddedDeviceEvent = new WeakEvent();
        private WeakEvent OnDeviceExpiredEvent = new WeakEvent();
        private WeakEvent OnRemovedDeviceEvent = new WeakEvent();
        private WeakEvent OnUpdatedDeviceEvent = new WeakEvent();
        private string searchFilter = "upnp:rootdevice";
        //"ssdp:all"; //


        public ArrayList DeviceTable
        {
            get { return activeDeviceList; }
        }

        public event DeviceHandler OnAddedDevice
        {
            add
            {
                this.OnAddedDeviceEvent.Register(value);
            }
            remove
            {
                this.OnAddedDeviceEvent.UnRegister(value);
            }
        }

        public event DeviceHandler OnDeviceExpired
        {
            add
            {
                this.OnDeviceExpiredEvent.Register(value);
            }
            remove
            {
                this.OnDeviceExpiredEvent.UnRegister(value);
            }
        }

        public event DeviceHandler OnRemovedDevice
        {
            add
            {
                this.OnRemovedDeviceEvent.Register(value);
            }
            remove
            {
                this.OnRemovedDeviceEvent.UnRegister(value);
            }
        }

        public event DeviceHandler OnUpdatedDevice
        {
            add
            {
                this.OnUpdatedDeviceEvent.Register(value);
            }
            remove
            {
                this.OnUpdatedDeviceEvent.UnRegister(value);
            }
        }

        public UpnpSmartControlPoint()
        {
            this.deviceFactory.OnDevice += new UPnPDeviceFactory.UPnPDeviceHandler(this.DeviceFactoryCreationSink);
            this.deviceLifeTimeClock.OnExpired += new LifeTimeMonitor.LifeTimeHandler(this.DeviceLifeTimeClockSink);
            this.deviceUpdateClock.OnExpired += new LifeTimeMonitor.LifeTimeHandler(this.DeviceUpdateClockSink);
            this.hostNetworkInfo = new NetworkInfo(new NetworkInfo.InterfaceHandler(this.NetworkInfoNewInterfaceSink));
            this.hostNetworkInfo.OnInterfaceDisabled += new NetworkInfo.InterfaceHandler(this.NetworkInfoOldInterfaceSink);
            this.genericControlPoint = new UPnPControlPoint(this.hostNetworkInfo);
            this.genericControlPoint.OnSearch += new UPnPControlPoint.SearchHandler(this.UPnPControlPointSearchSink);
            this.genericControlPoint.OnNotify += new SSDP.NotifyHandler(this.SSDPNotifySink);
            this.genericControlPoint.FindDeviceAsync(searchFilter);
        }

        private void DeviceFactoryCreationSink(UPnPDeviceFactory sender, UPnPDevice device, Uri locationURL)
        {
            //Console.WriteLine("UPnPDevice[" + device.FriendlyName + "]@" + device.LocationURL + " advertised UDN[" + device.UniqueDeviceName + "]");
            if (!this.deviceTable.Contains(device.UniqueDeviceName))
            {
                EventLogger.Log(
                    this,
                    EventLogEntryType.Error,
                    "UPnPDevice[" + device.FriendlyName + "]@" + device.LocationURL + " advertised UDN[" + device.UniqueDeviceName + "] in xml but not in SSDP"
                );
            }
            else
            {
                lock (this.deviceTableLock)
                {
                    DeviceInfo info2 = (DeviceInfo)this.deviceTable[device.UniqueDeviceName];
                    if (info2.Device != null)
                    {
                        EventLogger.Log(
                            this,
                            EventLogEntryType.Information,
                            "Unexpected UPnP Device Creation: " + device.FriendlyName + "@" + device.LocationURL
                        );
                        return;
                    }
                    DeviceInfo info = (DeviceInfo)this.deviceTable[device.UniqueDeviceName];
                    info.Device = device;
                    this.deviceTable[device.UniqueDeviceName] = info;
                    this.deviceLifeTimeClock.Add(device.UniqueDeviceName, device.ExpirationTimeout);
                    this.activeDeviceList.Add(device);
                }
                this.OnAddedDeviceEvent.Fire(this, device);
            }
        }

        private void DeviceLifeTimeClockSink(LifeTimeMonitor sender, object obj)
        {
            DeviceInfo info;
            lock (this.deviceTableLock)
            {
                if (!this.deviceTable.ContainsKey(obj))
                {
                    return;
                }
                info = (DeviceInfo)this.deviceTable[obj];
                this.deviceTable.Remove(obj);
                this.deviceUpdateClock.Remove(obj);
                if (this.activeDeviceList.Contains(info.Device))
                {
                    this.activeDeviceList.Remove(info.Device);
                }
                else
                {
                    info.Device = null;
                }
            }
            if (info.Device != null)
            {
                //info.Device.Removed();
            }
            if (info.Device != null)
            {
                //info.Device.Removed();
                this.OnDeviceExpiredEvent.Fire(this, info.Device);
            }
        }

        private void DeviceUpdateClockSink(LifeTimeMonitor sender, object obj)
        {
            lock (this.deviceTableLock)
            {
                if (this.deviceTable.ContainsKey(obj))
                {
                    DeviceInfo info = (DeviceInfo)this.deviceTable[obj];
                    if (info.PendingBaseURL != null)
                    {
                        info.BaseURL = info.PendingBaseURL;
                        info.MaxAge = info.PendingMaxAge;
                        info.SourceEP = info.PendingSourceEP;
                        info.LocalEP = info.PendingLocalEP;
                        info.NotifyTime = DateTime.Now;
                        info.Device.UpdateDevice(info.BaseURL, info.LocalEP.Address);
                        this.deviceTable[obj] = info;
                        this.deviceLifeTimeClock.Add(info.UDN, info.MaxAge);
                    }
                }
            }
        }

        public UPnPDevice[] GetCurrentDevices()
        {
            return (UPnPDevice[])this.activeDeviceList.ToArray(typeof(UPnPDevice));
        }

        private void NetworkInfoNewInterfaceSink(NetworkInfo sender, IPAddress Intfce)
        {
            if (this.genericControlPoint != null)
            {
                this.genericControlPoint.FindDeviceAsync(searchFilter);
            }
        }

        private void NetworkInfoOldInterfaceSink(NetworkInfo sender, IPAddress Intfce)
        {
            ArrayList list = new ArrayList();
            lock (this.deviceTableLock)
            {
                foreach (UPnPDevice device in this.GetCurrentDevices())
                {
                    if (device.InterfaceToHost.Equals(Intfce))
                    {
                        list.Add(this.UnprotectedRemoveMe(device));
                    }
                }
            }
            foreach (UPnPDevice device2 in list)
            {
                //device2.Removed();
                this.OnRemovedDeviceEvent.Fire(this, device2);
            }
            this.genericControlPoint.FindDeviceAsync(searchFilter);
        }

        internal void RemoveMe(UPnPDevice _d)
        {
            UPnPDevice parentDevice = _d;
            UPnPDevice device2 = null;
            while (parentDevice.ParentDevice != null)
            {
                parentDevice = parentDevice.ParentDevice;
            }
            lock (this.deviceTableLock)
            {
                if (!this.deviceTable.ContainsKey(parentDevice.UniqueDeviceName))
                {
                    return;
                }
                device2 = this.UnprotectedRemoveMe(parentDevice);
            }
            if (device2 != null)
            {
                //device2.Removed();
            }
            if (device2 != null)
            {
                this.OnRemovedDeviceEvent.Fire(this, device2);
            }
        }

        public void Rescan()
        {
            lock (this.deviceTableLock)
            {
                IDictionaryEnumerator enumerator = this.deviceTable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    string key = (string)enumerator.Key;
                    this.deviceLifeTimeClock.Add(key, 20);
                }
            }
            this.genericControlPoint.FindDeviceAsync(searchFilter);
        }

        internal void SSDPNotifySink(
            IPEndPoint source,
            IPEndPoint local,
            Uri LocationURL,
            bool IsAlive,
            string USN,
            string SearchTarget,
            int MaxAge,
            HTTPMessage Packet
        )
        {
            UPnPDevice device = null;
            if (SearchTarget == searchFilter)
            {
                if (!IsAlive)
                {
                    lock (this.deviceTableLock)
                    {
                        device = this.UnprotectedRemoveMe(USN);
                    }
                    if (device != null)
                    {
                        //device.Removed();
                    }
                    if (device != null)
                    {
                        this.OnRemovedDeviceEvent.Fire(this, device);
                    }
                }
                else
                {
                    lock (this.deviceTableLock)
                    {
                        if (!this.deviceTable.ContainsKey(USN))
                        {
                            DeviceInfo info = new DeviceInfo();
                            info.Device = null;
                            info.UDN = USN;
                            info.NotifyTime = DateTime.Now;
                            info.BaseURL = LocationURL;
                            info.MaxAge = MaxAge;
                            info.LocalEP = local;
                            info.SourceEP = source;
                            this.deviceTable[USN] = info;
                            this.deviceFactory.CreateDevice(info.BaseURL, info.MaxAge, IPAddress.Any, info.UDN);
                        }
                        else
                        {
                            DeviceInfo info2 = (DeviceInfo)this.deviceTable[USN];
                            if (info2.Device != null)
                            {
                                if (info2.BaseURL.Equals(LocationURL))
                                {
                                    this.deviceUpdateClock.Remove(info2);
                                    info2.PendingBaseURL = null;
                                    info2.PendingMaxAge = 0;
                                    info2.PendingLocalEP = null;
                                    info2.PendingSourceEP = null;
                                    info2.NotifyTime = DateTime.Now;
                                    this.deviceTable[USN] = info2;
                                    this.deviceLifeTimeClock.Add(info2.UDN, MaxAge);
                                }
                                else if (info2.NotifyTime.AddSeconds(10.0).Ticks < DateTime.Now.Ticks)
                                {
                                    info2.PendingBaseURL = LocationURL;
                                    info2.PendingMaxAge = MaxAge;
                                    info2.PendingLocalEP = local;
                                    info2.PendingSourceEP = source;
                                    this.deviceTable[USN] = info2;
                                    this.deviceUpdateClock.Add(info2.UDN, 3);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal UPnPDevice UnprotectedRemoveMe(UPnPDevice _d)
        {
            UPnPDevice parentDevice = _d;
            while (parentDevice.ParentDevice != null)
            {
                parentDevice = parentDevice.ParentDevice;
            }
            return this.UnprotectedRemoveMe(parentDevice.UniqueDeviceName);
        }

        internal UPnPDevice UnprotectedRemoveMe(string UDN)
        {
            UPnPDevice device = null;
            try
            {
                DeviceInfo info = (DeviceInfo)this.deviceTable[UDN];
                device = info.Device;
                this.deviceTable.Remove(UDN);
                this.deviceLifeTimeClock.Remove(info.UDN);
                this.deviceUpdateClock.Remove(info);
                this.activeDeviceList.Remove(device);
            }
            catch
            {
            }
            return device;
        }

        private void UPnPControlPointSearchSink(
            IPEndPoint source,
            IPEndPoint local,
            Uri LocationURL,
            string USN,
            string SearchTarget,
            int MaxAge
        )
        {
            lock (this.deviceTableLock)
            {
                if (!this.deviceTable.ContainsKey(USN))
                {
                    DeviceInfo info = new DeviceInfo();
                    info.Device = null;
                    info.UDN = USN;
                    info.NotifyTime = DateTime.Now;
                    info.BaseURL = LocationURL;
                    info.MaxAge = MaxAge;
                    info.LocalEP = local;
                    info.SourceEP = source;
                    this.deviceTable[USN] = info;
                    this.deviceFactory.CreateDevice(info.BaseURL, info.MaxAge, IPAddress.Any, info.UDN);
                }
                else
                {
                    DeviceInfo info2 = (DeviceInfo)this.deviceTable[USN];
                    if (info2.Device != null)
                    {
                        if (info2.BaseURL.Equals(LocationURL))
                        {
                            this.deviceUpdateClock.Remove(info2);
                            info2.PendingBaseURL = null;
                            info2.PendingMaxAge = 0;
                            info2.PendingLocalEP = null;
                            info2.PendingSourceEP = null;
                            info2.NotifyTime = DateTime.Now;
                            this.deviceTable[USN] = info2;
                            this.deviceLifeTimeClock.Add(info2.UDN, MaxAge);
                        }
                        else if (info2.NotifyTime.AddSeconds(10.0).Ticks < DateTime.Now.Ticks)
                        {
                            info2.PendingBaseURL = LocationURL;
                            info2.PendingMaxAge = MaxAge;
                            info2.PendingLocalEP = local;
                            info2.PendingSourceEP = source;
                            this.deviceUpdateClock.Add(info2.UDN, 3);
                        }
                    }
                }
            }
        }

        public delegate void DeviceHandler(UpnpSmartControlPoint sender, UPnPDevice Device);

        [StructLayout(LayoutKind.Sequential)]
        private struct DeviceInfo
        {
            public UPnPDevice Device;
            public DateTime NotifyTime;
            public string UDN;
            public Uri BaseURL;
            public int MaxAge;
            public IPEndPoint LocalEP;
            public IPEndPoint SourceEP;
            public Uri PendingBaseURL;
            public int PendingMaxAge;
            public IPEndPoint PendingLocalEP;
            public IPEndPoint PendingSourceEP;
        }
    }





}
