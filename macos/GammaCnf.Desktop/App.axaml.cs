using Avalonia.Platform.Storage;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
namespace GammaCnf.App;
public partial class App : Application
{
 public override void Initialize()=>AvaloniaXamlLoader.Load(this);
 public override void OnFrameworkInitializationCompleted()
 {
  if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
  {
   var window=new MainWindow();desktop.MainWindow=window;
   desktop.ShutdownMode=Avalonia.Controls.ShutdownMode.OnMainWindowClose;
   // Finder open-with events also work after the application has already started.
   if(this.TryGetFeature(typeof(IActivatableLifetime)) is IActivatableLifetime activatable)activatable.Activated+=(_,e)=>
   {if(e is FileActivatedEventArgs files)Dispatcher.UIThread.Post(()=>{foreach(var file in files.Files)if(file.TryGetLocalPath() is {} path)window.OpenPath(path);window.Activate();});};
   var args=desktop.Args??[];
   window.AutomatedSmoke=args.Length>0&&args[0]=="--smoke-test";
   window.Opened+=(_,_)=>Dispatcher.UIThread.Post(async ()=>
   {
    if(args.Length==3&&args[0]=="--smoke-test")
    {
     int code=0;Directory.CreateDirectory(args[2]);
     try{await window.RunSmokeChecks(args[1],args[2]);File.WriteAllText(Path.Combine(args[2],"ui-smoke-success.txt"),"PASS: native Avalonia CNF load, calibration, background, ROI/counts, log/energy PNG render, CSV/TXT export, efficiency GLS/JSON/plot, activity, multi-spectrum isolation, automatic time normalization, automatic peak/ROI/edit/cancel");}
     catch(Exception error){File.WriteAllText(Path.Combine(args[2],"ui-smoke-error.txt"),error.ToString());code=1;}
     desktop.Shutdown(code);
    }
    else foreach(var path in args.Where(a=>!a.StartsWith("--")))window.OpenPath(path);
   },DispatcherPriority.Background);
  }
  base.OnFrameworkInitializationCompleted();
 }
}
