﻿using GWLoggerControlService.GWLoggerHealthServiceReference;
using Microsoft.Web.Administration;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.ServiceModel;

namespace GWLoggerControlService
{
    public partial class ControlService : ServiceBase
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan FirstTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(5);
        private readonly ManualResetEvent StopHealthCheckThread = new ManualResetEvent(false);
        private Thread HealthCheckThread;

        public ControlService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            using (var serverManager = new ServerManager())
            {
                foreach (var pool in serverManager.ApplicationPools)
                {
                    if (pool.State != ObjectState.Started)
                    {
                        try
                        {
                            pool.Start();
                        }
                        catch (Exception ex)
                        {
                            EventLog.WriteEntry($"Exception trying to start application pool '{pool.Name}': {ex}", EventLogEntryType.Error);
                        }
                    }
                }
            }

            HealthCheckThread = new Thread(PerformHealthChecks);
            HealthCheckThread.Start();
        }

        protected override void OnStop()
        {
            StopHealthCheckThread.Set();

            using (var serverManager = new ServerManager())
            {
                foreach (var pool in serverManager.ApplicationPools)
                {
                    if (pool.State != ObjectState.Stopped)
                    {
                        try
                        {
                            pool.Stop();
                        }
                        catch (Exception ex)
                        {
                            EventLog.WriteEntry($"Exception trying to stop application pool '{pool.Name}': {ex}", EventLogEntryType.Error);
                        }
                    }
                }
            }

            HealthCheckThread.Join();
            HealthCheckThread = null;
        }

        private void PerformHealthChecks()
        {
            var binding = new BasicHttpBinding
            {
                OpenTimeout = ConnectionTimeout,
                SendTimeout = ConnectionTimeout,
                CloseTimeout = ConnectionTimeout,
                ReceiveTimeout = ConnectionTimeout,
                Name = "Binding",
            };
            var endpointUrl = $"http://localhost/Health.asmx";
            var endpoint = new EndpointAddress(endpointUrl);
            EventLog.WriteEntry($"Using health check url: {endpointUrl}", EventLogEntryType.Information);

            var firstCheck = true;
            while (true)
            {
                if (StopHealthCheckThread.WaitOne(DefaultTimeout))
                    break;

                var isOk = false;
                var exString = "";
                for (var i = 0; i < 5; i++)
                {
                    try
                    {
                        using (var client = new HealthSoapClient(binding, endpoint))
                        {
                            client.Open();
                            client.InnerChannel.OperationTimeout = DefaultTimeout;
                            if (firstCheck)
                            {
                                client.InnerChannel.OperationTimeout = FirstTimeout;
                                firstCheck = false;
                            }

                            var result = client.IsHealthy();
                            if (!result.IsHealthy)
                            {
                                exString = result.Message;
                            }
                            else
                            {
                                isOk = true;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        exString = ex.ToString();
                    }
                    Thread.Sleep(5000);
                }
                if(!isOk)
                {
                    EventLog.WriteEntry($"Health check failed because of exception: {exString}", EventLogEntryType.Error);
                    ThreadPool.QueueUserWorkItem((s) => Stop());
                    break;
                }
            }
        }
    }
}