using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using BT.SaaS.Core.Shared.Utils;
using BT.SaaS.Core.EnvironmentManagement.Enviroments.Installers;


namespace BT.SaaS.MSEOAdapter
{
    [RunInstaller(true)]
    public partial class MSEOAdapterInstaller : WebServiceInstaller
    {
        public MSEOAdapterInstaller()
        {
            InitializeComponent();
        }
    }
}
