namespace ZWaveLib
{
    internal interface ICommandClass
    {
        /// <summary>
        /// Returns Id of Command class
        /// </summary>
        /// <returns>command class Id</returns>
        byte GetCommandClassId();

        /// <summary>Processes the message and returns corresponding ZWaveEvent</summary>
        /// <param name="node">the Node triggered the command</param>
        /// <param name="message">command part of ZWave message (without headers and checksum)</param>
        /// <returns></returns>
        ZWaveEvent GetEvent(ZWaveNode node, byte[] message);
    }
}
