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
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

using OpenNI;

namespace OpenKinectLib
{

    public class NiteKinectHandGestureEventData //: IPluginData
    {
        public NiteKinectHandGestureData NiteKinectHandGestureData { get; set; }

        public NiteKinectHandGestureEventData()
        {
            NiteKinectHandGestureData = new NiteKinectHandGestureData();
        }
        public NiteKinectHandGestureEventData(NiteKinectHandGestureData hgdata)
        {
            NiteKinectHandGestureData = hgdata;
        }
    }

    public class NiteKinectHandGestureData
    {
        public int HandId { get; set; }
        public NiteKinectHandState HandState { get; set; }
        public Point3D HandLocation { get; set; }

        public NiteKinectHandGestureData()
        {

        }

        public NiteKinectHandGestureData(int hid, NiteKinectHandState hstate, Point3D hlocation)
        {
            HandId = hid;
            HandState = hstate;
            HandLocation = hlocation;
        }
    }

    public class NiteKinectUserEventData //: IPluginData
    {
        public NiteKinectUserData NiteKinectUserData { get; set; }

        public NiteKinectUserEventData()
        {
            NiteKinectUserData = new NiteKinectUserData();
        }
        public NiteKinectUserEventData(NiteKinectUserData userdata)
        {
            NiteKinectUserData = userdata;
        }
    }

    public class NiteKinectUserData
    {
        public int UserId { get; set; }
        public NiteKinectUserState UserState { get; set; }
        public Point3D UserLocation { get; set; }
        public Dictionary<SkeletonJoint, SkeletonJointPosition> UserSkelton { get; set; }

        public NiteKinectUserData()
        {

        }

        public NiteKinectUserData(int uid, NiteKinectUserState ustate, Point3D ulocation)
        {
            UserId = uid;
            UserState = ustate;
            UserLocation = ulocation;
        }
    }

    public enum NiteKinectUserState
    {
        Unknown = -1,
        Entered = 0,
        Exited,
        Calibrating,
        LookingForPose,
        Tracking
    }

    public enum NiteKinectHandState
    {
        Unknown = -1,
        Created,
        Destroyed,
        Tracking,
        GestureClick,
        GestureRaiseHand
    }
    public class DataReceivedEventHandler : EventArgs
    {
        public object PluginData { get; set; }
    }


    public class OpenKinect
    {

        public event EventHandler<DataReceivedEventHandler> DataReceived;

        private Context context;
        private DepthGenerator depth;
        private UserGenerator userGenerator;
        private HandsGenerator handsGenerator;
        private GestureGenerator gestureGenerator;
        private SkeletonCapability skeletonCapbility;
        private PoseDetectionCapability poseDetectionCapability;
        private string calibPose;
        private Thread readerThread;
        private bool shouldRun;
        private Bitmap bitmap;
        private int[] histogram;

        private Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>> joints;

        public OpenKinect()
        {
        }


