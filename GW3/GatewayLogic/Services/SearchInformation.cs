﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class SearchInformation : IDisposable
    {
        private uint nextId = 1;
        SafeLock dictionaryLock = new SafeLock();
        Dictionary<string, SearchInformationDetail> dictionary = new Dictionary<string, SearchInformationDetail>();

        ~SearchInformation()
        {
            dictionaryLock.Dispose();
        }

        public class SearchInformationDetail : IDisposable
        {
            public uint GatewayId { get; }
            SafeLock clientsLock = new SafeLock();
            public List<ClientId> clients = new List<ClientId>();
            public string Channel { get; internal set; }
            public IPEndPoint Server { get; internal set; }
            public DateTime LastSearch { get; internal set; }
            public ushort Version { get; internal set; }

            public SearchInformationDetail(uint id)
            {
                GatewayId = id;
            }

            ~SearchInformationDetail()
            {
                clientsLock.Dispose();
            }

            internal void AddClient(ClientId clientId)
            {
                using (clientsLock.Aquire())
                {
                    clients.RemoveAll(row => (DateTime.UtcNow - row.When).TotalSeconds > 1);
                    if (!clients.Any(row => row.Client == clientId.Client && row.Id == row.Id))
                        clients.Add(clientId);
                }
            }

            internal IEnumerable<ClientId> GetClients()
            {
                using (clientsLock.Aquire())
                {
                    clients.RemoveAll(row => (DateTime.UtcNow - row.When).TotalSeconds > 1);

                    var result = clients.ToList();
                    clients.Clear();
                    return result;
                }
            }

            public void Dispose()
            {
                //clientsLock.Dispose();
            }
        }

        internal SearchInformationDetail Get(string channelName)
        {
            using (dictionaryLock.Aquire())
            {
                if (!dictionary.ContainsKey(channelName))
                {
                    var result = new SearchInformationDetail(nextId++);
                    result.Channel = channelName;
                    dictionary.Add(channelName, result);
                }

                return dictionary[channelName];
            }
        }

        internal bool HasChannelServerInformation(string channelName)
        {
            using (dictionaryLock.Aquire())
            {
                return dictionary.ContainsKey(channelName) && dictionary[channelName].Server != null;
            }
        }

        internal SearchInformationDetail Get(uint gatewayId)
        {
            using (dictionaryLock.Aquire())
            {
                var result = dictionary.Values.FirstOrDefault(row => row.GatewayId == gatewayId);
                return result;
            }
        }

        public void Dispose()
        {
            using (dictionaryLock.Aquire())
            {
                foreach (var i in dictionary.Values)
                    i.Dispose();
                dictionary.Clear();
            }
            //dictionaryLock.Dispose();
        }

        public void Remove(string channelName)
        {
            using (dictionaryLock.Aquire())
            {
                if (dictionary.ContainsKey(channelName))
                {
                    dictionary[channelName].Dispose();
                    dictionary.Remove(channelName);
                }
            }
        }

        public void Cleanup()
        {
            using (dictionaryLock.Aquire())
            {
                foreach (var i in dictionary.Values)
                    i.Dispose();
                dictionary.Clear();
            }
        }
    }
}
