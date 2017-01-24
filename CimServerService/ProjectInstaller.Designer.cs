namespace CimServerService
{
    partial class ProjectInstaller
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
            this.cimServerServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.cimServerServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // cimServerServiceProcessInstaller
            // 
            this.cimServerServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.cimServerServiceProcessInstaller.Password = null;
            this.cimServerServiceProcessInstaller.Username = null;
            // 
            // cimServerServiceInstaller
            // 
            this.cimServerServiceInstaller.Description = "Cim Server Service";
            this.cimServerServiceInstaller.DisplayName = "Cim Server Service";
            this.cimServerServiceInstaller.ServiceName = "CimServerService";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.cimServerServiceProcessInstaller,
            this.cimServerServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller cimServerServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller cimServerServiceInstaller;
    }
}