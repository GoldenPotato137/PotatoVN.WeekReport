using System.Threading.Tasks;
using GalgameManager.WinApp.Base.Models.Plugin;
using PotatoVN.App.PluginBase.Controls;

namespace PotatoVN.App.PluginBase;

public partial class Plugin
{
    private bool _uiInit;
    
    private void InitUi()
    {
        if (_uiInit) return;
        _hostApi.RegisterSidebarButton(new SidebarButtonInfo
        {
           Id = "monthly-report",
           Text = "月报",
           Placement = SidebarButtonPlacement.Menu, 
           FluentGlyph = "&#xE9D2;",
           FallbackGlyph = "\uE9D2",
        }, () =>
        {
            _hostApi.NavigateTo(typeof(MonthlyReportPage), "游戏月报");
            return Task.CompletedTask;
        });
        _uiInit = true;
    }
}