        public bool SetConfig(XmlElement xmlconfig)
        {
            // TO-DO: add some configuration parameters if of any use

            this.context = new Context("NiteKinectConfig.xml");
            this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            if (this.depth == null)
            {
                throw new Exception("Viewer must have a depth node!");
            }

            this.userGenerator = new UserGenerator(this.context);
            this.userGenerator.NewUser += userGenerator_NewUser;
            this.userGenerator.LostUser += userGenerator_LostUser;
            //
            this.skeletonCapbility = this.userGenerator.SkeletonCapability;
            this.skeletonCapbility.SetSkeletonProfile(SkeletonProfile.All);
            this.skeletonCapbility.CalibrationEnd += skeletonCapbility_CalibrationEnd;
            //
            this.poseDetectionCapability = this.userGenerator.PoseDetectionCapability;
            this.calibPose = this.skeletonCapbility.CalibrationPose;
            this.poseDetectionCapability.PoseDetected += poseDetectionCapability_PoseDetected;
            //
            this.handsGenerator = new HandsGenerator(this.context);
            this.handsGenerator.HandCreate += handsGenerator_HandCreate;
            this.handsGenerator.HandDestroy += handsGenerator_HandDestroy;
            this.handsGenerator.HandUpdate += handsGenerator_HandUpdate;
            //
            this.gestureGenerator = new GestureGenerator(this.context);
            this.gestureGenerator.AddGesture("Wave");
            this.gestureGenerator.AddGesture("Click");
            this.gestureGenerator.AddGesture("RaiseHand");
            this.gestureGenerator.GestureRecognized += gestureGenerator_GestureRecognized;
            //
            this.joints = new Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>>();
            //
            this.userGenerator.StartGenerating();
            this.handsGenerator.StartGenerating();
            this.gestureGenerator.StartGenerating();
            //
            this.histogram = new int[this.depth.DeviceMaxDepth];
            MapOutputMode mapMode = this.depth.MapOutputMode;
            //
            this.bitmap = new Bitmap((int)mapMode.XRes, (int)mapMode.YRes);
            this.shouldRun = true;
            this.readerThread = new Thread(ReaderThread);
            this.readerThread.Priority = ThreadPriority.Lowest;
            this.readerThread.Start();
            //
            return true;
        }



        void gestureGenerator_GestureRecognized(object sender, GestureRecognizedEventArgs e)
        {
            NiteKinectHandGestureEventData eventdata;
            switch (e.Gesture.ToLower())
            {
                case "wave":
                    handsGenerator.StartTracking(e.IdentifiedPosition);
                    break;
                case "click":
                    eventdata = new NiteKinectHandGestureEventData(new NiteKinectHandGestureData(0, NiteKinectHandState.GestureClick, this.depth.ConvertRealWorldToProjective(e.EndPosition)));
                    if (DataReceived != null) DataReceived(this, new DataReceivedEventHandler() { PluginData = eventdata });
                    break;
                case "raisehand":
                    eventdata = new NiteKinectHandGestureEventData(new NiteKinectHandGestureData(0, NiteKinectHandState.GestureRaiseHand, this.depth.ConvertRealWorldToProjective(e.EndPosition)));
                    if (DataReceived != null) DataReceived(this, new DataReceivedEventHandler() { PluginData = eventdata });
                    break;
                default:
                    break;
            }
        }

        void handsGenerator_HandUpdate(object sender, HandUpdateEventArgs e)
        {
            NiteKinectHandGestureEventData eventdata = new NiteKinectHandGestureEventData(new NiteKinectHandGestureData(e.UserID, NiteKinectHandState.Tracking, this.depth.ConvertRealWorldToProjective(e.Position)));
            if (DataReceived != null) DataReceived(this, new DataReceivedEventHandler() { PluginData = eventdata });
        }

        void handsGenerator_HandDestroy(object sender, HandDestroyEventArgs e)
        {
            NiteKinectHandGestureEventData eventdata = new NiteKinectHandGestureEventData(new NiteKinectHandGestureData(e.UserID, NiteKinectHandState.Destroyed, new Point3D()));
            if (DataReceived != null) DataReceived(this, new DataReceivedEventHandler() { PluginData = eventdata });
        }

        void handsGenerator_HandCreate(object sender, HandCreateEventArgs e)
        {
            NiteKinectHandGestureEventData eventdata = new NiteKinectHandGestureEventData(new NiteKinectHandGestureData(e.UserID, NiteKinectHandState.Created, this.depth.ConvertRealWorldToProjective(e.Position)));
            if (DataReceived != null) DataReceived(this, new DataReceivedEventHandler() { PluginData = eventdata });
        }


