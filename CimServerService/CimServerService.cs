using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using SimpleChat;

namespace CimServerService
{
    public partial class CimServerService : ServiceBase
    {
        public CimServerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Server.Instance.Start();
        }

        protected override void OnStop()
        {
            Server.Instance.Stop();
        }
    }
}
