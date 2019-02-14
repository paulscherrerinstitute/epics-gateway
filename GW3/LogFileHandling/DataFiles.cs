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
            List<DataFile> fileList;
            lock (dataFiles)
                fileList = dataFiles.Values.ToList();

            foreach (var i in fileList)
            {
                /*using (var l = i.Lock())
                {*/
                    i.Flush();
                //}
            }
        }

        public void CleanOlderThan(int nbDays)
        {
            List<DataFile> fileList;
            lock (dataFiles)
                fileList = dataFiles.Values.ToList();
            foreach (var i in fileList)
            {
                /*using (var l = i.Lock())
                {*/
                    i.CleanOlderThan(nbDays);
                //}
            }
        }

        public void SaveStats()
        {
            List<DataFile> fileList;
            lock (dataFiles)
                fileList = dataFiles.Values.ToList();
            foreach (var i in fileList)
            {
                /*using (var l = i.Lock())
                {*/
                    i.SaveStats();
                //}
            }
        }

        public void Dispose()
        {
            List<DataFile> fileList;
            lock (dataFiles)
            {
                fileList = dataFiles.Values.ToList();
                dataFiles.Clear();
            }
            foreach (var i in fileList)
                i.Dispose();
        }

        public bool Exists(string gatewayName)
        {
            return DataFile.Exists(Context.StorageDirectory, gatewayName);
        }

        public IEnumerator<DataFile> GetEnumerator()
        {
            List<DataFile> tempDataFiles = null;
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