        void skeletonCapbility_CalibrationEnd(object sender, CalibrationEndEventArgs e)
        {
            if (e.Success)
            {
                this.skeletonCapbility.StartTracking(e.ID);
                this.joints.Add(e.ID, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
            }
            else
            {
                this.poseDetectionCapability.StartPoseDetection(calibPose, e.ID);
            }
        }

        void poseDetectionCapability_PoseDetected(object sender, PoseDetectedEventArgs e)
        {
            this.poseDetectionCapability.StopPoseDetection(e.ID);
            this.skeletonCapbility.RequestCalibration(e.ID, true);
        }

        void userGenerator_NewUser(object sender, NewUserEventArgs e)
        {
            NiteKinectUserEventData eventdata = new NiteKinectUserEventData(new NiteKinectUserData(e.ID, NiteKinectUserState.Entered, new Point3D()));
            if (DataReceived != null) DataReceived(this, new DataReceivedEventHandler() { PluginData = eventdata });
            //
            this.poseDetectionCapability.StartPoseDetection(this.calibPose, e.ID);
        }

        void userGenerator_LostUser(object sender, UserLostEventArgs e)
        {
            NiteKinectUserEventData eventdata = new NiteKinectUserEventData(new NiteKinectUserData(e.ID, NiteKinectUserState.Exited, new Point3D()));
            if (DataReceived != null) DataReceived(this, new DataReceivedEventHandler() { PluginData = eventdata });
            //
            this.joints.Remove(e.ID);
        }



        private void updatejoint(int user, SkeletonJoint joint)
        {
            SkeletonJointPosition pos = this.skeletonCapbility.GetSkeletonJointPosition(user, joint);
            if (pos.Position.Z == 0)
            {
                pos.Confidence = 0;
            }
            else
            {
                pos.Position = this.depth.ConvertRealWorldToProjective(pos.Position);
            }
            this.joints[user][joint] = pos;
        }

        private void updatejoints(int user)
        {
            updatejoint(user, SkeletonJoint.Head);
            updatejoint(user, SkeletonJoint.Neck);

            updatejoint(user, SkeletonJoint.LeftShoulder);
            updatejoint(user, SkeletonJoint.LeftElbow);
            updatejoint(user, SkeletonJoint.LeftHand);

            updatejoint(user, SkeletonJoint.RightShoulder);
            updatejoint(user, SkeletonJoint.RightElbow);
            updatejoint(user, SkeletonJoint.RightHand);

            updatejoint(user, SkeletonJoint.Torso);

            updatejoint(user, SkeletonJoint.LeftHip);
            updatejoint(user, SkeletonJoint.LeftKnee);
            updatejoint(user, SkeletonJoint.LeftFoot);

            updatejoint(user, SkeletonJoint.RightHip);
            updatejoint(user, SkeletonJoint.RightKnee);
            updatejoint(user, SkeletonJoint.RightFoot);
        }

        private void ReaderThread()
        {

            while (this.shouldRun)
            {
                try
                {
                    this.context.WaitOneUpdateAll(this.depth);
                }
                catch (Exception)
                {
                }

                //lock (this)
                try
                {
                    int[] users = this.userGenerator.GetUsers();
                    foreach (int user in users)
                    {
                        Point3D com = this.userGenerator.GetCoM(user);
                        com = this.depth.ConvertRealWorldToProjective(com);
                        //
                        NiteKinectUserState state = NiteKinectUserState.Unknown;
                        if (this.skeletonCapbility.IsTracking(user))
                        {
                            state = NiteKinectUserState.Tracking;
                            //
                            updatejoints(user);
                            //
                            NiteKinectUserEventData eventdata = new NiteKinectUserEventData(new NiteKinectUserData(user, state, com) { UserSkelton = joints[user] });
                            if (DataReceived != null) DataReceived(this, new DataReceivedEventHandler() { PluginData = eventdata });
                        }
                        else
                        {
                            if (this.skeletonCapbility.IsCalibrating(user))
                            {
                                state = NiteKinectUserState.Calibrating;
                            }
                            else
                            {
                                state = NiteKinectUserState.LookingForPose;
                            }
                            //
                            NiteKinectUserEventData eventdata = new NiteKinectUserEventData(new NiteKinectUserData(user, state, com));
                            if (DataReceived != null) DataReceived(this, new DataReceivedEventHandler() { PluginData = eventdata });
                        }
                    }
                }
                catch { }
            }
        }



    }
}
