using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace GWLogger.Backend.DataContext
{
    class BinaryIndex<TType> : IDisposable
    {
        Dictionary<TType, BinaryIndexCluster> Clusters = new Dictionary<TType, BinaryIndexCluster>();
        string filename;
        FileStream stream;
        BinaryWriter writer;
        BinaryReader reader;
        const int indexClusterSize = 200;
        const int maxStringSize = 125;
        const int indexItemNumbers = 1024 * 4;
        const int batchSize = 80;

        public BinaryIndex(string filename)
        {
            this.filename = filename;
        }

        private void Init()
        {
            stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            writer = new BinaryWriter(stream, Encoding.UTF8, true);
            reader = new BinaryReader(stream, Encoding.UTF8, true);
        }

        public string Filename => filename;

        public void AddEntry(TType key, long position)
        {
            if (((key as string)?.Length ?? 0) > maxStringSize)
                throw new Exception("String is too long for the index");
            if (key == null)
                return;
            try
            {
                if (!Clusters.ContainsKey(key))
                    Clusters.Add(key, new BinaryIndexCluster(reader, writer, key));
                Clusters[key].AddEntry(position);

                foreach (var c in Clusters.Where(row => row.Value.BufferSize >= batchSize))
                    c.Value.FlushBuffer();
            }
            // Wrong index. Let's delete it, and create a new one.
            catch
            {
                writer?.Dispose();
                reader?.Dispose();
                stream?.Dispose();
                File.Delete(this.filename);
                Init();
            }
        }

        public IEnumerable<long> Elements(TType key)
        {
            if (!Clusters.ContainsKey(key))
                Clusters.Add(key, new BinaryIndexCluster(reader, writer, key));
            return Clusters[key].GetEntries();
        }

        public void Flush()
        {
            foreach (var c in Clusters)
                c.Value.FlushBuffer();

            writer?.Flush();
        }

        public void Dispose()
        {
            foreach (var c in Clusters)
                c.Value.FlushBuffer();

            reader?.Dispose();
            reader = null;
            writer?.Dispose();
            writer = null;
            stream?.Dispose();
            stream = null;
        }

        private class BinaryIndexCluster
        {
            private TType key;
            private BinaryReader reader;
            private BinaryWriter writer;
            private int keySize = 0;
            private long baseChunkPosition;
            private long currentChunkPosition;
            private short currentChunkNumberOfItems;
            private long nextCunkItemsPosition;
            List<long> positionBuffer = new List<long>();
            public DateTime LastBufferFlush { get; set; } = DateTime.UtcNow;

            public BinaryIndexCluster(BinaryReader reader, BinaryWriter writer, TType key)
            {
                this.key = key;
                this.reader = reader;
                this.writer = writer;
                writer.Flush();

                switch ((object)key)
                {
                    case string strVal:
                        this.keySize = maxStringSize + 1 + 8;
                        break;
                    case short sVal:
                        this.keySize = 2 + 8;
                        break;
                    case int iVal:
                        this.keySize = 4 + 8;
                        break;
                }

                // File is empty, let's fill it
                if (reader.BaseStream.Length == 0)
                {
                    writer.Write((short)0);
                    writer.Write((long)0);
                    writer.Write(new byte[keySize * indexClusterSize]);
                }

                bool found = false;
                bool isNew = false;
                long pos = 0;
                // Move to start of the index cluster
                while (!found)
                {
                    Seek(pos);
                    var nbKeys = reader.ReadUInt16();
                    var nextIndex = reader.ReadInt64();
                    for (var i = 0; i < indexClusterSize && i < nbKeys; i++)
                    {
                        Seek(pos + i * keySize + 2 + 8);
                        switch ((object)key)
                        {
                            case string strVal:
                                {
                                    var r = reader.ReadString();
                                    var p = reader.ReadInt64();
                                    if (r == strVal)
                                    {
                                        pos = p;
                                        found = true;
                                    }
                                    break;
                                }
                            case short sVal:
                                {
                                    var r = reader.ReadInt16();
                                    var p = reader.ReadInt64();
                                    if (r == sVal)
                                    {
                                        pos = p;
                                        found = true;
                                    }
                                    break;
                                }
                            case int iVal:
                                {
                                    var r = reader.ReadInt32();
                                    var p = reader.ReadInt64();
                                    if (r == iVal)
                                    {
                                        pos = p;
                                        found = true;
                                    }
                                    break;
                                }
                        }
                    }
                    if (found)
                        break;

                    if (nbKeys < indexClusterSize)
                    {
                        Seek(pos + nbKeys * keySize + 2 + 8);
                        switch ((object)key)
                        {
                            case string strVal:
                                {
                                    writer.Write(strVal);
                                    writer.Write(writer.BaseStream.Length);
                                    Seek(pos);
                                    writer.Write((short)(nbKeys + 1));
                                    pos = writer.BaseStream.Length;
                                    isNew = true;
                                    found = true;
                                    break;
                                }
                            case short sVal:
                                {
                                    writer.Write(sVal);
                                    writer.Write(writer.BaseStream.Length);
                                    Seek(pos);
                                    writer.Write((short)(nbKeys + 1));
                                    pos = writer.BaseStream.Length;
                                    found = true;
                                    isNew = true;
                                    break;
                                }
                            case int iVal:
                                {
                                    writer.Write(iVal);
                                    writer.Write(writer.BaseStream.Length);
                                    Seek(pos);
                                    writer.Write((short)(nbKeys + 1));
                                    pos = writer.BaseStream.Length;
                                    found = true;
                                    isNew = true;
                                    break;
                                }
                        }
                    }
                    else
                    {
                        // Writing the index of the next block of cluster key
                        Seek(pos + 2);
                        writer.Write((long)writer.BaseStream.Length);

                        // Adding the next block of cluster key
                        pos = reader.BaseStream.Length;
                        writer.Write((short)0);
                        writer.Write((long)0);
                        writer.Write(new byte[keySize * indexClusterSize]);
                    }
                }

                this.baseChunkPosition = pos;
                this.currentChunkPosition = pos;
                Seek(pos);

                // we don't have yet the block of items
                if (isNew)
                {
                    writer.Write((short)0);
                    writer.Write((long)0);
                    writer.Write(new byte[indexItemNumbers * 8]); // A time stamp and a file position (long)
                    this.nextCunkItemsPosition = 0;
                }
                else
                {
                    this.currentChunkNumberOfItems = reader.ReadInt16();
                    this.nextCunkItemsPosition = reader.ReadInt64();
                }

                while (this.nextCunkItemsPosition != 0)
                {
                    Seek(pos);
                    this.currentChunkPosition = pos;
                    this.currentChunkNumberOfItems = reader.ReadInt16();
                    this.nextCunkItemsPosition = reader.ReadInt64();
                    pos = this.nextCunkItemsPosition;
                }
            }

            internal void AddEntry(long position)
            {
                lock (positionBuffer)
                {
                    positionBuffer.Add(position);
                }
            }

            internal long BufferSize
            {
                get
                {
                    lock (positionBuffer)
                    {
                        return positionBuffer.Count;
                    }
                }
            }

            internal void FlushBuffer()
            {
                lock (positionBuffer)
                {
                    while (positionBuffer.Count > 0)
                    {
                        if (this.currentChunkNumberOfItems >= indexItemNumbers)
                        {
                            var next = writer.BaseStream.Length;
                            Seek(currentChunkPosition + 2);
                            writer.Write(next);

                            Seek(next);
                            writer.Write((short)0);
                            writer.Write((long)0);
                            writer.Write(new byte[indexItemNumbers * 8]); // A time stamp and a file position (long)

                            this.currentChunkPosition = next;
                            this.currentChunkNumberOfItems = 0;
                        }

                        var pos = positionBuffer.Take(indexItemNumbers - currentChunkNumberOfItems);
                        Seek(this.currentChunkPosition + 2 + 8 + 8 * this.currentChunkNumberOfItems);
                        foreach (var p in pos)
                        {
                            this.currentChunkNumberOfItems++;
                            writer.Write(p);
                        }
                        Seek(this.currentChunkPosition);
                        writer.Write(this.currentChunkNumberOfItems);
                        positionBuffer.RemoveRange(0, pos.Count());
                    }
                    LastBufferFlush = DateTime.UtcNow;
                }
            }

            void Seek(long position)
            {
                writer.BaseStream.Seek(position, SeekOrigin.Begin);
            }

            public IEnumerable<long> GetEntries()
            {
                var enumerator = new EntriesEnumerator(this.baseChunkPosition, this.reader);
                while (enumerator.HasEntries())
                    yield return enumerator.NextEntry();
            }

            private class EntriesEnumerator
            {
                private long chunkPosition;
                private BinaryReader reader;
                private short currentChunkNumberOfItems;
                private long nextCunkItemsPosition;
                private int currentItem;

                public EntriesEnumerator(long baseChunkPosition, BinaryReader reader)
                {
                    this.chunkPosition = baseChunkPosition;
                    this.reader = reader;
                    reader.BaseStream.Seek(baseChunkPosition, SeekOrigin.Begin);

                    this.currentChunkNumberOfItems = reader.ReadInt16();
                    this.nextCunkItemsPosition = reader.ReadInt64();
                    this.currentItem = 0;
                }

                internal bool HasEntries()
                {
                    if (this.currentItem >= this.currentChunkNumberOfItems)
                    {
                        if (this.nextCunkItemsPosition == 0)
                            return false;
                        this.chunkPosition = this.nextCunkItemsPosition;
                        reader.BaseStream.Seek(this.chunkPosition, SeekOrigin.Begin);
                        this.currentChunkNumberOfItems = reader.ReadInt16();
                        this.nextCunkItemsPosition = reader.ReadInt64();
                        this.currentItem = 0;

                        if (this.currentItem >= this.currentChunkNumberOfItems)
                            return false;
                    }
                    return true;
                }

                internal long NextEntry()
                {
                    if (!HasEntries())
                        throw new Exception("No more entries");
                    reader.BaseStream.Seek(this.chunkPosition + 2 + 8 + this.currentItem * 8, SeekOrigin.Begin);
                    this.currentItem++;
                    return reader.ReadInt64();
                }
            }
        }
    }
}