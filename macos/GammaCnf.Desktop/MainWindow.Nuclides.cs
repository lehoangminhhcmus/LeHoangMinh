using Avalonia.Controls;
using System.IO;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GammaCnf.Core;
namespace GammaCnf.App;
public partial class MainWindow
{
    private NuclideLibrary? nuclideLibrary;
    private string libraryMessage="";
    private static string LibrarySettings=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GammaCNFReader","nuclide-library-path.txt");
    private void InitializeNuclides()
    {
        string defaultDirectory=Path.Combine(Path.GetDirectoryName(LibrarySettings)!,"nuclide-library");
        LibraryFolder.Text=defaultDirectory;
        try
        {
            if(File.Exists(LibrarySettings)&&new FileInfo(LibrarySettings).Length<4096)LibraryFolder.Text=File.ReadAllText(LibrarySettings).Trim();
            if(Directory.Exists(LibraryFolder.Text)&&Directory.EnumerateFiles(LibraryFolder.Text).Any(p=>Path.GetExtension(p).Equals(".nlb",StringComparison.OrdinalIgnoreCase)||Path.GetExtension(p).Equals(".csv",StringComparison.OrdinalIgnoreCase)))LoadNuclideLibrary(false);
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or CnfException or ArgumentException){libraryMessage="Chưa nạp thư viện: "+error.Message;}
        RefreshNuclideSuggestions();
    }
    private void RunNuclideSmokeChecks(string directory)
    {
        string folder=Path.Combine(directory,"synthetic-nuclide-library");Directory.CreateDirectory(folder);
        double energy=rows.Single().Result.EnergyKeV??throw new Exception("Missing energy for nuclide UI check.");
        string E(double value)=>value.ToString("G17",System.Globalization.CultureInfo.InvariantCulture);
        File.WriteAllText(Path.Combine(folder,"synthetic.csv"),NuclideLibrary.CsvHeader+"\nSYNTHETIC-ONLY,"+E(energy)+",50,UI SYNTHETIC TEST ONLY\nSYNTHETIC-ONLY,"+E(energy+15)+",30,UI SYNTHETIC TEST ONLY\nALTERNATIVE-TEST,"+E(energy+1)+",40,UI SYNTHETIC TEST ONLY\nALTERNATIVE-TEST,"+E(energy+30)+",20,UI SYNTHETIC TEST ONLY\n");
        LibraryFolder.Text=folder;LoadNuclideLibrary(false);
        var results=NuclideGrid.ItemsSource as IReadOnlyList<NuclideSuggestion>;
        if(results is null||results.Count!=2||results[0].Nuclide!="SYNTHETIC-ONLY")throw new Exception("Nuclide suggestions UI failed.");
        AnalysisTabs.SelectedIndex=1;UpdateLayout();NuclideGrid.UpdateLayout();
        NuclideGrid.SelectedItem=results[0];
        if(Plot.ConfirmationMarkers.Count!=2||Math.Abs(Plot.ConfirmationMarkers[0].EnergyKeV-energy)>1e-9)throw new Exception("Selecting isotope did not draw its confirmation lines.");
        NuclideGrid.SelectedItem=results[1];
        if(Plot.ConfirmationMarkers.Count!=2||Plot.ConfirmationMarkers.Any(m=>m.Nuclide!="ALTERNATIVE-TEST"))throw new Exception("Selecting another isotope retained previous confirmation markers.");
        NuclideGrid.SelectedItem=results[0];
        EnergyCheck.IsChecked=false;Plot.InvalidateVisual();
        if(Plot.ConfirmationMarkers.Count!=2)throw new Exception("Channel mode lost isotope confirmation lines.");
        EnergyCheck.IsChecked=true;
        ClearConfirmationClick(this,new RoutedEventArgs());
        if(Plot.ConfirmationMarkers.Count!=0)throw new Exception("Hide confirmation lines failed.");
        NuclideGrid.SelectedItem=results[0];
        LibraryFolder.Text=Path.Combine(directory,"missing-library");LoadNuclideLibrary(false);
        if(NuclideGrid.ItemsSource is not null||nuclideLibrary is not null||Plot.ConfirmationMarkers.Count!=0)throw new Exception("Failed library load retained stale suggestions.");
        LibraryFolder.Text=folder;LoadNuclideLibrary(false);
        NuclideGrid.SelectedItem=((IReadOnlyList<NuclideSuggestion>)NuclideGrid.ItemsSource!)[0];
        File.WriteAllText(Path.Combine(directory,"nuclide-ui-success.txt"),"PASS: load library directory, display candidate for calibrated ROI, reject missing directory and clear stale matches");
    }
    private void LoadNuclideLibrary(bool remember=true)
    {
        // Clear earlier matches before loading so a failed replacement cannot show stale identity guesses.
        nuclideLibrary=null;ClearConfirmation();NuclideGrid.ItemsSource=null;
        try
        {
            var loaded=NuclideLibrary.LoadDirectory((LibraryFolder.Text??"").Trim());
            nuclideLibrary=loaded;LibraryFolder.Text=loaded.DirectoryPath;
            libraryMessage=$"{loaded.Lines.Count:N0} vạch đã nạp. "+string.Join(" ",loaded.Warnings);
            if(remember)
            {
                try{Directory.CreateDirectory(Path.GetDirectoryName(LibrarySettings)!);File.WriteAllText(LibrarySettings,loaded.DirectoryPath);}
                catch(Exception e)when(e is IOException or UnauthorizedAccessException){libraryMessage+=" Không lưu được đường dẫn; thư viện chỉ dùng trong phiên này.";}
            }
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or CnfException or ArgumentException){libraryMessage="Không nạp được thư viện: "+error.Message;}
        RefreshNuclideSuggestions();
    }
    private void NuclideSelectionChanged(object? sender,SelectionChangedEventArgs e)
    {
        ClearConfirmation();
        if(NuclideGrid.SelectedItem is not NuclideSuggestion selected||nuclideLibrary is null||session is null)return;
        try
        {
            // Use the library of the selected candidate; do not mix alternative source evaluations.
            var lines=nuclideLibrary.Lines.Where(l=>string.Equals(l.Nuclide,selected.Nuclide,StringComparison.OrdinalIgnoreCase)&&l.FileName==selected.FileName);
            Plot.ConfirmationMarkers=NuclideConfirmation.Markers(session.Sample,lines);Plot.InvalidateVisual();
        }
        catch(CnfException error){NuclideSummary.Text="Không vẽ được vạch đối chiếu: "+error.Message;}
    }
    private void ClearConfirmation()
    {
        Plot.ConfirmationMarkers=[];Plot.InvalidateVisual();
    }
    private void ClearConfirmationClick(object sender,RoutedEventArgs e)
    {
        NuclideGrid.SelectedItem=null;ClearConfirmation();
    }
    private void LoadLibraryClick(object sender,RoutedEventArgs e)=>LoadNuclideLibrary();
    private void RefreshNuclidesClick(object sender,RoutedEventArgs e)=>RefreshNuclideSuggestions();
    private void RefreshNuclideSuggestions()
    {
        if(NuclideGrid is null)return;
        ClearConfirmation();NuclideGrid.ItemsSource=null;
        if(nuclideLibrary is null){NuclideSummary.Text=libraryMessage.Length>0?libraryMessage:"Chọn thư mục chứa thư viện NLB của Genie hoặc CSV. Chưa có thư viện nên chưa gợi ý đồng vị.";return;}
        try
        {
            var results=nuclideLibrary.Suggest(rows.Select(r=>r.Result).ToArray(),Number(NuclideTolerance.Text),Number(NuclideFwhm.Text));
            NuclideGrid.ItemsSource=results;
            int usable=rows.Count(r=>r.Result.Success&&r.Result.EnergyKeV is double e&&double.IsFinite(e)&&e>0);
            NuclideSummary.Text=libraryMessage+$" {results.Count:N0} vạch khớp / {usable} ROI có fit và năng lượng hợp lệ. "+(usable==0?"Cần phổ có chuẩn năng lượng và ROI fit thành công.":results.Count==0?"Không có vạch nằm trong dung sai.":"Ưu tiên đồng vị có vạch khớp ở các ROI khác, rồi độ lệch năng lượng; không phải xác suất nhận dạng.");
        }
        catch(CnfException error){NuclideSummary.Text=libraryMessage+" "+error.Message;}
    }
    private async void ChooseLibraryClick(object sender,RoutedEventArgs e)
    {
        var folders=await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions{Title="Chọn thư mục thư viện NLB/CSV",AllowMultiple=false});
        if(folders.Count>0&&folders[0].TryGetLocalPath() is string directory){LibraryFolder.Text=directory;LoadNuclideLibrary();}
    }
}
