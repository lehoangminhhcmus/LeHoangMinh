using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
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
    public string Position=>F(Result.Centroid);
    public string Energy=>F(Result.EnergyKeV);
    public string Width=>F(Result.FwhmChannels);
    public string EnergyWidth=>F(Result.FwhmKeV);
    public string ChiSquare=>F(Result.ReducedChiSquare);
    public string Status=>Result.Status;
}
public partial class MainWindow : Window
{
    private readonly EfficiencyWorkspace efficiencyWorkspace=new();
    private AnalysisSession? session;
    private string? currentPath,backgroundPath,pendingBackgroundPath;
    private Spectrum? pendingBackground;
    private byte[]? originalBytes;
    private bool calibrationDirty;
    private string scaleMode="None";
    private readonly List<RoiRange> ranges=[];
    private readonly ObservableCollection<RoiRow> rows=[];
    public MainWindow()
    {
        InitializeComponent();RoiGrid.ItemsSource=rows;
        Plot.HoverChanged+=text=>Status.Text=text;
        Plot.RoiSelected+=(a,b)=>AddRoi(a,b);
        Loaded+=(_,_)=> { var args=Environment.GetCommandLineArgs();if(args.Length>1&&!args[1].StartsWith("--"))LoadFile(args[1]); };
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
        if(!ConfirmUnsaved())e.Cancel=true;
        if(!e.Cancel&&efficiencyWorkspace.Dirty&&MessageBox.Show(this,"Điểm/bộ chuẩn hiệu suất chưa lưu JSON và sẽ mất khi đóng app. Vẫn đóng?","Chưa lưu chuẩn hiệu suất",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)e.Cancel=true;
        base.OnClosing(e);
    }
    internal void RunSmokeChecks(string path,string directory)
    {
        Directory.CreateDirectory(directory);
        LoadFile(path);
        if(session is null)throw new Exception("UI did not load the synthetic CNF.");
        A0.Text="1.2";A1.Text="0.51";A2.Text="0";A3.Text="0";ApplyCalibration();
        if(Math.Abs(session.Sample.Energy(100)!.Value-52.2)>1e-9)throw new Exception("Calibration UI failed.");
        pendingBackground=session.Sample with {Counts=Enumerable.Repeat(100u,session.Values.Length).ToArray(),LiveTime=500};
        pendingBackgroundPath="synthetic-background.cnf";UseLiveScale.IsChecked=true;
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
        var d=new OpenFileDialog { Filter="Canberra CNF (*.cnf)|*.cnf|Tất cả file (*.*)|*.*" };
        if(d.ShowDialog(this)==true)LoadFile(d.FileName);
    }
    private void LoadFile(string path)=>Execute(()=>
    {
        var bytes=ReadBytes(path);var sample=CnfReader.Decode(bytes);
        if(!ConfirmUnsaved())return;
        session=new AnalysisSession(sample);originalBytes=bytes;currentPath=Path.GetFullPath(path);
        pendingBackground=null;pendingBackgroundPath=backgroundPath=null;calibrationDirty=false;scaleMode="None";
        ranges.Clear();rows.Clear();PendingBackgroundLabel.Text="Chưa chọn file";
        SetCoefficientFields();EnergyCheck.IsChecked=sample.Calibration is not null;
        CsvButton.IsEnabled=PngButton.IsEnabled=true;Refresh(true);
    });
    private void SetCoefficientFields()
    {
        var c=session?.Sample.Calibration??[0,1,0,0];
        A0.Text=F(c[0]);A1.Text=F(c[1]);A2.Text=F(c[2]);A3.Text=F(c[3]);
    }
    private void Refresh(bool reset=false)
    {
        if(session is null)return;var s=session.Sample;
        FileLabel.Text=$"{Path.GetFileName(currentPath)}   |   {s.Title}";
        Title=$"{Path.GetFileName(currentPath)} — Gamma CNF Reader 0.4.0";
        string Sec(double? t)=>t is double v?$"{v:N3} s":"Không có";
        Metadata.Text=$"{s.Counts.Length:N0} kênh   •   {s.Total:N0} số đếm gốc   •   Bắt đầu (theo file): {s.Started?.ToString("dd/MM/yyyy HH:mm:ss")??"Không có"}\nThời gian thực: {Sec(s.RealTime)}   •   Thời gian sống: {Sec(s.LiveTime)}";
        if(s.RealTime>0&&s.LiveTime.HasValue&&s.LiveTime<=s.RealTime)Metadata.Text+=$"   •   Thời gian chết: {(1-s.LiveTime.Value/s.RealTime.Value)*100:F2}%";
        CalibrationLabel.Text=(calibrationDirty?"[Chưa lưu CNF] ":"")+(s.Calibration is null?"Chỉ hiển thị kênh.":"E(ch), keV: "+string.Join("   ",s.Calibration.Select((c,i)=>$"a{i} = {c:G9}")));
        BackgroundLabel.Text=session.Background is null?"Chưa trừ phông":$"Phông: {Path.GetFileName(backgroundPath)}   •   α = {session.Scale:G9} ({scaleMode})   •   Tổng net: {session.Values.Sum():N3}";
        EnergyCheck.IsEnabled=s.Calibration is not null;
        Plot.Spectrum=s;Plot.Values=session.Values;Plot.EnergyAxis=EnergyCheck.IsChecked==true;Plot.LogScale=LogCheck.IsChecked==true;
        Refit();if(reset)Plot.Reset();else Plot.InvalidateVisual();
        Warnings.Text=string.Join(" • ",s.Warnings);
    }
    private void Refit()
    {
        if(session is null)return;
        rows.Clear();int id=0;
        foreach(var range in ranges)rows.Add(new RoiRow(++id,RoiAnalysis.Fit(session,range)));
        Plot.Rois=rows.Select(r=>r.Result).ToArray();Plot.InvalidateVisual();
    }
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
        string? backup=CnfCalibration.Save(d.FileName,originalBytes,session.Sample.Calibration!,same);
        originalBytes=ReadBytes(d.FileName);currentPath=Path.GetFullPath(d.FileName);
        session.SetCalibration(CnfReader.Decode(originalBytes).Calibration!);calibrationDirty=false;SetCoefficientFields();Refresh();
        Status.Text="Đã lưu CNF; "+(backup is null?"file mới.":"bản sao lưu: "+backup);
    });
    private void BackgroundClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(session is null)throw new CnfException("Mở phổ mẫu trước.");
        var d=new OpenFileDialog { Filter="Canberra CNF (*.cnf)|*.cnf" };
        if(d.ShowDialog(this)!=true)return;
        if(string.Equals(Path.GetFullPath(d.FileName),currentPath,StringComparison.OrdinalIgnoreCase))throw new CnfException("Chọn file phông là phép đo độc lập với phổ mẫu.");
        var bg=CnfReader.Read(d.FileName);pendingBackground=bg;pendingBackgroundPath=d.FileName;
        PendingBackgroundLabel.Text=Path.GetFileName(d.FileName)+$" • live: {bg.LiveTime?.ToString("N3")??"NA"} s";
        if(session.Sample.LiveTime>0&&bg.LiveTime>0)ScaleText.Text=F(AnalysisSession.LiveTimeScale(session.Sample,bg));
        else { UseLiveScale.IsChecked=false;Status.Text="Thiếu live time. Nhập α thủ công và bấm Áp dụng trừ phông."; }
    });
    private void ApplyBackgroundClick(object sender,RoutedEventArgs e)=>Execute(()=>
    {
        if(session is null||pendingBackground is null)throw new CnfException("Chọn phổ mẫu và phổ phông trước.");
        double scale=UseLiveScale.IsChecked==true?AnalysisSession.LiveTimeScale(session.Sample,pendingBackground):Number(ScaleText.Text);
        session.SetBackground(pendingBackground,scale,AllowChannelOnly.IsChecked==true);
        backgroundPath=pendingBackgroundPath;scaleMode=UseLiveScale.IsChecked==true?"Live-time ratio":"Manual";
        ScaleText.Text=F(scale);Refresh();Status.Text="Đã trừ phông và tính lại các ROI. Số đếm âm được giữ nguyên.";
    });
    private void ClearBackgroundClick(object sender,RoutedEventArgs e)
    { if(session is null)return;session.ClearBackground();backgroundPath=null;scaleMode="None";Refresh();Status.Text="Đã bỏ trừ phông và tính lại ROI."; }
    private void AddRoi(int start,int end)=>Execute(()=>
    {
        if(session is null)throw new CnfException("Mở phổ trước.");
        if(start<0||end>=session.Values.Length||end-start<8||end-start>4095)throw new CnfException("ROI cần 9–4096 kênh và nằm trong phổ.");
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
    { if(RoiGrid.SelectedItem is RoiRow row){ranges.Remove(row.Result.Range);Refit();} }
    private void ClearRoisClick(object sender,RoutedEventArgs e) { ranges.Clear();rows.Clear();Plot.Rois=[];Plot.InvalidateVisual(); }
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
    { if(Plot is null)return;Plot.EnergyAxis=EnergyCheck.IsChecked==true;Plot.LogScale=LogCheck.IsChecked==true;Plot.Reset(); }
    private void ResetClick(object sender,RoutedEventArgs e)=>Plot.Reset();
    private void OnDragOver(object sender,DragEventArgs e)
    {e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;}
    private void OnDrop(object sender,DragEventArgs e)
    {if(e.Data.GetData(DataFormats.FileDrop) is string[] {Length:>0} paths)LoadFile(paths[0]);}
    private void OnKeyDown(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.O&&Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){OpenClick(sender,e);e.Handled=true;}
        if(e.Key==Key.Home){Plot.Reset();e.Handled=true;}
    }
}
