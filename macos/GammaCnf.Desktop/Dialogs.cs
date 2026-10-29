using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
namespace GammaCnf.App;
internal static class Dialogs
{
 private static FilePickerFileType Type(string extension)=>new(extension.ToUpperInvariant()+" files") {Patterns=["*."+extension,"*."+extension.ToUpperInvariant()]};
 internal static async Task<string[]> Open(Window owner,string title,string extension,bool multiple=false)
 {
  var files=await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions{Title=title,AllowMultiple=multiple,FileTypeFilter=[Type(extension),FilePickerFileTypes.All]});
  return files.Select(f=>f.TryGetLocalPath()).OfType<string>().ToArray();
 }
 internal static async Task<string?> Save(Window owner,string title,string name,string extension)
 {
  var file=await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{Title=title,SuggestedFileName=name,DefaultExtension=extension,FileTypeChoices=[Type(extension)],ShowOverwritePrompt=true});
  return file?.TryGetLocalPath();
 }
 internal static Task Message(Window owner,string text,string title)=>Show(owner,text,title,false);
 internal static Task<bool> Confirm(Window owner,string text,string title)=>Show(owner,text,title,true);
 private static async Task<bool> Show(Window owner,string text,string title,bool confirm)
 {
  var dialog=new Window{Title=title,Width=520,SizeToContent=SizeToContent.Height,CanResize=false,WindowStartupLocation=WindowStartupLocation.CenterOwner};
  var yes=new Button{Content=confirm?"Tiếp tục":"Đóng",IsDefault=true};
  var no=new Button{Content="Hủy",IsCancel=true};
  yes.Click+=(_,_)=>dialog.Close(true);no.Click+=(_,_)=>dialog.Close(false);
  var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Spacing=8,Margin=new Thickness(0,18,0,0)};
  if(confirm)buttons.Children.Add(no);buttons.Children.Add(yes);
  dialog.Content=new StackPanel{Margin=new Thickness(24),Children={new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap},buttons}};
  return await dialog.ShowDialog<bool>(owner);
 }
}
