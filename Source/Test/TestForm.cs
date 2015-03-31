using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test
{
    public partial class TestForm : Form
    {
        IActPluginV1 plugin;

        public TestForm()
        {
            InitializeComponent();

        }

        private void LoadPlugin()
        {
            
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox.Checked)
            {
                var dllPath = Path.Combine(Application.StartupPath, "ActServer.dll");
                var pdbPath = Path.Combine(Application.StartupPath, "ActServer.pdb");

                if (File.Exists(dllPath) && File.Exists(pdbPath))
                {
                    var dllData = File.ReadAllBytes(dllPath);
                    var pdbData = File.ReadAllBytes(pdbPath);

                    var assembly = AppDomain.CurrentDomain.Load(dllData, pdbData);

                    plugin = (IActPluginV1)assembly.CreateInstance("RainbowMage.ActServer.PluginMain");
                }

                var tabPage = new TabPage("ActServer.dll");
                tabControl.TabPages.Add(tabPage);

                plugin.InitPlugin(tabPage, statusLabel);
            }
            else
            {
                plugin.DeInitPlugin();
                tabControl.TabPages.Clear();
                statusLabel.Text = "";
                plugin = null;
            }
        }
    }
}
