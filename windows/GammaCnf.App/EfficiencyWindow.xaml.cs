using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GammaCnf.Core;
using Microsoft.Win32;

namespace GammaCnf.App;
public sealed class EfficiencyWorkspace
{
    public List<EfficiencyObservation> Points {get;}=[];
    public EfficiencyProfile? Profile {get;set;}
    public MeasurementGeometry? Geometry {get;set;}
    public string Name {get;set;}="";
    public int Degree {get;set;}=2;
    public DateTimeOffset CreatedAt {get;set;}=DateTimeOffset.UtcNow;
    public StandardSource? LastSource {get;set;}
    public GammaEmission? LastGamma {get;set;}
    public bool Dirty {get;set;}
}
public sealed record EfficiencyRow(EfficiencyObservation Observation)
{
    public string Source=>Observation.Source.Id+" / "+Observation.Source.Nuclide;
    public string Energy=>Observation.Gamma.EnergyKeV.ToString("G9");
    public string Efficiency=>Observation.Efficiency.ToString("G6");
    public string RelativeSigma=>(Observation.TotalRelativeSigma*100).ToString("N3");
    public string Provenance=>$"{Observation.RoiStart}–{Observation.RoiEnd} | {Path.GetFileName(Observation.SpectrumPath)}";
}
public sealed record EfficiencyRoi(int Id,RoiResult Result)
{
    public string Label=>$"ROI {Id}: {Result.Range.Start}–{Result.Range.End} | {Result.EnergyKeV:N3} keV | χ²/ν={Result.ReducedChiSquare:N2}";
}
public partial class EfficiencyWindow : Window
{
    private readonly AnalysisSession session;
    private readonly EfficiencyWorkspace workspace;
    private readonly string path,measurementId;
    private readonly string? background;
    private readonly ObservableCollection<EfficiencyRow> observations=[];
    private bool automatedSmoke;
    private ActivityResult? activity;
    private ActivityRequest? activityRequest;
    private RoiResult? activityRoi;
    private EfficiencyProfile? activityProfile;
    public EfficiencyWindow(AnalysisSession current,IReadOnlyList<RoiResult> rois,string spectrumPath,string id,string? backgroundPath,EfficiencyWorkspace state)
    {
        InitializeComponent();session=current;path=spectrumPath;measurementId=id;background=backgroundPath;workspace=state;
        SpectrumLabel.Text=Path.GetFileName(path)+" • "+session.Sample.Title;
        RoiChoice.ItemsSource=rois.Select((r,i)=>new EfficiencyRoi(i+1,r)).Where(r=>r.Result.Success).ToArray();
        if(RoiChoice.Items.Count>0)RoiChoice.SelectedIndex=0;
        MeasuredAt.Text=session.Sample.Started?.ToString("yyyy-MM-dd'T'HH:mm:ss",CultureInfo.InvariantCulture)??"";
        LiveText.Text=session.Sample.LiveTime is double l?F(l):"";RealText.Text=session.Sample.RealTime is double r?F(r):"";
        PointsGrid.ItemsSource=observations;RefreshPoints();
        if(state.Geometry is {} geometry)FillGeometry(geometry);
        ProfileName.Text=state.Name;DegreeChoice.SelectedIndex=state.Degree-1;
        if(state.LastSource is {} source)FillSource(source);
        if(state.LastGamma is {} gamma)FillGamma(gamma);
        RefreshFit();
    }
    private static string F(double x)=>x.ToString("G12",CultureInfo.InvariantCulture);
    private static double Num(TextBox box)
    {
        if(!double.TryParse(box.Text.Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double n)||!double.IsFinite(n))throw new CnfException("Nhập số hữu hạn cho: "+box.Name+" (không dùng dấu hàng nghìn).");
        return n;
    }
    private static DateTimeOffset Date(TextBox box)
    {
        string text=box.Text.Trim();
        if(!Regex.IsMatch(text,@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$",RegexOptions.IgnoreCase)||!DateTimeOffset.TryParse(text,CultureInfo.InvariantCulture,DateTimeStyles.None,out var date))
            throw new CnfException("Ngày phải có giờ và múi giờ, ví dụ 2026-10-29T10:30:00+07:00. Không tự suy diễn múi giờ từ CNF.");
        return date;
    }
    private void Run(Action action)
    {
        try{action();}
        catch(Exception e)when(e is CnfException or IOException or UnauthorizedAccessException or ArgumentException or OverflowException)
        {MessageBox.Show(this,e.Message,"Kiểm tra dữ liệu",MessageBoxButton.OK,MessageBoxImage.Warning);}
    }
    private MeasurementGeometry Geometry()
    {var g=new MeasurementGeometry(DetectorText.Text.Trim(),GeometryText.Text.Trim(),Num(DistanceText),GeometryNotes.Text.Trim());g.Validate();return g;}
    private void FillGeometry(MeasurementGeometry g){DetectorText.Text=g.Detector;GeometryText.Text=g.Configuration;DistanceText.Text=F(g.DistanceMm);GeometryNotes.Text=g.Notes;}
    private void FillSource(StandardSource s)
    {
        SourceId.Text=s.Id;SourceNuclide.Text=s.Nuclide;CertificateText.Text=s.Certificate;ActivityText.Text=F(s.ActivityBq);ActivitySigmaText.Text=F(100*s.ActivitySigmaBq/s.ActivityBq*s.CoverageFactor);ActivityCoverageText.Text=F(s.CoverageFactor);
        ReferenceDateText.Text=s.ReferenceDate.ToString("O");HalfLifeText.Text=F(s.HalfLifeDays);HalfLifeSigmaText.Text=F(s.HalfLifeSigmaDays);
    }
    private void FillGamma(GammaEmission g){GammaEnergy.Text=F(g.EnergyKeV);GammaProbability.Text=F(100*g.Probability);GammaSigma.Text=F(100*g.ProbabilitySigma);NuclearReference.Text=g.Reference;}
    private EfficiencyRoi SelectedRoi()=>RoiChoice.SelectedItem as EfficiencyRoi??throw new CnfException("Chọn ROI fit thành công ở phổ đang mở.");
    private void AddPointClick(object sender,RoutedEventArgs e)=>Run(AddPoint);
    private void AddPoint()
    {
        var g=Geometry();var roi=SelectedRoi().Result;
        if(roi.EnergyKeV is null||roi.FwhmKeV is null)throw new CnfException("ROI cần có hiệu chuẩn năng lượng hợp lệ.");
        double a=Num(ActivityText),relative=Num(ActivitySigmaText)/100;double coverage=Num(ActivityCoverageText);Guard.Positive(coverage,"Hệ số phủ k");
        var source=new StandardSource(SourceId.Text.Trim(),SourceNuclide.Text.Trim(),CertificateText.Text.Trim(),a,a*relative/coverage,Date(ReferenceDateText),Num(HalfLifeText),Num(HalfLifeSigmaText),coverage,NuclearReference.Text.Trim());
        var gamma=new GammaEmission(Num(GammaEnergy),Num(GammaProbability)/100,Num(GammaSigma)/100,NuclearReference.Text.Trim());
        var o=new EfficiencyObservation(g,source,gamma,Date(MeasuredAt),Num(LiveText),Num(RealText),roi.PeakArea!.Value,roi.AreaError!.Value,roi.EnergyKeV.Value,roi.FwhmKeV.Value,roi.ReducedChiSquare!.Value,roi.Range.Start,roi.Range.End,measurementId,path,background,session.Scale,Num(StandardCorrection),Num(StandardCorrectionSigma),StandardCorrectionNotes.Text.Trim());
        o.Validate();
        if(workspace.Points.Any(p=>!g.Matches(p.Geometry)))throw new CnfException("Điểm mới khác hình học của các điểm đang có. Tạo bộ chuẩn khác cho cấu hình khác.");
        if(workspace.Points.Any(p=>p.MeasurementId==o.MeasurementId&&p.RoiStart<=o.RoiEnd&&o.RoiStart<=p.RoiEnd))throw new CnfException("ROI trùng/chồng đã được dùng làm điểm chuẩn.");
        if(workspace.Points.Count>=500)throw new CnfException("Tối đa 500 điểm chuẩn.");
        workspace.Points.Add(o);workspace.LastSource=source;workspace.LastGamma=gamma;
        CaptureSettings();InvalidateFit();RefreshPoints();StatusText.Text="Đã thêm điểm. Lưu JSON có thể lưu cả bản nháp chưa đủ điểm để tiếp tục trên phổ khác.";
    }
    private void CaptureSettings()
    {
        workspace.Geometry=Geometry();workspace.Name=string.IsNullOrWhiteSpace(ProfileName.Text)?workspace.Geometry.Detector+" — "+workspace.Geometry.Configuration:ProfileName.Text.Trim();
        ProfileName.Text=workspace.Name;workspace.Degree=DegreeChoice.SelectedIndex+1;
    }
    private void InvalidateFit(){workspace.Profile=null;workspace.Dirty=true;activity=null;activityProfile=null;ActivitySummary.Text="Dữ liệu chuẩn đã thay đổi. Fit lại đường chuẩn trước khi tính hoạt độ.";RefreshFit();}
    private void RefreshPoints(){observations.Clear();foreach(var p in workspace.Points)observations.Add(new(p));}
    private void RemovePointClick(object sender,RoutedEventArgs e)
    {if(PointsGrid.SelectedItem is EfficiencyRow row){workspace.Points.Remove(row.Observation);InvalidateFit();RefreshPoints();}}
    private void ClearPointsClick(object sender,RoutedEventArgs e)
    {
        if(workspace.Points.Count==0)return;
        if(MessageBox.Show(this,"Xóa các điểm chuẩn đang có trong phiên? File JSON đã lưu không bị thay đổi.","Xóa điểm chuẩn",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
        workspace.Points.Clear();InvalidateFit();RefreshPoints();
    }
    private void FitClick(object sender,RoutedEventArgs e)=>Run(Fit);
    private void Fit()
    {
        CaptureSettings();workspace.Profile=EfficiencyProfile.Fit(workspace.Name,workspace.Geometry!,workspace.Points,workspace.Degree,workspace.CreatedAt);workspace.Dirty=true;activity=null;activityProfile=null;
        RefreshFit();StatusText.Text="Đã fit GLS; kiểm tra phần dư và lưu bộ chuẩn JSON. Sai số hoạt độ/bán rã chung của cùng nguồn được giữ tương quan.";
    }
    private void RefreshFit()
    {
        CurvePlot.Profile=workspace.Profile;CurvePlot.InvalidateVisual();
        if(workspace.Profile is not {} p){FitSummary.Text=$"{workspace.Points.Count} điểm. Chưa có đường chuẩn fit hợp lệ; cần bậc+2 năng lượng khác nhau.";return;}
        bool poor=p.ChiSquarePerDof>3||p.ResidualSigma.Any(r=>Math.Abs(r)>3);
        FitSummary.Text=$"{p.Definition.Name} • {p.Definition.Points.Length} điểm • {p.MinEnergy:G8}–{p.MaxEnergy:G8} keV • χ²/ν={p.ChiSquarePerDof:N3}\nln ε = Σ c[j] ln(E/1000)^j; c = "+string.Join(", ",p.Coefficients.Select(F))+(poor?"\nCẢNH BÁO: χ²/ν hoặc phần dư vượt 3; chưa cho tính hoạt độ.":"\nKhông ngoại suy. Khoảng sai số là 1σ, phụ thuộc các thành phần đã nhập.");
    }
    private void LoadProfileClick(object sender,RoutedEventArgs e)=>Run(()=>
    {
        var d=new OpenFileDialog{Filter="Efficiency JSON (*.json)|*.json"};if(d.ShowDialog(this)!=true)return;
        if(workspace.Dirty&&MessageBox.Show(this,"Thay bộ chuẩn đang sửa bằng file JSON?","Mở bộ chuẩn",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
        var f=EfficiencyProfile.ReadDefinition(d.FileName);
        EfficiencyProfile? fitted=null;
        if(f.Points.Select(p=>p.Gamma.EnergyKeV).Distinct().Count()>=f.Degree+2)fitted=EfficiencyProfile.Fit(f.Name,f.Geometry,f.Points,f.Degree,f.CreatedAt);
        workspace.Points.Clear();workspace.Points.AddRange(f.Points);workspace.Geometry=f.Geometry;workspace.Name=f.Name;workspace.Degree=f.Degree;workspace.CreatedAt=f.CreatedAt;workspace.Profile=fitted;workspace.Dirty=false;
        workspace.LastSource=f.Points.LastOrDefault()?.Source;workspace.LastGamma=f.Points.LastOrDefault()?.Gamma;
        FillGeometry(f.Geometry);ProfileName.Text=f.Name;DegreeChoice.SelectedIndex=f.Degree-1;
        if(workspace.LastSource is {} source)FillSource(source);if(workspace.LastGamma is {} gamma)FillGamma(gamma);
        GeometryConfirmed.IsChecked=false;activity=null;activityProfile=null;RefreshPoints();RefreshFit();StatusText.Text=fitted is null?"Đã mở bộ chuẩn nháp; thêm điểm rồi fit.":"Đã mở bộ chuẩn, tính lại fit từ dữ liệu gốc. Hãy kiểm tra hình học mẫu trước khi tính hoạt độ.";
    });
    private void SaveProfileClick(object sender,RoutedEventArgs e)=>Run(()=>
    {
        CaptureSettings();var f=new EfficiencyFile(1,workspace.Name,workspace.Geometry!,workspace.CreatedAt,workspace.Degree,workspace.Points.ToArray());
        EfficiencyProfile? fitted=null;
        if(f.Points.Select(p=>p.Gamma.EnergyKeV).Distinct().Count()>=f.Degree+2)fitted=EfficiencyProfile.Fit(f.Name,f.Geometry,f.Points,f.Degree,f.CreatedAt);
        var d=new SaveFileDialog{Filter="Efficiency JSON (*.json)|*.json",FileName="efficiency-calibration.json"};if(d.ShowDialog(this)!=true)return;
        EfficiencyProfile.SaveDefinition(d.FileName,f);workspace.Profile=fitted;RefreshFit();workspace.Dirty=false;StatusText.Text="Đã lưu dữ liệu chứng chỉ/điểm chuẩn JSON: "+d.FileName;
    });
    private EfficiencyProfile CurrentProfile()
    {
        var p=workspace.Profile??throw new CnfException("Fit đường chuẩn trước.");
        if(DegreeChoice.SelectedIndex+1!=p.Definition.Degree||!Geometry().Matches(p.Definition.Geometry)||(!string.IsNullOrWhiteSpace(ProfileName.Text)&&ProfileName.Text.Trim()!=p.Definition.Name))throw new CnfException("Tên/bậc/hình học bộ chuẩn đã đổi. Fit lại trước khi sử dụng hoặc xuất báo cáo.");
        return p;
    }
    private void ExportProfileClick(object sender,RoutedEventArgs e)=>Run(()=>
    {
        var p=CurrentProfile();
        var d=new SaveFileDialog{Filter="Text (*.txt)|*.txt",FileName="efficiency-report.txt"};if(d.ShowDialog(this)!=true)return;
        EfficiencyReports.Calibration(d.FileName,p);StatusText.Text="Đã xuất điểm chuẩn, phần dư, covariance và nguồn dữ liệu: "+d.FileName;
    });
    private void CalculateClick(object sender,RoutedEventArgs e)=>Run(Calculate);
    private void Calculate()
    {
        activity=null;activityProfile=null;ActivitySummary.Text="Chưa có kết quả hợp lệ cho dữ liệu hiện tại.";
        if(GeometryConfirmed.IsChecked!=true)throw new CnfException("Cần xác nhận hình học/cấu hình mẫu khớp bộ chuẩn.");
        var profile=CurrentProfile();
        var measured=Date(MeasuredAt);var gamma=new GammaEmission(Num(SampleEnergy),Num(SampleProbability)/100,Num(SampleProbabilitySigma)/100,SampleNuclearReference.Text.Trim());
        var request=new ActivityRequest(SampleNuclide.Text.Trim(),gamma,Geometry(),measurementId,measured,string.IsNullOrWhiteSpace(ReportDateText.Text)?measured:Date(ReportDateText),Num(LiveText),Num(RealText),Num(SampleHalfLife),Num(SampleHalfLifeSigma),Num(CorrectionText),Num(CorrectionSigmaText),CorrectionNotes.Text.Trim(),string.IsNullOrWhiteSpace(MassText.Text)?null:Num(MassText),Num(MassSigmaText),SampleNuclearReference.Text.Trim());
        var roi=SelectedRoi().Result;var result=ActivityAnalysis.Calculate(profile,roi,request);
        activity=result;activityRequest=request;activityRoi=roi;activityProfile=profile;
        ActivitySummary.Text=$"{request.Nuclide} • {gamma.EnergyKeV:G9} keV\nε = {result.Efficiency.Value:G7} ± {result.Efficiency.Sigma:G4}\nĐầu đo: {result.BqAtStart:N4} ± {result.SigmaBqAtStart:N4} Bq\nNgày quy chiếu {request.ReportDate:O}:\n{result.BqAtReportDate:N4} ± {result.SigmaBqAtReportDate:N4} Bq"+(result.BqPerKg is double specific?$"\n{specific:N4} ± {result.SigmaBqPerKg:N4} Bq/kg":"")+"\n\n"+result.Notes;
        StatusText.Text="Đã tính hoạt độ cho ROI này. Xuất TXT sẽ tính lại từ các ô hiện tại để tránh xuất kết quả cũ.";
    }
    private void ExportActivityClick(object sender,RoutedEventArgs e)=>Run(()=>
    {
        Calculate();
        var d=new SaveFileDialog{Filter="Text (*.txt)|*.txt",FileName="activity-report.txt"};if(d.ShowDialog(this)!=true)return;
        EfficiencyReports.Activity(d.FileName,activityProfile!,activityRoi!,activityRequest!,activity!,path,background,session.Scale);StatusText.Text="Đã xuất báo cáo hoạt độ: "+d.FileName;
    });
    internal void RunSmokeChecks(string directory)
    {
        automatedSmoke=true;
        DetectorText.Text="HPGe-SYNTHETIC";GeometryText.Text="Point-SYNTHETIC";DistanceText.Text="100";GeometryNotes.Text="SYNTHETIC ONLY";ProfileName.Text="Synthetic UI validation";
        MeasuredAt.Text="2026-01-02T00:00:00+07:00";
        SourceId.Text="SYNTHETIC-SOURCE";SourceNuclide.Text="Demo-1000";CertificateText.Text="SYNTHETIC - not a certificate";
        ReferenceDateText.Text="2026-01-01T00:00:00+07:00";HalfLifeText.Text="1000";HalfLifeSigmaText.Text="0.1";
        ActivitySigmaText.Text="4";ActivityCoverageText.Text="2";
        var roi=SelectedRoi().Result;double energy=roi.EnergyKeV!.Value;
        double True(double e)=>.01*Math.Pow(e/200,-.7);
        double activityBq=roi.PeakArea!.Value/(Num(LiveText)*.5*True(energy)*Math.Exp(-Math.Log(2)/1000)*RadioactiveDecay.AverageFactor(1000,Num(RealText)));
        ActivityText.Text=F(activityBq);GammaEnergy.Text=F(energy);GammaProbability.Text="50";GammaSigma.Text="0.5";NuclearReference.Text="SYNTHETIC nuclear data";
        AddPoint();
        var source=workspace.LastSource!;
        int index=0;foreach(double e in new[]{80d,120d,350d,600d})
        {
            var gamma=new GammaEmission(e,.5,.005,"SYNTHETIC nuclear data");
            double area=RadioactiveDecay.Exposure(source,Date(MeasuredAt),Num(LiveText),Num(RealText))*gamma.Probability*True(e);
            workspace.Points.Add(new(Geometry(),source,gamma,Date(MeasuredAt),Num(LiveText),Num(RealText),area,Math.Sqrt(area),e,2,1,index*30,index*30+20,"SYNTHETIC-ADDITIONAL-MEASUREMENT","synthetic.cnf",null,0));index++;
        }
        RefreshPoints();Fit();
        if(workspace.Profile is null||Math.Abs(workspace.Profile.Evaluate(200).Value/.01-1)>1e-5)throw new Exception("Efficiency UI fit failed.");
        string file=Path.Combine(directory,"efficiency.json");workspace.Profile.Save(file);workspace.Profile=EfficiencyProfile.Load(file);
        EfficiencyReports.Calibration(Path.Combine(directory,"efficiency.txt"),workspace.Profile);
        Tabs.SelectedIndex=1;UpdateLayout();CurvePlot.UpdateLayout();
        var image=new RenderTargetBitmap(Math.Max(1,(int)CurvePlot.ActualWidth),Math.Max(1,(int)CurvePlot.ActualHeight),96,96,PixelFormats.Pbgra32);
        image.Render(CurvePlot);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));
        using(var f=File.Create(Path.Combine(directory,"efficiency.png")))encoder.Save(f);
        workspace.Dirty=false;
        var sampleWindow=new EfficiencyWindow(session,[roi],path,"SYNTHETIC-INDEPENDENT-SAMPLE",background,workspace){Owner=this,automatedSmoke=true};
        try
        {
            sampleWindow.Show();sampleWindow.MeasuredAt.Text=MeasuredAt.Text;
            sampleWindow.SampleNuclide.Text=source.Nuclide;sampleWindow.SampleEnergy.Text=F(energy);sampleWindow.SampleProbability.Text="50";sampleWindow.SampleProbabilitySigma.Text="0.5";
            sampleWindow.SampleNuclearReference.Text="SYNTHETIC nuclear data";sampleWindow.SampleHalfLife.Text="1000";sampleWindow.SampleHalfLifeSigma.Text="0.1";
            sampleWindow.MassText.Text="0.5";sampleWindow.MassSigmaText.Text="0.001";sampleWindow.GeometryConfirmed.IsChecked=true;
            sampleWindow.Calculate();
            if(sampleWindow.activity is null||!(sampleWindow.activity.BqPerKg>0))throw new Exception("Activity UI calculation failed.");
            EfficiencyReports.Activity(Path.Combine(directory,"activity.txt"),workspace.Profile,roi,sampleWindow.activityRequest!,sampleWindow.activity,path,background,session.Scale);
        }
        finally{sampleWindow.Close();}
        StatusText.Text="Synthetic efficiency/activity UI checks passed. No real calibration is installed.";
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if(!automatedSmoke&&workspace.Dirty&&MessageBox.Show(this,"Bộ chuẩn/điểm mới chưa lưu JSON. Đóng cửa sổ? Dữ liệu còn trong phiên app; sẽ mất nếu đóng app mà chưa lưu.","Chưa lưu bộ chuẩn",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)e.Cancel=true;
        base.OnClosing(e);
    }
}
