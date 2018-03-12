using System;
using System.Collections.Generic;

namespace GWLogger.Backend.Controllers
{
    internal class LogStat
    {
        Dictionary<string, LogDateStat> dictionary = new Dictionary<string, LogDateStat>();

        public LogDateStat this[string key]
        {
            get
            {
                if (!dictionary.ContainsKey(key))
                    dictionary.Add(key, new LogDateStat());
                return dictionary[key];
            }
        }

        public List<StatEntry> GetListAndClear()
        {
            var result = new List<StatEntry>();

            foreach(var i in dictionary)
                foreach(var j in i.Value.dictionary)
                    result.Add(new StatEntry { Gateway = i.Key, Date = j.Key, Value = j.Value });

            dictionary.Clear();
            return result;
        }

        public class LogDateStat
        {
            public Dictionary<DateTime, int> dictionary = new Dictionary<DateTime, int>();

            public int this[DateTime key]
            {
                get
                {
                    if (!dictionary.ContainsKey(key))
                        dictionary.Add(key, 0);
                    return dictionary[key];
                }

                set
                {
                    if (!dictionary.ContainsKey(key))
                        dictionary.Add(key, value);
                    else
                        dictionary[key] = value;
                }
            }
        }

        public class StatEntry
        {
            public string Gateway { get; set; }
            public DateTime Date { get; set; }
            public int Value { get; set; }
        }
    }
}