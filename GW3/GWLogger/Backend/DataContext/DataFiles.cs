using System;
using System.Collections.Generic;

namespace GWLogger.Backend.DataContext
{
    internal class DataFiles : IDisposable
    {
        Dictionary<string, DataFile> dataFiles = new Dictionary<string, DataFile>();

        public DataFile this[string gateway]
        {
            get
            {
                lock (dataFiles)
                {
                    if (!dataFiles.ContainsKey(gateway))
                        dataFiles.Add(gateway, new DataFile(gateway));
                    return dataFiles[gateway];
                }
            }
        }

        internal void Flush()
        {
            lock(dataFiles)
            {
                foreach (var i in dataFiles)
                    i.Value.Flush();
            }
        }

        public void CleanOlderThan(int nbDays)
        {
            lock (dataFiles)
            {
                foreach (var i in dataFiles)
                    i.Value.CleanOlderThan(nbDays);
            }
        }

        public void Dispose()
        {
            lock (dataFiles)
            {
                foreach (var i in dataFiles)
                    i.Value.Dispose();
                dataFiles.Clear();
            }
        }
    }
}