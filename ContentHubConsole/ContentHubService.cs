using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole
{
    partial class ContentHubService : ServiceBase
    {
        public static string RunningServiceName = String.Empty;

        public ContentHubService(string serviceName)
        {
            InitializeComponent();
            this.ServiceName = serviceName;
            RunningServiceName = serviceName;
        }

        protected override void OnStart(string[] args)
        {
            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string location = Path.Combine(executableLocation, $"{RunningServiceName}.txt");

            File.WriteAllText(location, "Starting.");
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }
    }
}
