using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GatewayLogic;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GwUnitTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestSplitter
    {
        [TestMethod]
        [Timeout(1000)]
        public void CheckCompleteMessageSplitting()
        {
            var buffer = new MemoryStream();

            var p = DataPacket.Create(0);
            p.Command = 1;
            buffer.Write(p.Data, 0, (int)p.MessageSize);

            using (var splitter = new Splitter())
            {

                var fullPacket = DataPacket.Create(buffer.ToArray(), (int)buffer.Length);
                var toFind = new Queue<int>(new int[] { 1 });

                foreach (var i in splitter.Split(fullPacket))
                {
                    var f = toFind.Dequeue();
                    Assert.AreEqual(f, i.Command);
                }
            Assert.AreEqual(0, toFind.Count);
            }
        }

        [TestMethod]
        [Timeout(1000)]

        public void CheckSimpleSplitting()
        {
            var buffer = new MemoryStream();

            var p = DataPacket.Create(0);
            p.Command = 1;
            buffer.Write(p.Data, 0, (int)p.MessageSize);

            p = DataPacket.Create(0);
            p.Command = 2;
            buffer.Write(p.Data, 0, (int)p.MessageSize);

            p = DataPacket.Create(64);
            p.Command = 3;
            p.SetDataAsString("Hello");
            buffer.Write(p.Data, 0, (int)p.MessageSize);

            p = DataPacket.Create(1);
            p.Command = 4;
            buffer.Write(p.Data, 0, (int)p.MessageSize);

            using (var splitter = new Splitter())
            {

                var fullPacket = DataPacket.Create(buffer.ToArray(), (int)buffer.Length);
                var toFind = new Queue<int>(new int[] { 1, 2, 3, 4 });

                foreach (var i in splitter.Split(fullPacket))
                {
                    var f = toFind.Dequeue();
                    Assert.AreEqual(f, i.Command);
                }

                Assert.AreEqual(0, toFind.Count);
            }
        }

        [TestMethod]
        //[Timeout(1000)]

        public void CheckLongSplitting()
        {
            var buffer = new MemoryStream();

            var p = DataPacket.Create(64096);
            p.Command = 1;
            buffer.Write(p.Data, 0, 1024);

            using (var splitter = new Splitter())
            {

                var fullPacket = DataPacket.Create(buffer.ToArray(), (int)buffer.Length);
                var toFind = new Queue<int>(new int[] { 1, 2, 3, 4 });

                foreach (var i in splitter.Split(fullPacket))
                {
                    var f = toFind.Dequeue();
                    Assert.AreEqual(f, i.Command);
                }

                buffer = new MemoryStream();
                buffer.Write(p.Data, 1024, (int)p.MessageSize - 1024);

                p = DataPacket.Create(0);
                p.Command = 2;
                buffer.Write(p.Data, 0, (int)p.MessageSize);

                p = DataPacket.Create(64);
                p.Command = 3;
                p.SetDataAsString("Hello");
                buffer.Write(p.Data, 0, (int)p.MessageSize);

                p = DataPacket.Create(1);
                p.Command = 4;
                buffer.Write(p.Data, 0, (int)p.MessageSize);

                fullPacket = DataPacket.Create(buffer.ToArray(), (int)buffer.Length);
                foreach (var i in splitter.Split(fullPacket))
                {
                    var f = toFind.Dequeue();
                    Assert.AreEqual(f, i.Command);
                }

                Assert.AreEqual(0, toFind.Count);
            }
        }

        [TestMethod]
        [Timeout(1000)]
        //[Timeout(100)]
        public void CheckSplittingLeftOver()
        {
            var buffer = new MemoryStream();

            var p = DataPacket.Create(0);
            p.Command = 1;
            buffer.Write(p.Data, 0, (int)p.MessageSize);

            p = DataPacket.Create(0);
            p.Command = 2;
            buffer.Write(p.Data, 0, (int)p.MessageSize);

            p = DataPacket.Create(64);
            p.Command = 3;
            p.SetDataAsString("Hello");
            buffer.Write(p.Data, 0, (int)p.MessageSize);

            p = DataPacket.Create(1);
            p.Command = 4;
            buffer.Write(p.Data, 0, (int)p.MessageSize);

            using (var splitter = new Splitter())
            {

                var byteBuffer = buffer.ToArray();

                var toFind = new Queue<int>(new int[] { 1, 2, 3, 4 });

                foreach (var i in splitter.Split(DataPacket.Create(byteBuffer, 41)))
                {
                    var f = toFind.Dequeue();
                    Assert.AreEqual(f, i.Command);
                }

                var missingPiece = new byte[byteBuffer.Length - 41];
                Array.Copy(byteBuffer, 41, missingPiece, 0, byteBuffer.Length - 41);
                var missingPacket = DataPacket.Create(missingPiece, missingPiece.Length);

                foreach (var i in splitter.Split(missingPacket))
                {
                    var f = toFind.Dequeue();
                    Assert.AreEqual(f, i.Command);
                }

                Assert.AreEqual(0, toFind.Count);
            }
        }
    }
}
