using System;
using System.Threading;
using System.Threading.Tasks;
using GalgameManager.WinApp.Base.Contracts;
using GalgameManager.WinApp.Base.Models;
using PotatoVN.App.PluginBase.Helper;

namespace PotatoVN.App.PluginBase
{
    public partial class Plugin : IPlugin
    {
        public static IPotatoVnApi HostApi { get; private set; } = null!;
        private IPotatoVnApi _hostApi = null!;
        
        public PluginInfo Info { get; } = new()
        {
            Id = new Guid("035308de-bdc6-4605-bef5-9ed1bbade37a"), 
            Name = "游戏报告",
            Description = "为 PotatoVN 提供游戏月报与年度报告入口，汇总游玩时长、游戏排行、游玩习惯和鉴赏记录。",
        };

        public Task InitializeAsync(IPotatoVnApi hostApi)
        {
            _hostApi = hostApi;
            HostApi = hostApi;
            XamlResourceLocatorFactory.PackagePath = _hostApi.GetPluginPath();
            ResourceLoader.Initialize();
            InitUi();
            return Task.CompletedTask;
        }
        
        public Task OnUninstallAsync(bool deleteData, Action<TimeSpan> extendWaitHandler, CancellationToken cts)
        {
            if (cts.IsCancellationRequested) return Task.FromCanceled(cts);
            ResourceLoader.Unload(); // 卸载XAML资源字典
            return Task.CompletedTask;
        }
        
        protected Guid Id => Info.Id;
    }
}
