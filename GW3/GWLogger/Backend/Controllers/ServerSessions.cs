using GWLogger.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GWLogger.Backend.Controllers
{
    static internal class ServerSessions
    {
        static internal void Disconnect()
        {
            using (var ctx = new LoggerContext())
            {
                foreach (var i in ctx.ConnectedServers.Where(row => row.EndConnection == null))
                {
                    i.EndConnection = DateTime.UtcNow;
                }
                ctx.SaveChanges();
            }
        }

        static internal void DisconnectAll(string gateway)
        {
            using (var ctx = new LoggerContext())
            {
                foreach (var i in ctx.ConnectedServers.Where(row => row.Gateway == gateway && row.EndConnection == null))
                {
                    i.EndConnection = DateTime.UtcNow;
                }
                ctx.SaveChanges();
            }
        }

        static internal void Disconnect(string gateway, string remoteIpPoint)
        {
            using (var ctx = new LoggerContext())
            {
                var conn = ctx.ConnectedServers.FirstOrDefault(row => row.Gateway == gateway && row.RemoteIpPoint == remoteIpPoint && row.EndConnection == null);
                if (conn == null)
                    return;
                conn.EndConnection = DateTime.UtcNow;
                ctx.SaveChanges();
            }
        }

        static internal void Connect(string gateway, string remoteIpPoint)
        {
            using (var ctx = new LoggerContext())
            {
                ctx.ConnectedServers.Add(new ConnectedServer { Gateway = gateway, RemoteIpPoint = remoteIpPoint, StartConnection = DateTime.UtcNow });
                ctx.SaveChanges();
            }
        }
    }
}