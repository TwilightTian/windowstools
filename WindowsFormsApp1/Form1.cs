using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class form : Form
    {
        private TrackBar _lightBar;
        private Label _lightLab;
        private TrackBar _gamaBar;
        private TrackBar _redBar;
        private TrackBar _greenBar;
        private TrackBar _blueBar;
        private NumericUpDown _gamaNum;
        private NumericUpDown _redNum;
        private NumericUpDown _greenNum;
        private NumericUpDown _blueNum;
        private CheckBox _startBox;
        private NotifyIcon _notify;
        private NumericUpDown _lightNum;
        private String appPath = AppDomain.CurrentDomain.BaseDirectory;
        private String fileName = "";

        public form()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _lightBar = this.trackBar1;
            _lightLab = this.lightLab;
            _gamaBar = this.trackBar2;
            _redBar = this.trackBar3;
            _greenBar = this.trackBar4;
            _blueBar = this.trackBar5;
            _gamaNum = this.numericUpDown2;
            _redNum = this.numericUpDown3;
            _greenNum = this.numericUpDown4;
            _blueNum = this.numericUpDown5;
            _startBox = this.checkBox1;
            _notify = this.notifyIcon1;
            _lightNum = this.numericUpDown1;
            _notify.Visible = false;

            int x = (System.Windows.Forms.SystemInformation.WorkingArea.Width - this.Size.Width) / 2;
            int y = (System.Windows.Forms.SystemInformation.WorkingArea.Height - this.Size.Height) / 2;

            this.StartPosition = FormStartPosition.Manual; //窗体的位置由Location属性决定
            this.Location = (Point)new Size(x, y);
            String str = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            String[] strs = str.Split('\\');
            fileName = strs[strs.Length - 1];
            LoadConfig();

        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern int GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);
        RAMP ramp = new RAMP();

        [DllImport("gdi32.dll")]
        public static extern int SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public UInt16[] Blue;
        }
        void SetGamma(int gamma, int type)
        {
            if (ramp.Red == null)
            {
                ramp.Red = new ushort[256];
            }
            if (ramp.Green == null)
            {
                ramp.Green = new ushort[256];
            }
            if (ramp.Blue == null)
            {
                ramp.Blue = new ushort[256];
            }

            for (int i = 0; i < 256; i++)
            {
                // gamma 必须在3和44之间
                int value = i * (gamma + 128);
                if (value > 65535)
                {
                    value = 65535;
                }
                if (type == 0)
                {
                    ramp.Red[i] = ramp.Green[i] = ramp.Blue[i] = ushort.Parse(value.ToString());

                }
                else if (type == 1)
                {
                    ramp.Red[i] = ushort.Parse(value.ToString());
                }
                else if (type == 2)
                {
                    ramp.Green[i] = ushort.Parse(value.ToString());
                }
                else
                {
                    ramp.Blue[i] = ushort.Parse(value.ToString());
                }
            }
            SetDeviceGammaRamp(GetDC(IntPtr.Zero), ref ramp);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            RevertToPolicyBrightness();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Console.WriteLine(GetDeviceGammaRamp(GetDC(IntPtr.Zero), ref ramp));
            String str = "";
            foreach (short s in ramp.Red)
            {
                str = str + "," + s;
            }
            Console.WriteLine(str);
        }
        static void SetBrightness(byte targetBrightness)
        {
            ManagementScope scope = new ManagementScope("root\\WMI");
            SelectQuery query = new SelectQuery("WmiMonitorBrightnessMethods");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection objectCollection = searcher.Get())
                {
                    foreach (ManagementObject mObj in objectCollection)
                    {
                        mObj.InvokeMethod("WmiSetBrightness",
                            new Object[] { UInt32.MaxValue, targetBrightness });
                        break;
                    }
                }
            }
        }

        static void RevertToPolicyBrightness()
        {
            ManagementScope scope = new ManagementScope("root\\WMI");
            SelectQuery query = new SelectQuery("WmiMonitorBrightnessMethods");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection objectCollection = searcher.Get())
                {
                    foreach (ManagementObject mObj in objectCollection)
                    {
                        mObj.InvokeMethod("WmiRevertToPolicyBrightness", new object[] { });
                        break;
                    }
                }
            }
        }

        static int GetBrightness()
        {
            ManagementScope scope = new ManagementScope("root\\WMI");
            SelectQuery query = new SelectQuery("WmiMonitorBrightness");
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query))
            {
                using (ManagementObjectCollection objectCollection = searcher.Get())
                {
                    foreach (ManagementObject mObj in objectCollection)
                    {
                        foreach (var item in mObj.Properties)
                        {
                            if (item.Name == "CurrentBrightness")
                            {
                                return int.Parse(item.Value.ToString());
                            }
                        }
                    }
                }
            }
            return 0;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            SetBrightness(byte.Parse(_lightBar.Value.ToString()));
            _lightNum.Value = _lightBar.Value;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            SetGamma(_gamaBar.Value, 0);
            this._redBar.Value = _gamaBar.Value;
            this._greenBar.Value = _gamaBar.Value;
            this._blueBar.Value = _gamaBar.Value;
            this._gamaNum.Value = _gamaBar.Value;
            this._redNum.Value = _gamaBar.Value;
            this._greenNum.Value = _gamaBar.Value;
            this._blueNum.Value = _gamaBar.Value;

        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            SetGamma(_redBar.Value, 1);
            this._redNum.Value = _redBar.Value;

        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            SetGamma(_greenBar.Value, 2);
            this._greenNum.Value = _greenBar.Value;

        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            SetGamma(_blueBar.Value, 3);
            this._blueNum.Value = _blueBar.Value;

        }

        private void SetStartUp(bool flag)
        {
            // 添加到 当前登陆用户的 注册表启动项
            RegistryKey RKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
            if (flag)
            {
                RKey.SetValue("LightApp", appPath + fileName);
            }
            else
            {
                RKey.DeleteValue("LightApp");
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            SetStartUp(_startBox.Checked);
        }

        private void InitGamaValue(RAMP ramp)
        {
            int redVal = 0;
            int greenVal = 0;
            int blueVal = 0;
            if (ramp.Red != null)
            {
                ushort value = ramp.Red[1];
                redVal = int.Parse(value.ToString()) - 128;
                if (redVal < 0)
                {
                    redVal = 0;
                }
            }
            if (ramp.Green != null)
            {
                ushort value = ramp.Green[1];
                greenVal = int.Parse(value.ToString()) - 128;
                if (greenVal < 0)
                {
                    greenVal = 0;
                }
            }
            if (ramp.Blue != null)
            {
                ushort value = ramp.Blue[1];
                blueVal = int.Parse(value.ToString()) - 128;
                if (blueVal < 0)
                {
                    blueVal = 0;
                }
            }
            _redBar.Value = redVal;
            _greenBar.Value = greenVal;
            _blueBar.Value = blueVal;

            _redNum.Value = redVal;
            _greenNum.Value = greenVal;
            _blueNum.Value = blueVal;
        }

        private void LoadConfig()
        {
            if (File.Exists(appPath + "\\config.ini"))
            {
                FileStream fs = File.Open(appPath + "\\config.ini", FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                String str = sr.ReadLine();
                int set = 0;
                int bright = 0;
                int gama = 0;
                int red = 0;
                int green = 0;
                int blue = 0;
                int startUp = 0;
                while (str != null)
                {
                    String[] pas = str.Split('=');
                    if (pas.Length == 2)
                    {
                        if ("bright".Equals(pas[0]))
                        {
                            if (int.TryParse(pas[1], out int result))
                            {
                                bright = result;
                            }
                        }
                        else if ("gama".Equals(pas[0]))
                        {
                            if (int.TryParse(pas[1], out int result))
                            {
                                gama = result;
                            }
                        }
                        else if ("red".Equals(pas[0]))
                        {
                            if (int.TryParse(pas[1], out int result))
                            {
                                red = result;
                            }
                        }
                        else if ("green".Equals(pas[0]))
                        {
                            if (int.TryParse(pas[1], out int result))
                            {
                                green = result;
                            }
                        }
                        else if ("blue".Equals(pas[0]))
                        {
                            if (int.TryParse(pas[1], out int result))
                            {
                                blue = result;
                            }
                        }
                        else if ("startup".Equals(pas[0]))
                        {
                            if (int.TryParse(pas[1], out int result))
                            {
                                startUp = result;
                            }
                        }
                        else if ("set".Equals(pas[0]))
                        {
                            if (int.TryParse(pas[1], out int result))
                            {
                                set = result;
                            }
                        }

                    }
                    str = sr.ReadLine();
                }
                _lightBar.Value = bright;
                _lightNum.Value = bright;
                _gamaBar.Value = red;
                _gamaNum.Value = red;
                SetGamma(red, 1);
                SetGamma(green, 2);
                SetGamma(blue, 3);
                InitGamaValue(ramp);
                if (startUp == 1)
                {
                    _startBox.Checked = true;
                }
                if (set == 1)
                {
                    this.mClsItm.Checked = true;
                }
                sr.Close();
            }
            else
            {
                _lightBar.Value = GetBrightness();
                _lightLab.Text = _lightBar.Value.ToString() + "%";
                GetDeviceGammaRamp(GetDC(IntPtr.Zero), ref ramp);
                InitGamaValue(ramp);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            int bright = _lightBar.Value;
            int gama = _gamaBar.Value;
            int red = _redBar.Value;
            int green = _greenBar.Value;
            int blue = _blueBar.Value;
            int startUp = _startBox.Checked == true ? 1 : 0;
            int set = this.mClsItm.Checked == true ? 1 : 0;
            if (!File.Exists(appPath + "\\config.ini"))
            {
                FileStream fs = File.OpenWrite(appPath + "\\config.ini");
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("set=" + set);
                sw.WriteLine("bright=" + bright);
                sw.WriteLine("gama=" + gama);
                sw.WriteLine("red=" + red);
                sw.WriteLine("green=" + green);
                sw.WriteLine("blue=" + blue);
                sw.WriteLine("startup=" + startUp);
                sw.Close();
            }
            else
            {
                FileStream fs = File.Create(appPath + "\\config.ini");
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("set=" + set);
                sw.WriteLine("bright=" + bright);
                sw.WriteLine("gama=" + gama);
                sw.WriteLine("red=" + red);
                sw.WriteLine("green=" + green);
                sw.WriteLine("blue=" + blue);
                sw.WriteLine("startup=" + startUp);
                sw.Close();
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)    //最小化到系统托盘
            {
                _notify.Visible = true;    //显示托盘图标
                this.Hide();    //隐藏窗口
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                //还原窗体显示   
                _notify.Visible = false;
                this.Show();
                this.Focus();
                WindowState = FormWindowState.Normal;
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            SetBrightness(byte.Parse(_lightNum.Value.ToString()));
            _lightBar.Value = int.Parse(_lightNum.Value.ToString());
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            SetGamma(int.Parse(_gamaNum.Value.ToString()), 1);
            _redBar.Value = int.Parse(_gamaNum.Value.ToString());
            _greenBar.Value = int.Parse(_gamaNum.Value.ToString());
            _blueBar.Value = int.Parse(_gamaNum.Value.ToString());
            _gamaBar.Value = int.Parse(_gamaNum.Value.ToString());
            _redNum.Value = int.Parse(_gamaNum.Value.ToString());
            _greenNum.Value = int.Parse(_gamaNum.Value.ToString());
            _blueNum.Value = int.Parse(_gamaNum.Value.ToString());
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            SetGamma(int.Parse(_redNum.Value.ToString()), 1);
            _redBar.Value = int.Parse(_redNum.Value.ToString());
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            SetGamma(int.Parse(_greenNum.Value.ToString()), 2);
            _greenBar.Value = int.Parse(_greenNum.Value.ToString());
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            SetGamma(int.Parse(_blueNum.Value.ToString()), 3);
            _blueBar.Value = int.Parse(_blueNum.Value.ToString());
        }

        private void toolStripMenuItem_Click(object sender, EventArgs e)
        {
            _notify.Visible = false;   //设置图标不可见
            this.Close();                  //关闭窗体
            this.Dispose();                //释放资源
            Application.Exit();
        }

        private void mExtItm_Click(object sender, EventArgs e)
        {
            this.Close();                  //关闭窗体
            this.Dispose();                //释放资源
            Application.Exit();
        }

        private void mClsItm_Click(object sender, EventArgs e)
        {
            if (this.mClsItm.Checked)
            {
                this.mClsItm.Checked = false;
            }
            else
            {
                this.mClsItm.Checked = true;
            }
        }

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            if (this.mClsItm.Checked)
            {
                e.Cancel = true;
                _notify.Visible = true;    //显示托盘图标
                this.Hide();    //隐藏窗口
                this.WindowState = FormWindowState.Minimized;
            }
            else
            {
                e.Cancel = false;
                _notify.Visible = false;   //设置图标不可见
                this.Dispose();                //释放资源
                Application.Exit();
            }
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
        private const int WM_HOTKEY = 0x312; //窗口消息-热键
        private const int WM_CREATE = 0x1; //窗口消息-创建
        private const int WM_DESTROY = 0x2; //窗口消息-销毁
        private const int Space = 0x3572; //
        private const int Space2 = 0x3573; //热键ID


        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            switch (m.Msg)
            {
                case WM_HOTKEY: //窗口消息-热键ID
                    switch (m.WParam.ToInt32())
                    {
                        case Space: //热键ID
                            int g = int.Parse(_gamaNum.Value.ToString());
                            g--;
                            _gamaNum.Value = g;
                            SetGamma(g, 1);

                            break;
                        case Space2:
                            int g2 = int.Parse(_gamaNum.Value.ToString());
                            g2++;
                            _gamaNum.Value = g2;
                            SetGamma(g2, 1);

                            break;
                        default:
                            break;
                    }
                    break;
                case WM_CREATE: //窗口消息-创建
                    AppHotKey.RegKey(Handle, Space, AppHotKey.KeyModifiers.Alt, Keys.F8);
                    AppHotKey.RegKey(Handle, Space2, AppHotKey.KeyModifiers.Alt, Keys.F9);
                    break;
                case WM_DESTROY: //窗口消息-销毁
                    AppHotKey.UnRegKey(Handle, Space); //销毁热键
                    AppHotKey.UnRegKey(Handle, Space2); //销毁热键
                    break;
                default:
                    break;
            }
        }
    }
}
