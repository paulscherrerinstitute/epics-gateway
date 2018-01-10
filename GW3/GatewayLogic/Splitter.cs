using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic
{
    class Splitter
    {
        DataPacket remainingPacket = null;
        uint dataMissing = 0;
        int currentPos = 0;
        SemaphoreSlim lockSplitter = new SemaphoreSlim(1);

        public IEnumerable<DataPacket> Split(DataPacket packet)
        {
            lockSplitter.Wait();
            while (packet.BufferSize != 0)
            {
                // We had an incomplete packet, let's try to add the missing piece now
                if (dataMissing != 0)
                {
                    //Log.Write("Data missing...");
                    // The new packet is smaller than the missing piece
                    // Therefore send the whole as "BODY" and quit the splitter
                    if (packet.BufferSize < dataMissing)
                    {
                        dataMissing -= (uint)packet.BufferSize;
                        packet.Kind = DataPacketKind.BODY;

                        yield return packet;
                        lockSplitter.Release();
                        yield break;
                    }
                    // The new packet is bigger or equal than the missing piece
                    DataPacket p = DataPacket.Create(packet, dataMissing);
                    p.Kind = DataPacketKind.TAIL;
                    yield return p;
                    DataPacket newPacket = packet.SkipSize(dataMissing);
                    //packet.Dispose();
                    packet = newPacket;
                    dataMissing = 0;
                    continue;
                }

                // We had some left over, join with the current packet
                if (remainingPacket != null)
                {
                    //Log.Write("Joining left over...");
                    if (currentPos != 0)
                    {
                        // With the new block we have more than enough
                        if (packet.BufferSize + currentPos >= remainingPacket.BufferSize)
                        {
                            int s = remainingPacket.BufferSize - currentPos;
                            Buffer.BlockCopy(packet.Data, 0, remainingPacket.Data, currentPos, s);
                            remainingPacket.Kind = DataPacketKind.COMPLETE;
                            yield return remainingPacket;

                            remainingPacket = null;
                            currentPos = 0;

                            // Got all.
                            if (s == packet.BufferSize)
                            {
                                //packet.Dispose();
                                lockSplitter.Release();
                                yield break;
                            }

                            remainingPacket = packet.SkipSize((uint)s, true);
                            //packet.Dispose();
                            packet = remainingPacket;
                            remainingPacket = null;
                            continue;
                        }
                        // Just add the missing piece
                        else
                        {
                            Buffer.BlockCopy(packet.Data, 0, remainingPacket.Data, currentPos, packet.BufferSize);
                            currentPos += packet.BufferSize;
                            lockSplitter.Release();
                            yield break;
                        }
                    }
                    else
                    {
                        packet = DataPacket.Create(remainingPacket, packet);
                        remainingPacket = null;
                    }
                }

                // We don't even have a complete header, stop
                if (!packet.HasCompleteHeader)
                {
                    //Log.Write("Incomplete packet...");
                    //remainingPacket = packet;
                    remainingPacket = DataPacket.Create(packet, (uint)packet.BufferSize, false);
                    lockSplitter.Release();
                    yield break;
                }
                // Full packet, send it.
                if (packet.MessageSize == packet.BufferSize)
                {
                    //Log.Write("Complete packet...");
                    packet.Kind = DataPacketKind.COMPLETE;
                    yield return packet;
                    /*DataPacket p = DataPacket.Create(packet, (uint)packet.BufferSize);
                    this.SendData(p);*/
                    lockSplitter.Release();
                    yield break;
                }

                // More than one message in the packet, split and continue
                if (packet.MessageSize < packet.BufferSize)
                {
                    //Log.Write("Splitting...");
                    DataPacket p = DataPacket.Create(packet, packet.MessageSize, false);
                    p.Kind = DataPacketKind.COMPLETE;
                    yield return p;
                    if (packet.Offset >= packet.BufferSize)
                    {
                        lockSplitter.Release();
                        yield break;
                    }
                    DataPacket newPacket = packet.SkipSize(packet.MessageSize, false);
                    //packet.Dispose();
                    packet = newPacket;
                }
                // Message bigger than packet.
                // Cannot be the case on UDP!
                else
                {
                    //Log.Write("Missing some...");

                    //remainingPacket = (DataPacket)packet.Clone();
                    if (packet.HasCompleteHeader)
                    {
                        //currentPos = packet.BufferSize;
                        //remainingPacket = DataPacket.Create(packet, packet.MessageSize);
                        currentPos = 0;
                        remainingPacket = (DataPacket)packet.Clone();
                    }
                    else
                    {
                        remainingPacket = (DataPacket)packet.Clone();
                    }
                    lockSplitter.Release();
                    yield break;
                }
            }
            lockSplitter.Release();
        }

        public void Reset()
        {
            remainingPacket = null;
            dataMissing = 0;
            currentPos = 0;
        }
    }
}
