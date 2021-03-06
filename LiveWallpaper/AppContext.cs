﻿using Giantapp.LiveWallpaper.Engine;
using LiveWallpaper.LocalServer;
using NLog;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace LiveWallpaper
{
    public class AppContext : ApplicationContext
    {
        #region ui
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _btnMainUI;
        private ToolStripMenuItem _btnCommunity;
        private ToolStripMenuItem _btnSetting;
        private ToolStripMenuItem _btnExit;
        private System.ComponentModel.IContainer _components;
        #endregion

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly LanService _lanService = new LanService();
        private static Dispatcher _uiDispatcher;
        private static Mutex _mutex;

        public AppContext()
        {
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            InitializeUI();

            WallpaperApi.Initlize(_uiDispatcher);
            AppManager.CultureChanged += LanService_CultureChanged;
            SetMenuText();
            _ = Task.Run(() =>
            {
                int port = GetPort();
                ServerWrapper.Start(port);
            });
            CheckMutex();
        }

        private async void CheckMutex()
        {
            try
            {
                _mutex = new Mutex(true, "Livewallpaper", out bool ret);

                if (!ret)
                {
                    _notifyIcon.ShowBalloonTip(5, await GetText("common.information"), await GetText("client.started"), ToolTipIcon.Info);
                    Environment.Exit(0);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private void InitializeUI()
        {
            _components = new System.ComponentModel.Container();
            _contextMenu = new ContextMenuStrip();

            //_contextMenu.Items.Add(new ToolStripSeparator());

            _btnCommunity = new ToolStripMenuItem();
            _btnCommunity.Click += BtnCommunity_Click;
            _contextMenu.Items.Add(_btnCommunity);

            _btnMainUI = new ToolStripMenuItem();
            _btnMainUI.Click += BtnMainUI_Click;
            _contextMenu.Items.Add(_btnMainUI);

            _btnSetting = new ToolStripMenuItem();
            _btnSetting.Click += BtnSetting_Click;
            string dir = Path.GetDirectoryName(Application.ExecutablePath);
            string imgPath = Path.Combine(dir, "Assets", "setting.png");
            _btnSetting.Image = Image.FromFile(imgPath);
            _contextMenu.Items.Add(_btnSetting);

            _contextMenu.Items.Add(new ToolStripSeparator());

            _btnExit = new ToolStripMenuItem();
            _btnExit.Click += BtnExit_Click;
            _contextMenu.Items.Add(_btnExit);

            _notifyIcon = new NotifyIcon(_components)
            {
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            _notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
            _notifyIcon.MouseClick += new MouseEventHandler(NotifyIcon_MouseClick);
        }

        private void SetMenuText()
        {
            _ = _uiDispatcher.Invoke(async () =>
              {
                  _btnCommunity.Text = await GetText("community.title");
                  _btnMainUI.Text = await GetText("local.title");
                  _btnExit.Text = await GetText("client.exit");
                  _btnSetting.Text = await GetText("common.settings");
                  _notifyIcon.Text = await GetText("common.appName");
              });
        }

        private async Task<string> GetText(string key)
        {
            if (AppManager.UserSetting == null)
            {
                await AppManager.LoadUserSetting();
            }
            string culture = AppManager.UserSetting.General.CurrentLan ?? Thread.CurrentThread.CurrentCulture.Name;
            var r = await _lanService.GetText(key, culture);
            return r;
        }

        private void LanService_CultureChanged(object sender, EventArgs e)
        {
            SetMenuText();
        }

        /// <summary>
        /// 获取可用端口
        /// </summary>
        /// <returns></returns>
        static int GetPort()
        {
            //#if DEBUG
            return 5001;
            //#endif
            //TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            //l.Start();
            //int port = ((IPEndPoint)l.LocalEndpoint).Port;
            //l.Stop();
            //return port;
        }

        private void BtnCommunity_Click(object sender, EventArgs e)
        {
            OpenUrl("https://livewallpaper.giantapp.cn/wallpapers");
        }
        private void BtnMainUI_Click(object sender, EventArgs e)
        {
            OpenUrl("https://livewallpaper.giantapp.cn/local");
        }

        private void BtnSetting_Click(object sender, EventArgs e)
        {
            OpenUrl("https://livewallpaper.giantapp.cn/dashboard/client/setting");
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                 BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(_notifyIcon, null);
            }
        }

        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu",
                   BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(_notifyIcon, null);
        }

        private void BtnExit_Click(object Sender, EventArgs e)
        {
            _notifyIcon.Icon.Dispose();
            _notifyIcon.Dispose();
            Application.Exit();
        }
    }
}
