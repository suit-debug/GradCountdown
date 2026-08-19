using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GradCountdown.ViewModels;

namespace GradCountdown.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly MainViewModel _viewModel;
    private readonly ToolStripMenuItem _itemShow;
    private readonly ToolStripMenuItem _itemHide;
    private readonly ToolStripMenuItem _itemTopmost;
    private readonly ToolStripMenuItem _itemAutoStart;
    private readonly ToolStripMenuItem _itemExit;

    public TrayIconService(MainViewModel viewModel)
    {
        _viewModel = viewModel;

        // 尝试加载应用图标
        Icon? appIcon = null;
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(iconPath))
            {
                appIcon = new Icon(iconPath);
            }
            else
            {
                string? exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    appIcon = Icon.ExtractAssociatedIcon(exePath);
                }
            }
        }
        catch { }

        appIcon ??= SystemIcons.Application;

        // 构建右键菜单
        var contextMenu = new ContextMenuStrip();

        _itemShow = new ToolStripMenuItem("显示 GradCountdown", null, (s, e) => _viewModel.ShowWindow());
        _itemHide = new ToolStripMenuItem("隐藏 GradCountdown", null, (s, e) => _viewModel.HideWindow());

        _itemTopmost = new ToolStripMenuItem("始终置顶", null, (s, e) => _viewModel.IsTopmost = !_viewModel.IsTopmost)
        {
            Checked = _viewModel.IsTopmost
        };

        _itemAutoStart = new ToolStripMenuItem("开机自启动", null, (s, e) => _viewModel.IsAutoStart = !_viewModel.IsAutoStart)
        {
            Checked = _viewModel.IsAutoStart
        };

        _itemExit = new ToolStripMenuItem("退出 GradCountdown", null, (s, e) => System.Windows.Application.Current.Shutdown());

        contextMenu.Items.Add(_itemShow);
        contextMenu.Items.Add(_itemHide);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_itemTopmost);
        contextMenu.Items.Add(_itemAutoStart);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_itemExit);

        contextMenu.Opening += (s, e) =>
        {
            _itemTopmost.Checked = _viewModel.IsTopmost;
            _itemAutoStart.Checked = _viewModel.IsAutoStart;
        };

        _notifyIcon = new NotifyIcon
        {
            Icon = appIcon,
            Text = "研究生倒计时",
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _notifyIcon.DoubleClick += (s, e) => _viewModel.ShowWindow();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
