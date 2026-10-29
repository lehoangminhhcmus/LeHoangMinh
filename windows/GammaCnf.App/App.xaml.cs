using System.IO;
using System.Windows;
using System.Windows.Threading;
namespace GammaCnf.App;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window=new MainWindow();MainWindow=window;window.Show();
        if(e.Args.Length==3 && e.Args[0]=="--smoke-test")
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(async ()=>
            {
                int code=0;
                try { await window.RunSmokeChecks(e.Args[1],e.Args[2]);File.WriteAllText(Path.Combine(e.Args[2],"ui-smoke-success.txt"),"PASS: WPF load, calibration, background, ROI, log/energy render, CSV/TXT/PNG export, efficiency GLS/JSON/plot, activity calculation, multi-spectrum state isolation, automatic background timing, ROI sums, automatic peak search/ROI/edit/cancel"); }
                catch(Exception error) { Directory.CreateDirectory(e.Args[2]);File.WriteAllText(Path.Combine(e.Args[2],"ui-smoke-error.txt"),error.ToString());code=1; }
                Shutdown(code);
            }));
    }
}
