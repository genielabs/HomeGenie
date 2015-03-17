using System;

namespace ZWaveLib
{

    public enum FunctionType: byte
    {
        None = 0x00,
        DiscoveryNodes = 0x02,
        ApplicationCommand = 0x04,
        ControllerSoftReset = 0x08,
        SendData = 0x13,
        GetNodeProtocolInfo = 0x41,
        ControllerSetDefault = 0x42,
        // hard reset
        NodeUpdateInfo = 0x49,
        NodeAdd = 0x4A,
        NodeRemove = 0x4B,
        RequestNodeInfo = 0x60
    }

    public enum NodeFunctionOption : byte
    {
        AddNodeAny = 0x01,
        AddNodeController = 0x02,
        AddNodeSlave = 0x03,
        AddNodeExisting = 0x04,
        AddNodeStop = 0x05,
        //
        RemoveNodeAny = 0x01,
        RemoveNodeController = 0x02,
        RemoveNodeSlave = 0x03,
        RemoveNodeStop = 0x05
    }

    public enum NodeFunctionStatus : byte
    {
        AddNodeLearnReady = 0x01,
        AddNodeNodeFound = 0x02,
        AddNodeAddingSlave = 0x03,
        AddNodeAddingController = 0x04,
        AddNodeProtocolDone = 0x05,
        AddNodeDone = 0x06,
        AddNodeFailed = 0x07,
        //
        RemoveNodeLearnReady = 0x01,
        RemoveNodeNodeFound = 0x02,
        RemoveNodeRemovingSlave = 0x03,
        RemoveNodeRemovingController = 0x04,
        RemoveNodeDone = 0x06,
        RemoveNodeFailed = 0x07
    }

}

