using Avalonia;
using Avalonia.Headless;
using GammaCnf.App;
if(args.Length!=2){Console.Error.WriteLine("Usage: UiChecks fixture.cnf evidence-directory");return 2;}
return AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).UseSkia().StartWithClassicDesktopLifetime(["--smoke-test",Path.GetFullPath(args[0]),Path.GetFullPath(args[1])]);
