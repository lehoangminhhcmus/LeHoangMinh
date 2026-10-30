using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using GammaCnf.Core;

namespace GammaCnf.App;
public sealed record RoiRow(int Id,RoiResult Result)
{
    private static string F(double? v)=>v?.ToString("N3")??"—";
    public string Bounds=>$"{Result.Range.Start}–{Result.Range.End}";
    public string Area=>Result.Success?$"{F(Result.PeakArea)} ± {F(Result.AreaError)}":"—";
    public string GrossCounts=>Result.Counts?.Gross.ToString("N0")??"—";
    public string BackgroundCounts=>F(Result.Counts?.ScaledBackground);
    public string NetCounts=>Result.Counts is {} c?$"{F(c.Net)} ± {F(c.NetSigma)}":"—";
    public string Origin=>Result.Origin=="Automatic"?"Tự dò":"Thủ công";
    public string Position=>F(Result.Centroid);
    public string Energy=>F(Result.EnergyKeV);
    public string Width=>F(Result.FwhmChannels);
    public string EnergyWidth=>F(Result.FwhmKeV);
    public string ChiSquare=>F(Result.ReducedChiSquare);
    public string Status=>Result.Status+(Result.Origin=="Automatic"?" • ROI tự dò.":"");
    public string Detail=>Result.Status+"\n"+Result.DetectionNote;
}
public partial class MainWindow : Window
{
    private readonly EfficiencyWorkspace efficiencyWorkspace=new();
    private readonly ObservableCollection<OpenSpectrum> documents=[];
    private OpenSpectrum? active;
    private bool restoringView;
    private bool updatingRows;
    private CancellationTokenSource? peakCancellation;
    private OpenSpectrum? peakDocument;
    private AnalysisSession? session=>active?.Session;
    private string? currentPath {get=>active?.Path;set{if(active is not null&&value is not null)active.Path=value;}}
    private string? backgroundPath {get=>active?.BackgroundPath;set{if(active is not null)active.BackgroundPath=value;}}
    private string? pendingBackgroundPath {get=>active?.PendingBackgroundPath;set{if(active is not null)active.PendingBackgroundPath=value;}}
    private Spectrum? pendingBackground {get=>active?.PendingBackground;set{if(active is not null)active.PendingBackground=value;}}
    private byte[]? originalBytes {get=>active?.OriginalBytes;set{if(active is not null&&value is not null)active.OriginalBytes=value;}}
    private bool calibrationDirty {get=>active?.CalibrationDirty??false;set{if(active is not null)active.CalibrationDirty=value;}}
    private string scaleMode {get=>active?.ScaleMode??"None";set{if(active is not null)active.ScaleMode=value;}}
    private readonly List<RoiRange> emptyRanges=[];
    private List<RoiRange> ranges=>active?.Ranges??emptyRanges;
    private readonly ObservableCollection<RoiRow> rows=[];
    public MainWindow()
    {
        InitializeComponent();RoiGrid.ItemsSource=rows;OpenedSpectra.ItemsSource=documents;
        Plot.HoverChanged+=text=>Status.Text=text;
        Plot.RoiSelected+=(a,b)=>AddRoi(a,b);
        Loaded+=(_,_)=> { var args=Environment.GetCommandLineArgs();if(args.Length>1&&!args[1].StartsWith("--"))foreach(var file in args.Skip(1))LoadFile(file); };
    }
    private static byte[] ReadBytes(string path)
    {
        using var f=File.OpenRead(path);if(f.Length>64*1024*1024)throw new CnfException("File vượt giới hạn 64 MB.");
        var b=new byte[checked((int)f.Length)];f.ReadExactly(b);return b;
    }
    private static double Number(string text)
    {
        if(!double.TryParse(text.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double value)||!double.IsFinite(value))
            throw new CnfException("Nhập số hợp lệ (không dùng dấu phân nhóm hàng nghìn).");
        return value;
    }
    private static string F(double x)=>x.ToString("G12",CultureInfo.InvariantCulture);
    private void Error(Exception e)=>MessageBox.Show(this,e.Message,"Không thực hiện được",MessageBoxButton.OK,MessageBoxImage.Warning);
    private void Execute(Action action)
    {
        try { action(); }
        catch(Exception e) when(e is CnfException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or OverflowException) { Error(e); }
    }
    private bool ConfirmUnsaved()=>!calibrationDirty||MessageBox.Show(this,"Hiệu chuẩn chưa lưu vào CNF. Tiếp tục bỏ thay đổi chưa lưu?","Hiệu chuẩn chưa lưu",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;
    protected override void OnClosing(CancelEventArgs e)
    {
        CaptureCurrentView();
        var dirty=documents.Where(d=>d.CalibrationDirty).Select(d=>Path.GetFileName(d.Path)).ToArray();
        if(dirty.Length>0&&MessageBox.Show(this,"Các phổ chưa lưu hiệu chuẩn: "+string.Join(", ",dirty)+". Vẫn đóng app?","Hiệu chuẩn chưa lưu",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)e.Cancel=true;
        if(!e.Cancel&&efficiencyWorkspace.Dirty&&MessageBox.Show(this,"Điểm/bộ chuẩn hiệu suất chưa lưu JSON và sẽ mất khi đóng app. Vẫn đóng?","Chưa lưu chuẩn hiệu suất",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)e.Cancel=true;
        if(!e.Cancel)peakCancellation?.Cancel();
        base.OnClosing(e);
    }
    internal async Task RunSmokeChecks(string path,string directory)
    {
        Directory.CreateDirectory(directory);
        LoadFile(path);
        if(session is null)throw new Exception("UI did not load the synthetic CNF.");
        A0.Text="1.2";A1.Text="0.51";A2.Text="0";A3.Text="0";ApplyCalibration();
        if(Math.Abs(session.Sample.Energy(100)!.Value-52.2)>1e-9)throw new Exception("Calibration UI failed.");
        if(CalibrationPlot.Coefficients is not {} applied||applied[0]!=1.2)throw new Exception("Energy graph did not update.");
        UpdateLayout();CalibrationPlot.UpdateLayout();
        var calibrationImage=new RenderTargetBitmap(Math.Max(1,(int)CalibrationPlot.ActualWidth),Math.Max(1,(int)CalibrationPlot.ActualHeight),96,96,PixelFormats.Pbgra32);
        calibrationImage.Render(CalibrationPlot);var calibrationEncoder=new PngBitmapEncoder();calibrationEncoder.Frames.Add(BitmapFrame.Create(calibrationImage));
        using(var calibrationFile=File.Create(Path.Combine(directory,"energy-calibration.png")))calibrationEncoder.Save(calibrationFile);

        pendingBackground=session.Sample with {Counts=Enumerable.Repeat(100u,session.Values.Length).ToArray(),LiveTime=500};
        pendingBackgroundPath="synthetic-background.cnf";TimeBasisChoice.SelectedIndex=0;
        ApplyBackgroundClick(this,new RoutedEventArgs());
        if(session.Values[0]>=0||session.Scale!=2)throw new Exception("Background UI failed.");
        AddRoi(270,330);
        if(rows.Count!=1||!rows[0].Result.Success)throw new Exception("ROI UI fit failed.");
        session.ExportCsv(Path.Combine(directory,"net.csv"));
        RoiAnalysis.ExportReport(Path.Combine(directory,"roi.txt"),session,path,backgroundPath,scaleMode,rows.Select(r=>r.Result));
        EnergyCheck.IsChecked=true;LogCheck.IsChecked=true;UpdateLayout();Plot.UpdateLayout();
        var image=new RenderTargetBitmap(Math.Max(1,(int)Plot.ActualWidth),Math.Max(1,(int)Plot.ActualHeight),96,96,PixelFormats.Pbgra32);
        image.Render(Plot);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));
        using(var f=File.Create(Path.Combine(directory,"plot.png")))encoder.Save(f);
        LogCheck.IsChecked=false;ClearBackgroundClick(this,new RoutedEventArgs());
        if(session.Background is not null||rows.Count!=1||!rows[0].Result.Success)throw new Exception("ROI recalculation after clearing background failed.");
        calibrationDirty=false;
        var efficiencyWindow=new EfficiencyWindow(session,rows.Select(r=>r.Result).ToArray(),path,"SYNTHETIC-UI-MEASUREMENT",null,new EfficiencyWorkspace()){Owner=this};
        try{efficiencyWindow.Show();efficiencyWindow.RunSmokeChecks(directory);}finally{efficiencyWindow.Close();}
        var first=active??throw new Exception("No active spectrum in multi-spectrum smoke check.");
        var sharedBackground=session.Sample with{Counts=Enumerable.Repeat(100u,session.Values.Length).ToArray(),LiveTime=500};
        pendingBackground=sharedBackground;pendingBackgroundPath="shared-synthetic-background.cnf";ApplyBackground();
        double firstNet=session.Values[0];A0.Text="first unapplied draft";PeakMinAmplitude.Text="125";first.CalibrationDirty=true;
        LogCheck.IsChecked=true;Plot.RestoreViewport((250,350));
        // New measurement with twice the live time and a different real time.
        byte[] other=(byte[])originalBytes!.Clone();
        for(int i=0;i<28;i++)
        {
            int entry=0x70+48*i;
            if(System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(other.AsSpan(entry,4))!=0x12000)continue;
            int p=(int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(other.AsSpan(entry+10,4));
            int time=p+0x30+System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(other.AsSpan(p+0x24,2));
            System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(other.AsSpan(time+9,8),-2200L*10000000);
            System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(other.AsSpan(time+17,8),-2000L*10000000);
        }
        other=CnfCalibration.Patch(other,first.Session.Sample.Calibration!);
        string otherPath=Path.Combine(directory,"second-spectrum.cnf");File.WriteAllBytes(otherPath,other);LoadFile(otherPath);
        var second=active??throw new Exception("Second spectrum did not open.");
        if(documents.Count!=2||ReferenceEquals(first,second)||ranges.Count!=0||session!.Background is not null)throw new Exception("New spectrum inherited another spectrum's analysis.");
        pendingBackground=sharedBackground;pendingBackgroundPath="shared-synthetic-background.cnf";ApplyBackground();
        if(session.Scale!=4||session.BackgroundTargetSeconds!=2000)throw new Exception("Automatic background time normalization not recomputed for second spectrum.");
        AddRoi(390,410);A0.Text="second unapplied draft";PeakMinAmplitude.Text="250";
        PreviousSpectrumClick(this,new RoutedEventArgs());
        if(active!=first||session!.Scale!=2||session.Values[0]!=firstNet||A0.Text!="first unapplied draft"||PeakMinAmplitude.Text!="125"||!calibrationDirty||ranges.Count!=1||ranges[0].Start!=270||LogCheck.IsChecked!=true||Plot.Viewport!=(250d,350d))throw new Exception("First spectrum state was not restored.");
        NextSpectrumClick(this,new RoutedEventArgs());
        if(active!=second||A0.Text!="second unapplied draft"||PeakMinAmplitude.Text!="250"||ranges.Count!=1||ranges[0].Start!=390||session!.Scale!=4)throw new Exception("Second spectrum state was not restored.");
        LoadFile(otherPath);if(documents.Count!=2)throw new Exception("Opening the same path created a duplicate document.");
        TimeBasisChoice.SelectedIndex=1;
        if(Math.Abs(session!.Scale-2200/sharedBackground.RealTime!.Value)>1e-9||session.TimeBasis!=BackgroundTimeBasis.RealTime)throw new Exception("Changing time basis did not automatically update background.");
        var counts=rows.Single().Result.Counts??throw new Exception("ROI counts missing from UI.");
        if(counts.Gross!=2100||counts.Net>=0||string.IsNullOrWhiteSpace(rows[0].NetCounts)||rows[0].GrossCounts=="—")throw new Exception("ROI sums in multi-spectrum UI failed.");
        CloseDocument(second);if(active!=first||documents.Count!=1||session!.Scale!=2)throw new Exception("Closing a spectrum lost the other document.");
        ClearBackgroundClick(this,new RoutedEventArgs());
        ClearRoisClick(this,new RoutedEventArgs());
        PeakMinAmplitude.Text="1000000000";
        await RunPeakSearchAsync(first,ReadPeakSearchOptions(),true);
        if(ranges.Count!=0||rows.Count!=0)throw new Exception("Minimum amplitude did not prevent low automatic peak fit.");
        PeakMinAmplitude.Text="100,5";
        await RunPeakSearchAsync(first,ReadPeakSearchOptions(),true);
        if(rows.Count!=1||!rows[0].Result.DetectionNote.Contains("amplitude>=100.5"))throw new Exception("Amplitude form parsing/provenance failed.");
        PeakMinAmplitude.Text="0";
        await RunPeakSearchAsync(first,new PeakSearchOptions(),true);
        if(ranges.Count!=1||first.AutomaticRois.Count!=1||!rows.Single().Result.Success||Math.Abs(rows.Single().Result.Centroid!.Value-300.3)>.1)throw new Exception("Automatic peak/ROI UI failed.");
        var automaticRange=ranges[0];
        pendingBackground=sharedBackground;pendingBackgroundPath="shared-synthetic-background.cnf";ApplyBackground();
        if(!rows[0].Result.DetectionNote.Contains("changed since detection"))throw new Exception("Changing background did not mark automatic search provenance stale.");
        ClearBackgroundClick(this,new RoutedEventArgs());
        await RunPeakSearchAsync(first,new PeakSearchOptions(),true);
        if(rows[0].Result.DetectionNote.Contains("changed since detection"))throw new Exception("New automatic search retained stale provenance.");
        if(ranges.Count!=1||ranges[0]!=automaticRange)throw new Exception("Repeated automatic detection duplicated ROI.");
        RoiGrid.SelectedItem=rows[0];RoiStart.Text=(automaticRange.Start-1).ToString();RoiEnd.Text=(automaticRange.End+1).ToString();EditRoiClick(this,new RoutedEventArgs());
        if(first.AutomaticRois.Count!=0||rows[0].Result.Origin!="Manual")throw new Exception("Editing automatic ROI did not create a retained manual ROI.");
        var manualRange=ranges[0];await RunPeakSearchAsync(first,new PeakSearchOptions(),true);
        if(ranges.Count!=1||ranges[0]!=manualRange||first.AutomaticRois.Count!=0)throw new Exception("Automatic detection overwrote existing manual ROI.");
        ClearRoisClick(this,new RoutedEventArgs());
        var pending=RunPeakSearchAsync(first,new PeakSearchOptions(),true);CancelPeakClick(this,new RoutedEventArgs());await pending;
        if(ranges.Count!=0)throw new Exception("Cancelled peak search changed ROI.");
        await RunPeakSearchAsync(first,new PeakSearchOptions(),true);
        RoiAnalysis.ExportReport(Path.Combine(directory,"automatic-roi.txt"),session!,path,null,"None",rows.Select(r=>r.Result));
        UpdateLayout();var automaticImage=new RenderTargetBitmap(Math.Max(1,(int)Plot.ActualWidth),Math.Max(1,(int)Plot.ActualHeight),96,96,PixelFormats.Pbgra32);automaticImage.Render(Plot);
        var automaticEncoder=new PngBitmapEncoder();automaticEncoder.Frames.Add(BitmapFrame.Create(automaticImage));using(var f=File.Create(Path.Combine(directory,"automatic-peaks.png")))automaticEncoder.Save(f);
        File.WriteAllText(Path.Combine(directory,"automatic-peak-success.txt"),"PASS: asynchronous automatic search, Gaussian ROI, repeated search, manual ROI edit/preservation, cancellation, provenance TXT");
        first.CalibrationDirty=false;CloseDocument(first);if(active is not null||documents.Count!=0||Plot.Spectrum is not null)throw new Exception("Closing final spectrum did not clear the UI.");
        File.WriteAllText(Path.Combine(directory,"multi-spectrum-success.txt"),"PASS: multi-open/deduplicate/previous/next/close; per-spectrum ROI, calibration drafts, background/time scale, log view and zoom; direct ROI totals");
    }
    private void EfficiencyClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(session is null||currentPath is null||originalBytes is null)throw new CnfException("Mở phổ, tạo ROI và fit trước khi thiết lập hiệu suất.");
        string id=MeasurementIdentity.Create(session.Sample);
        var window=new EfficiencyWindow(session,rows.Select(r=>r.Result).ToArray(),currentPath,id,backgroundPath,efficiencyWorkspace){Owner=this};
        window.ShowDialog();
    });
    private void OpenClick(object sender,RoutedEventArgs e)
    {
        var d=new OpenFileDialog { Filter="Canberra CNF (*.cnf)|*.cnf|Tất cả file (*.*)|*.*",Multiselect=true };
        if(d.ShowDialog(this)==true)foreach(var file in d.FileNames)LoadFile(file);
    }
    private void LoadFile(string path)=>Execute(()=>
    {
        string full=Path.GetFullPath(path);
        var existing=documents.FirstOrDefault(d=>string.Equals(d.Path,full,StringComparison.OrdinalIgnoreCase));
        if(existing is not null){Activate(existing);Status.Text="Đã chuyển tới phổ đang mở: "+full;return;}
        if(documents.Count>=32)throw new CnfException("Tối đa 32 phổ đang mở. Đóng bớt phổ trước khi mở thêm.");
        var bytes=ReadBytes(full);var sample=CnfReader.Decode(bytes);var document=new OpenSpectrum(full,bytes,sample);
        documents.Add(document);Activate(document);Status.Text=$"Đang mở {documents.Count} phổ. Dùng Phổ trước/Phổ sau hoặc danh sách để chuyển.";
    });
    private void CaptureCurrentView()
    {
        if(active is null)return;var view=active.View;
        view.Coefficients=[A0.Text,A1.Text,A2.Text,A3.Text];view.Energy=EnergyCheck.IsChecked==true;view.Log=LogCheck.IsChecked==true;
        view.TimeBasis=TimeBasisChoice.SelectedIndex;view.AllowChannelOnly=AllowChannelOnly.IsChecked==true;
        view.RoiStart=RoiStart.Text;view.RoiEnd=RoiEnd.Text;view.PeakMinimumFwhm=PeakMinWidth.Text;view.PeakMaximumFwhm=PeakMaxWidth.Text;view.PeakMinimumAmplitude=PeakMinAmplitude.Text;view.PeakThreshold=PeakThreshold.Text;view.PeakPadding=PeakPadding.Text;view.ReplaceAutomatic=ReplaceAutomatic.IsChecked==true;view.Viewport=Plot.Viewport;view.SelectedRoi=(RoiGrid.SelectedItem as RoiRow)?.Result.Range;
    }
    private void Activate(OpenSpectrum document)
    {
        if(ReferenceEquals(active,document))return;
        CaptureCurrentView();active=document;restoringView=true;
        try
        {
            var view=document.View;SetCoefficientFields();
            if(view.Coefficients.Length==4){A0.Text=view.Coefficients[0];A1.Text=view.Coefficients[1];A2.Text=view.Coefficients[2];A3.Text=view.Coefficients[3];}
            EnergyCheck.IsChecked=view.Energy;LogCheck.IsChecked=view.Log;TimeBasisChoice.SelectedIndex=view.TimeBasis;AllowChannelOnly.IsChecked=view.AllowChannelOnly;
            RoiStart.Text=view.RoiStart;RoiEnd.Text=view.RoiEnd;PeakMinWidth.Text=view.PeakMinimumFwhm;PeakMaxWidth.Text=view.PeakMaximumFwhm;PeakMinAmplitude.Text=view.PeakMinimumAmplitude;PeakThreshold.Text=view.PeakThreshold;PeakPadding.Text=view.PeakPadding;ReplaceAutomatic.IsChecked=view.ReplaceAutomatic;
            PendingBackgroundLabel.Text=pendingBackgroundPath is null?"Chưa chọn file":Path.GetFileName(pendingBackgroundPath)+$" • live: {pendingBackground?.LiveTime?.ToString("N3")??"NA"} s • real: {pendingBackground?.RealTime?.ToString("N3")??"NA"} s";
            ScaleText.Text=session?.Background is null?"":F(session.Scale);
            CsvButton.IsEnabled=PngButton.IsEnabled=true;Refresh();
            if(view.Viewport is {} viewport)Plot.RestoreViewport(viewport);else Plot.Reset();
            if(view.SelectedRoi is {} roi)RoiGrid.SelectedItem=rows.FirstOrDefault(r=>r.Result.Range==roi);
            OpenedSpectra.SelectedItem=document;UpdateDocumentButtons();
        }
        finally{restoringView=false;}
    }
    private void UpdateDocumentButtons()
    {PreviousButton.IsEnabled=NextButton.IsEnabled=documents.Count>1;CloseSpectrumButton.IsEnabled=documents.Count>0;}
    private void OpenedSpectrumChanged(object sender,SelectionChangedEventArgs e)
    {if(!restoringView&&OpenedSpectra.SelectedItem is OpenSpectrum document)Activate(document);}
    private void StepSpectrum(int offset)
    {if(documents.Count>1&&active is not null)Activate(documents[(documents.IndexOf(active)+offset+documents.Count)%documents.Count]);}
    private void PreviousSpectrumClick(object sender,RoutedEventArgs e)=>StepSpectrum(-1);
    private void NextSpectrumClick(object sender,RoutedEventArgs e)=>StepSpectrum(1);
    private void CloseSpectrumClick(object sender,RoutedEventArgs e)
    {
        if(active is null||!ConfirmUnsaved())return;
        if(ranges.Count>0&&MessageBox.Show(this,"Đóng phổ sẽ bỏ các ROI trong phiên của phổ này. Tiếp tục?","Đóng phổ",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
        CloseDocument(active);
    }
    private void CloseDocument(OpenSpectrum document)
    {
        if(ReferenceEquals(document,peakDocument))peakCancellation?.Cancel();
        int index=documents.IndexOf(document);restoringView=true;
        try{if(ReferenceEquals(active,document))active=null;documents.Remove(document);OpenedSpectra.SelectedItem=null;}
        finally{restoringView=false;}
        if(documents.Count>0)Activate(documents[Math.Clamp(index,0,documents.Count-1)]);
        else
        {
            rows.Clear();Plot.Spectrum=null;Plot.Values=null;Plot.Rois=[];Plot.DetectedPeaks=[];Plot.Reset();
            CalibrationPlot.Coefficients=null;CalibrationPlot.InvalidateVisual();
            FileLabel.Text="Chưa mở phổ";Metadata.Text="Chọn hoặc kéo thả một hay nhiều file CNF.";CalibrationLabel.Text="";BackgroundLabel.Text="Chưa trừ phông";
            PendingBackgroundLabel.Text="Chưa chọn file";ScaleText.Text="";Warnings.Text="";CsvButton.IsEnabled=PngButton.IsEnabled=EnergyCheck.IsEnabled=false;
            Title="Gamma CNF Reader 0.9.1";Status.Text="Đã đóng tất cả phổ.";UpdateDocumentButtons();
        }
    }
    private void SetCoefficientFields()
    {
        var c=session?.Sample.Calibration??[0,1,0,0];
        A0.Text=F(c[0]);A1.Text=F(c[1]);A2.Text=F(c[2]);A3.Text=F(c[3]);
    }
    private void Refresh(bool reset=false)
    {
        if(session is null)return;var s=session.Sample;
        FileLabel.Text=$"{Path.GetFileName(currentPath)}   |   {s.Title}";
        Title=$"{Path.GetFileName(currentPath)} — Gamma CNF Reader 0.9.1";
        string Sec(double? t)=>t is double v?$"{v:N3} s":"Không có";
        Metadata.Text=$"{s.Counts.Length:N0} kênh   •   {s.Total:N0} số đếm gốc   •   Bắt đầu (theo file): {s.Started?.ToString("dd/MM/yyyy HH:mm:ss")??"Không có"}\nThời gian thực: {Sec(s.RealTime)}   •   Thời gian sống: {Sec(s.LiveTime)}";
        if(s.RealTime>0&&s.LiveTime.HasValue&&s.LiveTime<=s.RealTime)Metadata.Text+=$"   •   Thời gian chết: {(1-s.LiveTime.Value/s.RealTime.Value)*100:F2}%";
        CalibrationLabel.Text=(calibrationDirty?"[Chưa lưu CNF] ":"")+(s.Calibration is null?"Chỉ hiển thị kênh.":"E(ch), keV: "+string.Join("   ",s.Calibration.Select((c,i)=>$"a{i} = {c:G9}")));
        BackgroundLabel.Text=session.Background is null?"Chưa trừ phông":$"Phông: {Path.GetFileName(backgroundPath)}   •   {session.BackgroundOriginalSeconds:N3} → {session.BackgroundTargetSeconds:N3} s   •   α = {session.Scale:G9} ({scaleMode})   •   Tổng net: {session.Values.Sum():N3}";
        CalibrationPlot.Coefficients=s.Calibration;CalibrationPlot.Channels=s.Counts.Length;CalibrationPlot.InvalidateVisual();
        EnergyCheck.IsEnabled=s.Calibration is not null;
        Plot.Spectrum=s;Plot.Values=session.Values;Plot.EnergyAxis=EnergyCheck.IsChecked==true;Plot.LogScale=LogCheck.IsChecked==true;
        Refit();if(reset)Plot.Reset();else Plot.InvalidateVisual();
        Warnings.Text=string.Join(" • ",s.Warnings);
    }
    private void Refit()
    {
        if(session is null)return;
        var selected=(RoiGrid.SelectedItem as RoiRow)?.Result.Range;
        updatingRows=true;
        try
        {
            rows.Clear();int id=0;
            foreach(var range in ranges)
            {
                var result=active!.AutomaticRois.TryGetValue(range,out var automatic)?PeakSearch.Analyze(session,automatic):RoiAnalysis.Fit(session,range);
                if(result.Origin=="Automatic"&&(!active.AutomaticSearchRevisions.TryGetValue(range,out long searchedRevision)||searchedRevision!=session.Revision))result=result with{Status=result.Status+" • Phông/hiệu chuẩn đã đổi, hãy dò lại.",DetectionNote=result.DetectionNote+"; spectrum/calibration/background changed since detection; re-run search"};
                rows.Add(new RoiRow(++id,result));
            }
            if(selected is not null)RoiGrid.SelectedItem=rows.FirstOrDefault(r=>r.Result.Range==selected);
        }
        finally{updatingRows=false;}
        Plot.Rois=rows.Select(r=>r.Result).ToArray();Plot.DetectedPeaks=active!.AutomaticRois.Values.SelectMany(r=>r.Peaks).ToArray();Plot.InvalidateVisual();
    }
    private PeakSearchOptions ReadPeakSearchOptions()=>new PeakSearchOptions(Number(PeakMinWidth.Text),Number(PeakMaxWidth.Text),Number(PeakThreshold.Text),Number(PeakPadding.Text),MinimumAmplitude:Number(PeakMinAmplitude.Text));
    private async void AutoPeakClick(object sender,RoutedEventArgs e)
    {
        try
        {
            if(active is null)throw new CnfException("Mở phổ trước khi dò đỉnh.");
            var options=ReadPeakSearchOptions();options.Validate();
            await RunPeakSearchAsync(active,options,ReplaceAutomatic.IsChecked==true);
        }
        catch(Exception error)when(error is CnfException or ArgumentException or OverflowException){Error(error);}
    }
    private async Task RunPeakSearchAsync(OpenSpectrum document,PeakSearchOptions options,bool replace)
    {
        if(peakCancellation is not null)throw new CnfException("Đang dò đỉnh; hãy đợi hoặc bấm Hủy dò.");
        options.Validate();var revision=document.Session.Values;
        var values=(double[])revision.Clone();var variances=(double[])document.Session.Variances.Clone();
        using var cancellation=new CancellationTokenSource();peakCancellation=cancellation;peakDocument=document;
        PeakSearchButton.IsEnabled=false;CancelPeakButton.IsEnabled=true;Status.Text="Đang dò đỉnh toàn phổ: "+Path.GetFileName(document.Path)+"…";
        try
        {
            var result=await Task.Run(()=>PeakSearch.Find(values,variances,options,cancellation.Token),cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            if(!documents.Contains(document))return;
            if(!ReferenceEquals(revision,document.Session.Values)){Status.Text="Phổ/hiệu chuẩn/phông đã thay đổi trong lúc dò; đã bỏ kết quả cũ. Hãy dò lại.";return;}
            var merge=ApplyAutomaticRois(document,result,replace);
            if(ReferenceEquals(active,document))Refit();
            Status.Text=$"{Path.GetFileName(document.Path)}: {result.Peaks.Count} ứng viên, thêm {merge.Accepted.Count} ROI; bỏ {merge.SkippedOverlap} ROI chồng vùng đang có, {merge.SkippedCapacity} vì đủ 100 ROI."+
                (result.Truncated?" Đã chạm giới hạn 100 ứng viên.":"")+$" Amplitude tối thiểu: {options.MinimumAmplitude:G6} counts/kênh trên nền. Kiểm tra nhóm đỉnh, nền và giới hạn ROI trước khi định lượng.";
        }
        catch(OperationCanceledException){Status.Text="Đã hủy dò đỉnh; ROI trước đó được giữ nguyên.";}
        finally{peakCancellation=null;peakDocument=null;PeakSearchButton.IsEnabled=true;CancelPeakButton.IsEnabled=false;}
    }
    private static RoiMergeResult ApplyAutomaticRois(OpenSpectrum document,PeakSearchResult result,bool replace)
    {
        if(replace)
        {
            foreach(var range in document.AutomaticRois.Keys.ToArray())document.Ranges.Remove(range);
            document.AutomaticRois.Clear();document.AutomaticSearchRevisions.Clear();
        }
        var merge=PeakSearch.Merge(result.Rois,document.Ranges);
        foreach(var roi in merge.Accepted){document.Ranges.Add(roi.Range);document.AutomaticRois[roi.Range]=roi;document.AutomaticSearchRevisions[roi.Range]=document.Session.Revision;}
        return merge;
    }
    private void CancelPeakClick(object sender,RoutedEventArgs e)=>peakCancellation?.Cancel();
    private void SelectedRoiChanged(object sender,SelectionChangedEventArgs e)
    {
        if(!restoringView&&!updatingRows&&RoiGrid.SelectedItem is RoiRow row){RoiStart.Text=row.Result.Range.Start.ToString();RoiEnd.Text=row.Result.Range.End.ToString();}
    }
    private void EditRoiClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(session is null||active is null||RoiGrid.SelectedItem is not RoiRow row)throw new CnfException("Chọn một ROI trong bảng trước.");
        if(!int.TryParse(RoiStart.Text,out int start)||!int.TryParse(RoiEnd.Text,out int end)||start<0||end<start||end>=session.Values.Length)throw new CnfException("Giới hạn ROI không hợp lệ.");
        var next=new RoiRange(start,end);if(ranges.Any(r=>r!=row.Result.Range&&r==next))throw new CnfException("ROI này đã tồn tại.");
        int index=ranges.IndexOf(row.Result.Range);ranges[index]=next;active.AutomaticRois.Remove(row.Result.Range);active.AutomaticSearchRevisions.Remove(row.Result.Range);Refit();RoiGrid.SelectedItem=rows.FirstOrDefault(r=>r.Result.Range==next);
        Status.Text="Đã chỉnh ROI thủ công và tính lại. Dò lại sẽ giữ ROI này.";
    });
    private void ApplyCalibrationClick(object sender,RoutedEventArgs e)=>Execute(ApplyCalibration);
    private void ApplyCalibration()
    {
        if(session is null)throw new CnfException("Mở phổ mẫu trước.");
        session.SetCalibration([Number(A0.Text),Number(A1.Text),Number(A2.Text),Number(A3.Text)]);
        calibrationDirty=true;EnergyCheck.IsChecked=true;Refresh();Status.Text="Đã áp dụng đường chuẩn và tính lại các ROI. Chưa ghi vào CNF.";
    }
    private void SaveCnfClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(session is null||originalBytes is null||currentPath is null)throw new CnfException("Mở phổ mẫu trước.");
        ApplyCalibration();
        // Validate writability before asking the user to choose a destination.
        CnfCalibration.Patch(originalBytes,session.Sample.Calibration!);
        var d=new SaveFileDialog { Filter="Canberra CNF (*.cnf)|*.cnf",FileName=Path.GetFileName(currentPath),InitialDirectory=Path.GetDirectoryName(currentPath),OverwritePrompt=true };
        if(d.ShowDialog(this)!=true)return;
        bool same=string.Equals(Path.GetFullPath(d.FileName),currentPath,StringComparison.OrdinalIgnoreCase);
        string warning=(same?"Ghi chồng hiệu chuẩn lên CNF gốc; app sẽ tạo file .bak. ":"Lưu CNF với hiệu chuẩn mới. ")+"Số đếm không đổi. Các kết quả phân tích cũ trong CNF không được tính lại và có thể không còn phù hợp. Tiếp tục?";
        if(MessageBox.Show(this,warning,"Lưu hiệu chuẩn CNF",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
        if(documents.Any(other=>!ReferenceEquals(other,active)&&string.Equals(other.Path,Path.GetFullPath(d.FileName),StringComparison.OrdinalIgnoreCase)))throw new CnfException("Đích đang là một phổ khác được mở. Chọn tên mới hoặc đóng phổ đích trước.");
        string? backup=CnfCalibration.Save(d.FileName,originalBytes,session.Sample.Calibration!,same);
        originalBytes=ReadBytes(d.FileName);currentPath=Path.GetFullPath(d.FileName);
        session.SetCalibration(CnfReader.Decode(originalBytes).Calibration!);calibrationDirty=false;SetCoefficientFields();Refresh();
        Status.Text="Đã lưu CNF; "+(backup is null?"file mới.":"bản sao lưu: "+backup);
    });
    private BackgroundTimeBasis SelectedTimeBasis=>TimeBasisChoice.SelectedIndex==1?BackgroundTimeBasis.RealTime:BackgroundTimeBasis.LiveTime;
    private void BackgroundClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(session is null)throw new CnfException("Mở phổ mẫu trước.");
        var d=new OpenFileDialog { Filter="Canberra CNF (*.cnf)|*.cnf" };
        if(d.ShowDialog(this)!=true)return;
        if(string.Equals(Path.GetFullPath(d.FileName),currentPath,StringComparison.OrdinalIgnoreCase))throw new CnfException("Chọn file phông là phép đo độc lập với phổ mẫu.");
        var bg=CnfReader.Read(d.FileName);
        if(MeasurementIdentity.Create(bg)==MeasurementIdentity.Create(session.Sample))throw new CnfException("Phổ phông trùng phép đo mẫu; cần phép đo phông độc lập.");
        pendingBackground=bg;pendingBackgroundPath=d.FileName;
        PendingBackgroundLabel.Text=Path.GetFileName(d.FileName)+$" • live: {bg.LiveTime?.ToString("N3")??"NA"} s • real: {bg.RealTime?.ToString("N3")??"NA"} s";
        Status.Text="Đã chọn phông. Nếu chưa áp dụng được, kiểm tra thời gian/hiệu chuẩn và bấm Áp dụng trừ phông.";
        ApplyBackground();
    });
    private void ApplyBackground()
    {
        if(session is null||pendingBackground is null)throw new CnfException("Chọn phổ mẫu và phổ phông trước.");
        session.SetTimedBackground(pendingBackground,SelectedTimeBasis,AllowChannelOnly.IsChecked==true);
        backgroundPath=pendingBackgroundPath;scaleMode=SelectedTimeBasis==BackgroundTimeBasis.LiveTime?"Automatic live-time ratio":"Automatic real-time ratio";
        ScaleText.Text=F(session.Scale);Refresh();
        Status.Text=$"Đã tự scale phông từ {session.BackgroundOriginalSeconds:N3} s về {session.BackgroundTargetSeconds:N3} s (α={session.Scale:G9}); không sửa CNF phông gốc.";
    }
    private void ApplyBackgroundClick(object sender,RoutedEventArgs e)=>Execute(ApplyBackground);
    private void BackgroundTimeChanged(object sender,SelectionChangedEventArgs e)
    {if(!restoringView&&session is not null&&pendingBackground is not null)Execute(ApplyBackground);}
    private void ClearBackgroundClick(object sender,RoutedEventArgs e)
    { if(session is null)return;session.ClearBackground();backgroundPath=null;pendingBackground=null;pendingBackgroundPath=null;scaleMode="None";ScaleText.Text="";PendingBackgroundLabel.Text="Chưa chọn file";Refresh();Status.Text="Đã bỏ trừ phông và tính lại ROI."; }
    private void AddRoi(int start,int end)=>Execute(()=>
    {
        if(session is null)throw new CnfException("Mở phổ trước.");
        if(start<0||end<start||end>=session.Values.Length)throw new CnfException("ROI cần nằm trong phổ, kênh đầu ≤ kênh cuối.");
        if(ranges.Count>=100)throw new CnfException("Giới hạn 100 ROI mỗi phiên.");
        if(ranges.Contains(new RoiRange(start,end)))throw new CnfException("ROI này đã tồn tại.");
        ranges.Add(new(start,end));RoiStart.Text=start.ToString();RoiEnd.Text=end.ToString();Refit();Status.Text=$"Đã thêm ROI {ranges.Count}.";
    });
    private void AddRoiClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(!int.TryParse(RoiStart.Text,out int a)||!int.TryParse(RoiEnd.Text,out int b))throw new CnfException("Giới hạn ROI phải là số kênh nguyên.");
        AddRoi(a,b);
    });
    private void RemoveRoiClick(object sender,RoutedEventArgs e)
    { if(RoiGrid.SelectedItem is RoiRow row){ranges.Remove(row.Result.Range);active?.AutomaticRois.Remove(row.Result.Range);active?.AutomaticSearchRevisions.Remove(row.Result.Range);Refit();} }
    private void ClearRoisClick(object sender,RoutedEventArgs e) { if(ReferenceEquals(active,peakDocument))peakCancellation?.Cancel();ranges.Clear();active?.AutomaticRois.Clear();active?.AutomaticSearchRevisions.Clear();rows.Clear();Plot.Rois=[];Plot.DetectedPeaks=[];Plot.InvalidateVisual(); }
    private void ExportRoisClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(session is null||rows.Count==0)throw new CnfException("Thêm ROI trước khi xuất.");
        var d=new SaveFileDialog { Filter="Text (*.txt)|*.txt",FileName=Path.GetFileNameWithoutExtension(currentPath)+"-ROI.txt" };
        if(d.ShowDialog(this)!=true)return;
        RoiAnalysis.ExportReport(d.FileName,session,currentPath!,backgroundPath,scaleMode,rows.Select(r=>r.Result));Status.Text="Đã xuất tất cả ROI: "+d.FileName;
    });
    private void CsvClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(session is null)return;
        var d=new SaveFileDialog { Filter="CSV (*.csv)|*.csv",FileName=Path.GetFileNameWithoutExtension(currentPath)+".csv" };
        if(d.ShowDialog(this)==true){session.ExportCsv(d.FileName);Status.Text="Đã lưu: "+d.FileName;}
    });
    private void PngClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(session is null)return;
        var d=new SaveFileDialog { Filter="PNG (*.png)|*.png",FileName=Path.GetFileNameWithoutExtension(currentPath)+".png" };
        if(d.ShowDialog(this)!=true)return;
        var bitmap=new RenderTargetBitmap(Math.Max(1,(int)Plot.ActualWidth*2),Math.Max(1,(int)Plot.ActualHeight*2),192,192,PixelFormats.Pbgra32);
        bitmap.Render(Plot);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output=File.Create(d.FileName);encoder.Save(output);Status.Text="Đã lưu: "+d.FileName;
    });
    private void ModeChanged(object sender,RoutedEventArgs e)
    { if(Plot is null||restoringView)return;Plot.EnergyAxis=EnergyCheck.IsChecked==true;Plot.LogScale=LogCheck.IsChecked==true;Plot.Reset(); }
    private void ResetClick(object sender,RoutedEventArgs e)=>Plot.Reset();
    private void OnDragOver(object sender,DragEventArgs e)
    {e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;}
    private void OnDrop(object sender,DragEventArgs e)
    {if(e.Data.GetData(DataFormats.FileDrop) is string[] {Length:>0} paths)foreach(var file in paths)LoadFile(file);}
    private void OnKeyDown(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.O&&Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){OpenClick(sender,e);e.Handled=true;}
        if(e.Key==Key.Tab&&Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){StepSpectrum(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)?-1:1);e.Handled=true;}
        if(e.Key==Key.Home){Plot.Reset();e.Handled=true;}
    }
}
