﻿using EpicsSharp.ChannelAccess.Client;
using GWWatchdog.Caesar;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Threading;

namespace GWWatchdog
{
    internal enum GWStatus
    {
        STARTING,
        ALL_OK,
        HIGH_CPU,
        NOT_ANSWERING,
        RESTARTING
    }

    public partial class WatchdogService : ServiceBase
    {
        private Thread checkGateway;
        private bool shouldStop = false;
        private const int nbCPUAvg = 120;
        private TcpListener tcpListener = null;
        private GWStatus status = GWStatus.STARTING;
        private double currentAVG = 0;
        private List<double> lastCPUVals = new List<double>();

        private readonly DataAccessSoapClient caesar;
        private readonly string gatewayName;

        public WatchdogService()
        {
            InitializeComponent();
            try
            {
                caesar = new Caesar.DataAccessSoapClient(new System.ServiceModel.BasicHttpBinding(), new System.ServiceModel.EndpointAddress("http://caesar.psi.ch/DataAccess.asmx"));
            }
            catch(Exception ex)
            {
                if (Environment.UserInteractive)
                    Console.WriteLine(ex);
            }
            gatewayName = ConfigurationManager.AppSettings["gatewayName"];
        }

        public void Start()
        {
            checkGateway = new Thread(CheckGateway);
            checkGateway.Start();

            if (ConfigurationManager.AppSettings["WebInterface"] != null)
            {
                string s = ConfigurationManager.AppSettings["WebInterface"];
                IPEndPoint ipSource = new IPEndPoint(IPAddress.Parse(s.Split(':')[0]), int.Parse(s.Split(':')[1]));
                if (Environment.UserInteractive)
                    Console.WriteLine("Start receiving HTTP on " + ipSource);
                tcpListener = new TcpListener(ipSource);
                tcpListener.Start(10);
                tcpListener.BeginAcceptSocket(ReceiveConn, tcpListener);
            }
        }

        private void ReceiveConn(IAsyncResult result)
        {
            TcpListener listener = null;
            Socket client = null;

            try
            {
                listener = (TcpListener)result.AsyncState;
                client = listener.EndAcceptSocket(result);

                client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                //Thread.Sleep(100);
                using (var reader = new StreamReader(new NetworkStream(client)))
                {
                    var cmd = reader.ReadLine();
                    using (var writer = new StreamWriter(new NetworkStream(client)))
                    {
                        Console.WriteLine(cmd);

                        if (cmd.StartsWith("POST /restart"))
                        {
                            RestartGW();
                        }

                        writer.WriteLine("HTTP/1.0 OK");
                        writer.WriteLine("Content-Type: text/html");
                        writer.WriteLine("Expires: now");
                        writer.WriteLine();
                        writer.WriteLine("<html>");
                        writer.WriteLine("<head><title>Gateway Watchdog - " + gatewayName + "</title></head>");
                        writer.WriteLine("<body>");
                        writer.WriteLine("Status: " + status + "<br>");
                        writer.WriteLine("CPU Average: " + currentAVG.ToString("0.00") + "%<br>");
                        writer.WriteLine("<form method='post' action='/restart'>");
                        writer.WriteLine("<input type='submit' value='Restart Gateway' onclick='return confirm(\"Are you sure you want to restart the gateway?\");'>");
                        writer.WriteLine("</form>");
                        writer.WriteLine("Updated on " + DateTime.Now + "<br>");
                        writer.WriteLine("<script>setTimeout(\"document.location='/';\",1000);</script>");
                        writer.WriteLine("</body>");
                        writer.WriteLine("</html>");
                        writer.Close();
                    }

                    reader.Close();
                }
                client.Close();
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                    Console.WriteLine(ex);
            }


            try
            {
                listener.BeginAcceptSocket(new AsyncCallback(ReceiveConn), listener);
            }
            catch
            {
            }
        }

        protected override void OnStart(string[] args)
        {
            this.Start();
        }

        protected override void OnStop()
        {
            shouldStop = true;

            if (ConfigurationManager.AppSettings["WebInterface"] != null)
            {
            }
        }

