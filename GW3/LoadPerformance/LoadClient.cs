using EpicsSharp.ChannelAccess.Client;
using System;
using System.Linq;
using System.Threading;

namespace LoadPerformance
{
    internal class LoadClient : IDisposable
    {
        private CAClient client;
        private Channel<int[]>[] records = new Channel<int[]>[Program.NbArrays];
        private long totData = 0;
        public DateTime startTime = DateTime.UtcNow;

        public LoadClient(string search, int offset, int nb, int portOffset)
        {
            Console.WriteLine("Starting client at " + (5064 + portOffset) + ", offset: " + offset + ", " + nb);
            client = new CAClient(5064 + portOffset);
            client.Configuration.SearchAddress = search;
            for (var i = 0; i < nb; i++)
            {
                records[i + offset] = client.CreateChannel<int[]>("MY-PERF-" + (i + offset) + ":ARR");
                records[i + offset].MonitorChanged += LoadClient_MonitorChanged;
                records[i + offset].StatusChanged += (chan, state) =>
                  {
                      if(state == EpicsSharp.ChannelAccess.Constants.ChannelStatus.DISCONNECTED)
                          Console.WriteLine(state);
                  };
            }
        }

        private void LoadClient_MonitorChanged(Channel<int[]> sender, int[] newValue)
        {
            Interlocked.Add(ref totData, newValue.Length * 4);
        }

        public long NbBytesSec
        {
            get
            {
                return (long)(totData / (DateTime.UtcNow - startTime).TotalSeconds);
            }
        }

        public int Connected => records.Count(row => row != null && row.Status == EpicsSharp.ChannelAccess.Constants.ChannelStatus.CONNECTED);

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
