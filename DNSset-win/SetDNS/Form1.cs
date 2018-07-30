using SetDNS.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SetDNS
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //Icon = ShareXResources.Icon;

            AddDNS("自定义方案&默认方案","","");
            AddDNS("Pure DNS", "123.207.137.88", "115.159.220.214");
            AddDNS("Xsico DNS", "115.159.96.69", "122.114.245.45");
            AddDNS("Mogu DNS", "122.114.245.45", "115.159.96.69");
            AddDNS("AIXYZ DNS", "115.159.146.99", "123.206.21.48");
            AddDNS("中科大DNS 电信", "202.141.162.123", "202.141.162.123");
            AddDNS("中科大DNS 移动", "202.141.176.93", "202.141.176.93");
            AddDNS("中科大DNS 教育网", "202.38.93.153", "202.141.162.123");
           
        

            foreach (AdapterInfo adapter in AdapterInfo.GetEnabledAdapters())
            {
                cbAdapters.Items.Add(adapter);
            }

            if (cbAdapters.Items.Count > 0)
            {
                cbAdapters.SelectedIndex = 0;
            }
        }

        private void AddDNS(string name, string primary = null, string secondary = null)
        {
            cbDNSType.Items.Add(new DNSInfo(name, primary, secondary));
        }

        private void cbAdapters_SelectedIndexChanged(object sender, EventArgs e)
        {
            AdapterInfo adapter = cbAdapters.SelectedItem as AdapterInfo;

            if (adapter != null)
            {
                string[] dns = adapter.GetDNS();

                if (dns != null && dns.Length == 2)
                {
                    cbAutomatic.Checked = false;
                    txtPreferredDNS.Text = dns[0];
                    txtAlternateDNS.Text = dns[1];
                }
                else
                {
                    cbAutomatic.Checked = false;
                }

                cbDNSType.SelectedIndex = 0;
            }

            UpdateControls();
        }

        private void cbAutomatic_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void cbDNSType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbDNSType.SelectedIndex > 0)
            {
                DNSInfo dnsInfo = cbDNSType.SelectedItem as DNSInfo;

                if (dnsInfo != null)
                {
                    txtPreferredDNS.Text = dnsInfo.PrimaryDNS;
                    txtAlternateDNS.Text = dnsInfo.SecondaryDNS;
                }
            }

            UpdateControls();
        }

        private void txtPreferredDNS_TextChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void txtAlternateDNS_TextChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void UpdateControls()
        {
            cbDNSType.Enabled = !cbAutomatic.Checked;
            txtPreferredDNS.Enabled = !cbAutomatic.Checked && cbDNSType.SelectedIndex == 0;
            txtAlternateDNS.Enabled = !cbAutomatic.Checked && cbDNSType.SelectedIndex == 0;
        }

        private void SendPing(string ip)
        {
            if (!string.IsNullOrEmpty(ip))
            {
                btnPingPrimary.Enabled = btnPingSecondary.Enabled = false;

                TaskEx.Run(() =>
                {
                    PingResult pingResult = PingHelper.PingHost(ip);
                    this.nc.ShowBalloonTip(5000, "状态完成", pingResult.ToString(), ToolTipIcon.Info);
                    //MessageBox.Show(pingResult.ToString(), "Menglolita Host", MessageBoxButtons.OK, MessageBoxIcon.Information);
                },
                () =>
                {
                    btnPingPrimary.Enabled = btnPingSecondary.Enabled = true;
                });
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            AdapterInfo adapter = cbAdapters.SelectedItem as AdapterInfo;

            if (adapter != null)
            {
                uint result;

                try
                {
                    if (cbAutomatic.Checked)
                    {
                        result = adapter.SetDNSAutomatic();
                    }
                    else
                    {
                        string primaryDNS = txtPreferredDNS.Text.Trim();
                        string secondaryDNS = txtAlternateDNS.Text.Trim();

                        if (Helpers.IsValidIPAddress(primaryDNS) && Helpers.IsValidIPAddress(secondaryDNS))
                        {
                            result = adapter.SetDNS(primaryDNS, secondaryDNS);
                        }
                        else
                        {
                            throw new Exception("无法验证IP");
                        }
                    }

                    if (result == 0)
                    {
                        NativeMethods.DnsFlushResolverCache();
                        MessageBox.Show("DNS成功更新", "已完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (result == 1)
                    {
                        MessageBox.Show("DNS成功更新，但需要重启", "已完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else if (result > 1)
                    {
                        MessageBox.Show("DNS更新出错"+ " " + result, "已完成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DNS更新失败"+ "\r\n" + ex, "已完成", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnPingPrimary_Click(object sender, EventArgs e)
        {
            SendPing(txtPreferredDNS.Text);
        }

        private void btnPingSecondary_Click(object sender, EventArgs e)
        {
            SendPing(txtAlternateDNS.Text);
        }

        private void lblPreferredDNS_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            string Path = AppDomain.CurrentDomain.BaseDirectory;
            if (System.IO.File.Exists(Path + "bin" + @"\" + "reg.hbs"))
            {
                checkBox2.Checked = true;
                Process proc = new Process();
                proc.StartInfo.FileName = Path + "bin" + @"\" + "dnsproxy.exe";
                proc.Start();
            }
            else
            {
                Process regservice = new Process();
                regservice.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory+ "bin" + @"\" + @"regservice.bat";
                regservice.StartInfo.CreateNoWindow = true;   //不创建该进程的窗口
                regservice.StartInfo.UseShellExecute = false;   //不使用shell壳运行
                regservice.Start();
                string filename = Path + "bin" + @"\" + "reg.hbs";
                string word = "true";
                FileStream fs = File.Create(filename);  //创建文件
                fs.Close();
                StreamWriter sw = new StreamWriter(filename);  //创建写入流
                sw.Write("配置文件" + "{" + word + "}");
                sw.Flush();
                sw.Close();
            }
            if (System.IO.File.Exists(Path + "bin" + @"\" + "DNSCrypt.use"))
            {
                checkBox2.Checked = true;
                Process proc = new Process();
                proc.StartInfo.FileName = Path + "bin" + @"\" + "OpenDNSInterface.exe";
                proc.Start();
            }
            else
            {
                checkBox2.Checked = false;
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (checkBox2.CheckState == CheckState.Checked)
            {
                string Path = AppDomain.CurrentDomain.BaseDirectory;
                string filename2 = Path + "bin" + @"\" + "DNSCrypt.use";
                string word2 = "true";
                FileStream fs2 = File.Create(filename2);  //创建文件
                fs2.Close();
                StreamWriter sw2 = new StreamWriter(filename2);  //创建写入流
                sw2.Write("配置文件" + "{" + word2 + "}");
                sw2.Flush();
                sw2.Close();
                MessageBox.Show("设置保存完成\r\n重启生效", "已配置", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (checkBox2.CheckState == CheckState.Unchecked)
            {
                string Path = AppDomain.CurrentDomain.BaseDirectory;
                string filename = Path + "bin" + @"\" + "DNSCrypt.use";
                File.Delete(filename);
                this.Close();
                MessageBox.Show("设置保存完成\r\n重启生效", "已配置", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                //
            }
        }
    }
}