        private void CheckGateway()
        {
            if (!Environment.UserInteractive)
                Thread.Sleep(40000);

            NameValueCollection additionalChannels = (NameValueCollection)ConfigurationManager.GetSection("AdditionalChannels");

            while (!shouldStop)
            {
                try
                {
                    if (Environment.UserInteractive)
                        Console.WriteLine("Checking...");
                    bool isOk = false;
                    for (int i = 0; i < 30; i++)
                    {
                        if (Environment.UserInteractive)
                            Console.WriteLine("Trial " + i);
                        isOk = false;
                        using (var client = new CAClient())
                        {
                            client.Configuration.WaitTimeout = 3000;
                            var cpuInfo = client.CreateChannel<double>(gatewayName + ":CPU");
                            try
                            {
                                double v = cpuInfo.Get();
                                lastCPUVals.Add(v);
                                while (lastCPUVals.Count > nbCPUAvg)
                                    lastCPUVals.RemoveAt(0);

                                currentAVG = lastCPUVals.Average();
                                if (lastCPUVals.Count < nbCPUAvg * 0.8 || currentAVG < 75.0)
                                {
                                    isOk = true;
                                    status = GWStatus.ALL_OK;
                                }
                                else
                                    status = GWStatus.HIGH_CPU;
                                //isOk = true;
                            }
                            catch
                            {
                                status = GWStatus.NOT_ANSWERING;
                            }
                        }

                        if (isOk && additionalChannels != null)
                        {
                            foreach (string gw in additionalChannels.AllKeys)
                            {
                                using (CAClient client = new CAClient())
                                {
                                    client.Configuration.SearchAddress = gw;
                                    client.Configuration.WaitTimeout = 2000;
                                    var channel = client.CreateChannel<string>(additionalChannels[gw]);
                                    try
                                    {
                                        string s = channel.Get();
                                        if (Environment.UserInteractive)
                                            Console.WriteLine("Read " + s);
                                        isOk = true;
                                        status = GWStatus.ALL_OK;
                                    }
                                    catch
                                    {
                                        isOk = false;
                                        status = GWStatus.NOT_ANSWERING;
                                    }
                                }
                            }
                        }

                        if (isOk == true)
                            break;

                        Thread.Sleep(1000);
                    }

                    if (!isOk)
                    {
                        if (Environment.UserInteractive)
                            Console.WriteLine("Not ok!!!");
                        RestartGW();
                        Thread.Sleep(40000);
                    }
                    else
                    {
                        if (Environment.UserInteractive)
                            Console.WriteLine("All ok");
                    }
                }
                catch (Exception ex)
                {
                    if (Environment.UserInteractive)
                        Console.WriteLine(ex.ToString());
                }
                Thread.Sleep(10000);
            }
        }

        private void RestartGW()
        {
            try
            {
                if (Environment.UserInteractive)
                    Console.WriteLine("Restart due: " + status.ToString());
                switch (status)
                {
                    case GWStatus.STARTING:
                        break;
                    case GWStatus.ALL_OK:
                        break;
                    case GWStatus.HIGH_CPU:
                        caesar.UpdateLastGatewaySessionInformation(gatewayName, RestartType.WatchdogCPULimit, "Automatic restart due to CPU load.");
                        break;
                    case GWStatus.NOT_ANSWERING:
                        caesar.UpdateLastGatewaySessionInformation(gatewayName, RestartType.WatchdogNoResponse, "Automatic restart due to missing answers.");
                        break;
                    case GWStatus.RESTARTING:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                    Console.WriteLine(ex);
            }

            status = GWStatus.RESTARTING;
            StopGateway();
            lastCPUVals.Clear();
            StartGateway();
        }

        private void StopGateway()
        {
            try
            {
                ServiceController service = new ServiceController(ConfigurationManager.AppSettings["serviceName"]);
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(5000));
            }
            catch
            {
            }

            Thread.Sleep(2000);

            // Kill the remaining processes
            try
            {
                var processes = Process.GetProcesses()
                    .Where(row => row.ProcessName.ToLower() == "gwservice" || row.ProcessName.ToLower() == "epics gateway");
                foreach (var i in processes)
                    i.Kill();
            }
            catch
            {
            }
        }

        private void StartGateway()
        {
            try
            {
                if (Environment.UserInteractive)
                    Console.WriteLine("Starting gw");
                ServiceController service = new ServiceController(ConfigurationManager.AppSettings["serviceName"]);
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(5000));
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                    Console.WriteLine(ex.ToString());
            }
        }
    }
}
