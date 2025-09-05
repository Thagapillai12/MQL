namespace BT.SaaS.MSEO.MQService
{
    partial class MseoMQServiceAdapterInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 
            // MseoMQServiceAdapterInstaller
            // 
            this.ConfigFileTarget = "BT.SaaS.MSEO.MQService.exe.config";
            this.ConfigFileTemplate = "BT.SaaS.MSEO.MQService.template.config";
            this.ConfigurationFilename = "BT.SaaS.MSEO.MQService.exe.config";
            this.EnvironmentKey = "External Systems|MSEO.MQService";
            this.EventLogSources = "MSEO MQService Adapter";

        }

        #endregion
    }
}