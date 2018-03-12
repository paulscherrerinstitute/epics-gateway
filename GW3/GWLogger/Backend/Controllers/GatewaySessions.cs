using System;
using System.Collections.Generic;

namespace GWLogger.Backend.Controllers
{
    internal class GatewaySessions
    {
        Dictionary<string, OpenSession> dictionary = new Dictionary<string, OpenSession>();

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
            public DateTime Start { get; private set; } = DateTime.UtcNow;
            public DateTime End { get; private set; } = DateTime.UtcNow;
            public long NbEntries { get; private set; } = 0;
            public bool Changed { get; set; } = true;

            public List<OpenSession> History { get; private set; } = new List<OpenSession>();

            public void Restart()
            {
                if (this.NbEntries == 0)
                    return;
                History.Add(this.Clone());
                Start = DateTime.UtcNow;
                End = DateTime.UtcNow;
                NbEntries = 0;
                Changed = true;
            }

            public void Log()
            {
                End = DateTime.UtcNow;
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