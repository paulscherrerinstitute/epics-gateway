using System;
using System.Collections.Generic;
using System.Linq;

namespace GWLogger.Backend.Controllers
{
    internal class GatewaySessions
    {
        Dictionary<string, OpenSession> dictionary;

        public GatewaySessions()
        {
            using (var ctx = new Model.LoggerContext())
            {
                var lastSessions = ctx.GatewaySessions.GroupBy(key => key.Gateway)
                    .Select(row => new { Gateway = row.Key, LastSession = row.Max(r2 => r2.StartDate) });

                dictionary = ctx.GatewaySessions
                    .Join(lastSessions, s => new { Gateway = s.Gateway, LastSession = s.StartDate }, l => l, (s, l) => s)
                    .Select(row => new OpenSession
                    {
                        Gateway = row.Gateway,
                        Start = row.StartDate,
                        End = row.LastEntry,
                        NbEntries = row.NbEntries
                    }).ToDictionary(key => key.Gateway, val => val);
            }
        }

        public OpenSession this[string key]
        {
            get
            {
                if (!dictionary.ContainsKey(key))
                    dictionary.Add(key, new OpenSession { Gateway = key });
                return dictionary[key];
            }
        }

        public List<OpenSession> GetAndReset()
        {
            var result = new List<OpenSession>();
            foreach (var i in dictionary)
            {
                if (!i.Value.Changed)
                    continue;

                if (i.Value.History.Count > 0)
                {
                    result.AddRange(i.Value.History);
                    i.Value.History.Clear();
                }
                result.Add(i.Value.Clone());

                i.Value.Changed = false;
            }
            return result;
        }

        public class OpenSession
        {
            public string Gateway { get; set; }
            public DateTime Start { get; set; } = DateTime.UtcNow.TrimToSeconds();
            public bool ChangedSinceLoaded { get; private set; } = false;
            public DateTime End { get; set; } = DateTime.UtcNow.TrimToSeconds();
            public long NbEntries { get; set; } = 0;
            public bool Changed { get; set; } = true;

            public List<OpenSession> History { get; private set; } = new List<OpenSession>();

            public void Restart()
            {
                if (this.NbEntries == 0)
                    return;
                if (ChangedSinceLoaded)
                    History.Add(this.Clone());
                Start = DateTime.UtcNow.TrimToSeconds();
                End = DateTime.UtcNow.TrimToSeconds();
                NbEntries = 0;
                Changed = true;
                ChangedSinceLoaded = true;
            }

            public void Log()
            {
                ChangedSinceLoaded = true;
                End = DateTime.UtcNow.TrimToSeconds();
                NbEntries++;
                Changed = true;
            }

            public OpenSession Clone()
            {
                return new OpenSession { Gateway = this.Gateway, Start = this.Start, End = this.End, NbEntries = NbEntries };
            }
        }
    }
}