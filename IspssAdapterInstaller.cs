using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using BT.SaaS.Core.Shared.Utils;
using BT.SaaS.Core.EnvironmentManagement.Enviroments.Installers;

namespace BT.SaaS.IspssAdapter
{
    [RunInstaller(true)]
    public partial class IspssAdapterInstaller : WebServiceInstaller
    {
        public IspssAdapterInstaller()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // 
            // IspssAdapterInstaller
            // 
            this.ConfigFileTarget = "web.config";
            this.ConfigFileTemplate = "web.template.config";
            this.EnvironmentKey = "External Systems|IspssAdapter";
            this.EventLogSources = "ISPSS Adapter";

        }
    }
}
