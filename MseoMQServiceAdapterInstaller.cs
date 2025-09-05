using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using BT.SaaS.Core.Shared.Utils;
using System.IO;
using System.Diagnostics;
using BT.SaaS.Core.EnvironmentManagement.Enviroments.Installers;


namespace BT.SaaS.MSEO.MQService
{
    [RunInstaller(true)]
    public partial class MseoMQServiceAdapterInstaller : WindowsServiceInstaller
    {
        public MseoMQServiceAdapterInstaller()
        {
            InitializeComponent();
        }
    }
}
