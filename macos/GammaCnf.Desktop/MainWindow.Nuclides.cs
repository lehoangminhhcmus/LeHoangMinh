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
        LibraryFolder.Text=NuclideLibrary.BundledLibraryPath;
        try
        {
            if(File.Exists(LibrarySettings)&&new FileInfo(LibrarySettings).Length<4096)
            {
                string saved=File.ReadAllText(LibrarySettings).Trim();
                if(saved.Length>0)LibraryFolder.Text=saved;
            }
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or ArgumentException)
        {libraryMessage="Không đọc được thiết lập thư viện: "+error.Message;}
        LoadNuclideLibrary(false);
        if(nuclideLibrary is null&&LibraryFolder.Text!=NuclideLibrary.BundledLibraryPath)
        {
            string failure=libraryMessage;
            LibraryFolder.Text=NuclideLibrary.BundledLibraryPath;LoadNuclideLibrary(false);
            libraryMessage=failure+" Đã chuyển sang thư viện tích hợp. "+libraryMessage;
            RefreshNuclideSuggestions();
        }
    }
    private void RunNuclideSmokeChecks(string directory)
    {
        if(nuclideLibrary is null)throw new Exception("Startup did not initialize a nuclide library.");
        if(!File.Exists(LibrarySettings)&&LibraryFolder.Text!=NuclideLibrary.BundledLibraryPath)
            throw new Exception("First startup did not select the bundled library.");
        LibraryFolder.Text=NuclideLibrary.BundledLibraryPath;LoadNuclideLibrary(false);
        if(nuclideLibrary is null||!nuclideLibrary.Lines.Any(l=>l.Nuclide=="CS-137"&&Math.Abs(l.EnergyKeV-661.657)<.1))
            throw new Exception("Bundled Stdlib library did not load in UI.");
        string folder=Path.Combine(directory,"synthetic-nuclide-library");Directory.CreateDirectory(folder);
        double energy=rows.Single().Result.EnergyKeV??throw new Exception("Missing energy for nuclide UI check.");
        string E(double value)=>value.ToString("G17",System.Globalization.CultureInfo.InvariantCulture);
        File.WriteAllText(Path.Combine(folder,"synthetic.csv"),NuclideLibrary.CsvHeader+"\nSYNTHETIC-ONLY,"+E(energy)+",50,UI SYNTHETIC TEST ONLY\nSYNTHETIC-ONLY,"+E(energy+15)+",30,UI SYNTHETIC TEST ONLY\nALTERNATIVE-TEST,"+E(energy+1)+",40,UI SYNTHETIC TEST ONLY\nALTERNATIVE-TEST,"+E(energy+30)+",20,UI SYNTHETIC TEST ONLY\n");
        LibraryFolder.Text=folder;LoadNuclideLibrary(false);
        var results=NuclideGrid.ItemsSource as IReadOnlyList<NuclideSuggestion>;
        if(results is null||results.Count!=2||results[0].Nuclide!="SYNTHETIC-ONLY")throw new Exception("Nuclide suggestions UI failed.");
        AnalysisTabs.SelectedIndex=1;UpdateLayout();NuclideGrid.UpdateLayout();
        NuclideGrid.SelectedItem=((IReadOnlyList<NuclideSuggestion>)NuclideGrid.ItemsSource!)[0];
        if(Plot.ConfirmationMarkers.Count!=2||Math.Abs(Plot.ConfirmationMarkers[0].EnergyKeV-energy)>1e-9)throw new Exception("Selecting isotope did not draw its confirmation lines.");
        NuclideGrid.SelectedItem=results[1];
        if(Plot.ConfirmationMarkers.Count!=2||Plot.ConfirmationMarkers.Any(m=>m.Nuclide!="ALTERNATIVE-TEST"))throw new Exception("Selecting another isotope retained previous confirmation markers.");
        NuclideGrid.SelectedItem=((IReadOnlyList<NuclideSuggestion>)NuclideGrid.ItemsSource!)[0];
        ConfirmGammaClick(this,new RoutedEventArgs());
        if(active?.ConfirmedLines.Count!=1)throw new Exception("Explicit gamma confirmation was not stored for ROI.");
        var probabilityWindow=new EfficiencyWindow(session!,[rows.Single().Result],active.Path,"SYNTHETIC-PG-TEST",null,new EfficiencyWorkspace(),active.ConfirmedLines.Values.ToArray());
        try{probabilityWindow.RunLibraryGammaSmoke(directory);}finally{probabilityWindow.Close();}
        NuclideGrid.SelectedItem=((IReadOnlyList<NuclideSuggestion>)NuclideGrid.ItemsSource!)[0];
        EnergyCheck.IsChecked=false;Plot.InvalidateVisual();
        if(Plot.ConfirmationMarkers.Count!=2)throw new Exception($"Channel mode lost isotope confirmation lines: count={Plot.ConfirmationMarkers.Count}, selected={NuclideGrid.SelectedItem}, library={nuclideLibrary?.Lines.Count}, session={session?.Sample.Energy(300)}.");
        EnergyCheck.IsChecked=true;
        ClearConfirmationClick(this,new RoutedEventArgs());
        if(Plot.ConfirmationMarkers.Count!=0)throw new Exception("Hide confirmation lines failed.");
        NuclideGrid.SelectedItem=((IReadOnlyList<NuclideSuggestion>)NuclideGrid.ItemsSource!)[0];
        LibraryFolder.Text=Path.Combine(directory,"missing-library");LoadNuclideLibrary(false);
        if(NuclideGrid.ItemsSource is not null||nuclideLibrary is not null||Plot.ConfirmationMarkers.Count!=0||active!.ConfirmedLines.Count!=0)throw new Exception("Failed library load retained stale suggestions.");
        LibraryFolder.Text=folder;LoadNuclideLibrary(false);
        NuclideGrid.SelectedItem=((IReadOnlyList<NuclideSuggestion>)NuclideGrid.ItemsSource!)[0];
        File.WriteAllText(Path.Combine(directory,"nuclide-ui-success.txt"),"PASS: bundled Stdlib, load library directory, display candidate for calibrated ROI, reject missing directory and clear stale matches");
    }
    private void LoadNuclideLibrary(bool remember=true)
    {
        // Clear earlier matches before loading so a failed replacement cannot show stale identity guesses.
        foreach(var document in documents)document.ConfirmedLines.Clear();
        nuclideLibrary=null;ClearConfirmation();NuclideGrid.SelectedItem=null;NuclideGrid.ItemsSource=null;
        try
        {
            string selected=(LibraryFolder.Text??"").Trim();
            var loaded=selected==NuclideLibrary.BundledLibraryPath?NuclideLibrary.LoadBundled():NuclideLibrary.LoadDirectory(selected);
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
    private void ConfirmGammaClick(object sender,RoutedEventArgs e)
    {
        if(active is null||session is null||NuclideGrid.SelectedItem is not NuclideSuggestion selected)
        {NuclideSummary.Text="Chọn một hàng vạch đồng vị để xác nhận cho ROI tương ứng.";return;}
        try
        {
            if(selected.Roi<1||selected.Roi>rows.Count)throw new CnfException("ROI đã thay đổi; cập nhật gợi ý trước.");
            var confirmed=ConfirmedGammaLine.Create(session,rows[selected.Roi-1].Result,selected);
            active.ConfirmedLines[confirmed.Roi]=confirmed;
            NuclideSummary.Text=libraryMessage+$" Đã xác nhận ROI {confirmed.Roi.Start}–{confirmed.Roi.End}: {confirmed.Line.Nuclide}, {confirmed.Line.EnergyKeV:G9} keV. Mở Hiệu suất/Hoạt độ để chọn lấy Pγ từ thư viện hoặc nhập tay.";
        }
        catch(CnfException error){NuclideSummary.Text=error.Message;}
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
    private void UseBundledLibraryClick(object sender,RoutedEventArgs e)
    {
        LibraryFolder.Text=NuclideLibrary.BundledLibraryPath;LoadNuclideLibrary();
    }
    private void LoadLibraryClick(object sender,RoutedEventArgs e)=>LoadNuclideLibrary();
    private void RefreshNuclidesClick(object sender,RoutedEventArgs e)=>RefreshNuclideSuggestions();
    private void RefreshNuclideSuggestions()
    {
        if(NuclideGrid is null)return;
        ClearConfirmation();NuclideGrid.SelectedItem=null;NuclideGrid.ItemsSource=null;
        if(active is not null&&session is not null)
            foreach(var key in active.ConfirmedLines.Keys.ToArray())
                if(!rows.Any(r=>active.ConfirmedLines[key].IsCurrent(session,r.Result)))active.ConfirmedLines.Remove(key);
        if(nuclideLibrary is null){NuclideSummary.Text=libraryMessage.Length>0?libraryMessage:"Chọn thư mục chứa thư viện NLB của Genie hoặc CSV. Chưa có thư viện nên chưa gợi ý đồng vị.";return;}
        try
        {
            var results=nuclideLibrary.Suggest(rows.Select(r=>r.Result).ToArray(),Number(NuclideTolerance.Text),Number(NuclideFwhm.Text));
            NuclideGrid.ItemsSource=results;
            int usable=rows.Count(r=>r.Result.Success&&r.Result.EnergyKeV is double e&&double.IsFinite(e)&&e>0);
            NuclideSummary.Text=libraryMessage+$" {results.Count:N0} vạch khớp / {usable} ROI có fit và năng lượng hợp lệ. "+(usable==0?"Cần phổ có chuẩn năng lượng và ROI fit thành công.":results.Count==0?"Không có vạch nằm trong dung sai.":"Ưu tiên đồng vị có vạch khớp ở các ROI khác, rồi độ lệch năng lượng; không phải xác suất nhận dạng.");
        }
        catch(CnfException error){NuclideSummary.Text=libraryMessage+" "+error.Message;}
        if(active is not null&&active.ConfirmedLines.Count>0)
            NuclideSummary.Text+=" Đã xác nhận: "+string.Join("; ",active.ConfirmedLines.Values.Select(c=>$"ROI {c.Roi.Start}–{c.Roi.End}: {c.Line.Nuclide}, {c.Line.EnergyKeV:G9} keV, Pγ={c.Line.IntensityPercent?.ToString("G9")??"chưa có"}%"));
    }
    private async void ChooseLibraryClick(object sender,RoutedEventArgs e)
    {
        var folders=await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions{Title="Chọn thư mục thư viện NLB/CSV",AllowMultiple=false});
        if(folders.Count>0&&folders[0].TryGetLocalPath() is string directory){LibraryFolder.Text=directory;LoadNuclideLibrary();}
    }
}
