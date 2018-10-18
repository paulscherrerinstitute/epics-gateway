using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GWLogger.Backend.DataContext
{
    internal class DataFiles : IDisposable, IEnumerable<DataFile>
    {
        private Dictionary<string, DataFile> dataFiles = new Dictionary<string, DataFile>();
        private readonly Context Context;

        public DataFiles(Context context)
        {
            this.Context = context;
        }

        public DataFile this[string gateway]
        {
            get
            {
                lock (dataFiles)
                {
                    if (!dataFiles.ContainsKey(gateway.ToLower()))
                        dataFiles.Add(gateway.ToLower(), new DataFile(Context, gateway.ToLower()));
                    return dataFiles[gateway.ToLower()];
                }
            }
        }

        internal void Flush()
        {
            lock (dataFiles)
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

        public void SaveStats()
        {
            lock (dataFiles)
            {
                foreach (var i in dataFiles)
                    i.Value.SaveStats(true);
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

        public bool Exists(string gatewayName)
        {
            return DataFile.Exists(gatewayName);
        }

        public IEnumerator<DataFile> GetEnumerator()
        {
            List<DataFile> tempDataFiles=null;
            lock (dataFiles)
            {
                tempDataFiles = dataFiles.Values.ToList();
            }
            return tempDataFiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}