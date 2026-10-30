using System.Globalization;
using System.Text;
using System.Text.Json;

namespace GammaCnf.Core;

public sealed record MeasurementGeometry(string Detector,string Configuration,double DistanceMm,string Notes,bool OptionalMetadata=false,bool DistanceSpecified=true)
{
    public void Validate()
    {
        if((!OptionalMetadata&&(string.IsNullOrWhiteSpace(Detector)||string.IsNullOrWhiteSpace(Configuration)))||!double.IsFinite(DistanceMm)||DistanceMm<0)
            throw new CnfException("Nhập detector, mã hình học/cấu hình mẫu và khoảng cách không âm (mm).");
    }
    public bool Matches(MeasurementGeometry other)=>string.Equals(Detector.Trim(),other.Detector.Trim(),StringComparison.OrdinalIgnoreCase)
        &&string.Equals(Configuration.Trim(),other.Configuration.Trim(),StringComparison.OrdinalIgnoreCase)&&DistanceSpecified==other.DistanceSpecified&&(!DistanceSpecified||Math.Abs(DistanceMm-other.DistanceMm)<.01)
        &&string.Equals(Notes.Trim(),other.Notes.Trim(),StringComparison.OrdinalIgnoreCase);
}
public sealed record StandardSource(string Id,string Nuclide,string Certificate,double ActivityBq,double ActivitySigmaBq,
    DateTimeOffset ReferenceDate,double HalfLifeDays,double HalfLifeSigmaDays,double CoverageFactor=1,string HalfLifeReference="",bool ActivityAtMeasurement=false,bool OptionalMetadata=false)
{
    public void Validate()
    {
        if(string.IsNullOrWhiteSpace(Id)||(!OptionalMetadata&&(string.IsNullOrWhiteSpace(Nuclide)||string.IsNullOrWhiteSpace(Certificate))))throw new CnfException("Nhập mã nguồn, đồng vị và tham chiếu chứng chỉ.");
        Guard.Positive(CoverageFactor,"Hệ số phủ k");if(CoverageFactor<1)throw new CnfException("Hệ số phủ chứng chỉ k phải ≥1.");
        Guard.Positive(ActivityBq,"Hoạt độ chuẩn");Guard.Nonnegative(ActivitySigmaBq,"Sai số hoạt độ");if(ActivityAtMeasurement)Guard.Nonnegative(HalfLifeDays,"Chu kỳ bán rã");else Guard.Positive(HalfLifeDays,"Chu kỳ bán rã");Guard.Nonnegative(HalfLifeSigmaDays,"Sai số chu kỳ bán rã");
        if(HalfLifeDays==0&&HalfLifeSigmaDays!=0)throw new CnfException("Nhập bán rã trước khi nhập sai số bán rã.");
        if(ActivitySigmaBq/ActivityBq>.5||(HalfLifeDays>0&&HalfLifeSigmaDays/HalfLifeDays>.2))throw new CnfException("Sai số chuẩn quá lớn cho phép lan truyền tuyến tính (hoạt độ ≤50%, chu kỳ bán rã ≤20%).");
    }
}
public sealed record GammaEmission(double EnergyKeV,double Probability,double ProbabilitySigma,string Reference,bool OptionalMetadata=false)
{
    public void Validate()
    {
        Guard.Positive(EnergyKeV,"Năng lượng gamma");Guard.Positive(Probability,"Xác suất gamma");Guard.Nonnegative(ProbabilitySigma,"Sai số xác suất gamma");
        if(Probability>1||ProbabilitySigma>=Probability||(!OptionalMetadata&&string.IsNullOrWhiteSpace(Reference)))throw new CnfException("Xác suất gamma phải trong (0,1], sai số nhỏ hơn xác suất; cần nguồn dữ liệu hạt nhân.");
    }
    public string Key(string nuclide)=>string.IsNullOrWhiteSpace(nuclide)||string.IsNullOrWhiteSpace(Reference)?"":$"gamma:{nuclide.Trim().ToUpperInvariant()}:{EnergyKeV.ToString("G17",CultureInfo.InvariantCulture)}:{Reference.Trim().ToUpperInvariant()}";
}
public static class Guard
{
    public static void Positive(double x,string label){if(!double.IsFinite(x)||x<=0)throw new CnfException(label+" phải dương và hữu hạn.");}
    public static void Nonnegative(double x,string label){if(!double.IsFinite(x)||x<0)throw new CnfException(label+" phải không âm và hữu hạn.");}
}
public static class RadioactiveDecay
{
    public static double AverageFactor(double halfLifeDays,double realSeconds)
    {
        Guard.Positive(halfLifeDays,"Chu kỳ bán rã");Guard.Positive(realSeconds,"Real time");
        double x=Math.Log(2)*realSeconds/(halfLifeDays*86400);
        double f=x<1e-5?1-x/2+x*x/6-x*x*x/24:(1-Math.Exp(-x))/x;
        Guard.Positive(f,"Hệ số phân rã trong phép đo");return f;
    }
    public static double Exposure(StandardSource source,DateTimeOffset measured,double live,double real)
    {
        source.Validate();
        if(source.ActivityAtMeasurement&&source.HalfLifeDays==0){Guard.Positive(live,"Live time");if(real!=0)ValidateTimes(live,real);return source.ActivityBq*live;}
        ValidateTimes(live,real);
        double exponent=source.ActivityAtMeasurement?0:-Math.Log(2)*(measured-source.ReferenceDate).TotalDays/source.HalfLifeDays;
        double result=source.ActivityBq*Math.Exp(exponent)*live*AverageFactor(source.HalfLifeDays,real);
        Guard.Positive(result,"Tích hoạt độ–thời gian");return result;
    }
    public static void ValidateTimes(double live,double real)
    { Guard.Positive(live,"Live time");Guard.Positive(real,"Real time");if(live>real*(1+1e-9))throw new CnfException("Live time không thể lớn hơn real time."); }
    public static double HalfLifeLogSensitivity(StandardSource source,DateTimeOffset date,double live,double real)
    {
        if(source.HalfLifeSigmaDays==0)return 0;
        double delta=source.HalfLifeDays*1e-5;
        double a=Math.Log(Exposure(source with{HalfLifeDays=source.HalfLifeDays+delta,HalfLifeSigmaDays=0},date,live,real));
        double b=Math.Log(Exposure(source with{HalfLifeDays=source.HalfLifeDays-delta,HalfLifeSigmaDays=0},date,live,real));
        return -(a-b)/(2*delta)*source.HalfLifeSigmaDays;
    }
}
public sealed record EfficiencyObservation(MeasurementGeometry Geometry,StandardSource Source,GammaEmission Gamma,
    DateTimeOffset MeasuredAt,double LiveSeconds,double RealSeconds,double Area,double AreaSigma,
    double CentroidKeV,double FwhmKeV,double ReducedChiSquare,int RoiStart,int RoiEnd,
    string MeasurementId,string SpectrumPath,string? BackgroundPath,double BackgroundScale,double Correction=1,double CorrectionSigma=0,string CorrectionNotes="",string[]? MissingInputs=null)
{
    public void Validate()
    {
        Geometry.Validate();Source.Validate();Gamma.Validate();
        if(Source.ActivityAtMeasurement&&Source.HalfLifeDays==0){Guard.Positive(LiveSeconds,"Live time");if(RealSeconds!=0)RadioactiveDecay.ValidateTimes(LiveSeconds,RealSeconds);}
        else RadioactiveDecay.ValidateTimes(LiveSeconds,RealSeconds);
        Guard.Positive(Area,"Diện tích đỉnh");Guard.Positive(AreaSigma,"Sai số diện tích");Guard.Positive(CentroidKeV,"Tâm đỉnh keV");Guard.Positive(FwhmKeV,"FWHM keV");Guard.Nonnegative(ReducedChiSquare,"Chi2/ν");
        Guard.Nonnegative(BackgroundScale,"Hệ số phông");Guard.Positive(Correction,"K đỉnh nguồn chuẩn");Guard.Nonnegative(CorrectionSigma,"Sai số K đỉnh nguồn chuẩn");
        if(!Source.OptionalMetadata&&(Correction!=1||CorrectionSigma!=0)&&string.IsNullOrWhiteSpace(CorrectionNotes))throw new CnfException("Ghi căn cứ K hiệu chỉnh đỉnh nguồn chuẩn.");
        if(RoiStart<0||RoiEnd-RoiStart<8||string.IsNullOrWhiteSpace(MeasurementId))
            throw new CnfException("Điểm chuẩn cần ROI hợp lệ và mã phép đo.");
        if(Math.Abs(CentroidKeV-Gamma.EnergyKeV)>Math.Max(.2,FwhmKeV/2))throw new CnfException("Vạch gamma không khớp tâm ROI (lệch hơn nửa FWHM hoặc 0,2 keV).");
        double epsilon=Efficiency;
        if(!double.IsFinite(epsilon)||epsilon<=0||epsilon>1)throw new CnfException("Hiệu suất đỉnh phải trong (0,1]. Kiểm tra Bq, %, thời gian, phân rã và ghép vạch.");
    }
    public string[] QualityWarnings=>new[]{Area/AreaSigma<3?"Diện tích điểm chuẩn <3σ.":null,ReducedChiSquare>3?"χ²/ν ROI chuẩn >3.":null}.OfType<string>().ToArray();
    public double Efficiency=>Area*Correction/(RadioactiveDecay.Exposure(Source,MeasuredAt,LiveSeconds,RealSeconds)*Gamma.Probability);
    public string HalfLifeKey=>(string.IsNullOrWhiteSpace(Source.HalfLifeReference)||string.IsNullOrWhiteSpace(Source.Nuclide))?$"source:{Source.Id.Trim().ToUpperInvariant()}:half-life":$"half-life:{Source.Nuclide.Trim().ToUpperInvariant()}:{Source.HalfLifeReference.Trim().ToUpperInvariant()}";
    public string SourceKey=>Source.ActivityAtMeasurement?Source.Id+":"+MeasurementId:Source.Id;
    public Dictionary<string,double> Systematics()
    {
        var result=new Dictionary<string,double>
        {
            [HalfLifeKey]=RadioactiveDecay.HalfLifeLogSensitivity(Source,MeasuredAt,LiveSeconds,RealSeconds)
        };
        if(!Source.Id.StartsWith("auto:"))result[$"source:{SourceKey.Trim().ToUpperInvariant()}:activity"]=-Source.ActivitySigmaBq/Source.ActivityBq;
        string key=Gamma.Key(Source.Nuclide);
        if(key.Length>0)result[key]=-Gamma.ProbabilitySigma/Gamma.Probability;
        return result;
    }
    public double IndependentLogVariance=>Math.Pow(AreaSigma/Area,2)+Math.Pow(CorrectionSigma/Correction,2)+(Source.Id.StartsWith("auto:")?Math.Pow(Source.ActivitySigmaBq/Source.ActivityBq,2):0)+
        (Gamma.Key(Source.Nuclide).Length==0?Math.Pow(Gamma.ProbabilitySigma/Gamma.Probability,2):0);
    public double TotalRelativeSigma=>Math.Sqrt(IndependentLogVariance+Systematics().Values.Sum(x=>x*x));
}
public sealed record EfficiencyFile(int SchemaVersion,string Name,MeasurementGeometry Geometry,DateTimeOffset CreatedAt,int Degree,EfficiencyObservation[] Points);
public sealed record EfficiencyEstimate(double Value,double Sigma);
public sealed class EfficiencyProfile
{
    public EfficiencyFile Definition {get;}
    public double[] Coefficients {get;}
    public double[,] Covariance {get;}
    public double[] ResidualSigma {get;}
    public double ChiSquarePerDof {get;}
    public string[] InputWarnings=>Definition.Points.SelectMany(p=>p.MissingInputs??[]).Distinct().ToArray();
    public string[] QualityWarnings=>Definition.Points.SelectMany((p,i)=>p.QualityWarnings.Select(w=>$"Điểm {i+1}: {w}")).Concat(ChiSquarePerDof>3?["χ²/ν đường chuẩn >3."]:Array.Empty<string>()).Concat(ResidualSigma.Any(r=>Math.Abs(r)>3)?["Phần dư đường chuẩn vượt 3σ."]:Array.Empty<string>()).ToArray();
    public string QualitySummary=>QualityWarnings.Length==0?"":"CẢNH BÁO: "+string.Join(" ",QualityWarnings)+" Kết quả cần được kiểm tra trước khi sử dụng.";
    public bool HasOptionalInputs=>InputWarnings.Length>0||Definition.Geometry.OptionalMetadata;
    public string InputSummary=>InputWarnings.Length==0?"":"Thông tin chưa cung cấp/giả định: "+string.Join("; ",InputWarnings)+". Sai số chỉ gồm các thành phần đã nhập; chưa phải ngân sách đầy đủ.";
    public double MinEnergy=>Definition.Points.Min(p=>p.Gamma.EnergyKeV);
    public double MaxEnergy=>Definition.Points.Max(p=>p.Gamma.EnergyKeV);
    private double center,scale;
    private double[] fitCoefficients=[];
    private double[,] fitCovariance=new double[0,0];
    private EfficiencyProfile(EfficiencyFile file,double[] coefficients,double[,] covariance,double[] residuals,double chi2)
    {Definition=file;Coefficients=coefficients;Covariance=covariance;ResidualSigma=residuals;ChiSquarePerDof=chi2;}
    public static EfficiencyProfile Fit(string name,MeasurementGeometry geometry,IEnumerable<EfficiencyObservation> observations,int degree,DateTimeOffset? created=null)
    {
        geometry.Validate();var points=observations.ToArray();
        if(string.IsNullOrWhiteSpace(name)||degree is <1 or >6||points.Length<degree+2||points.Length>500)throw new CnfException("Đặt tên bộ chuẩn; bậc 1–6 cần ít nhất bậc+2 điểm, tối đa 500 điểm.");
        foreach(var p in points){p.Validate();if(!geometry.Matches(p.Geometry))throw new CnfException("Không gộp điểm chuẩn khác detector/hình học/khoảng cách/ghi chú cấu hình.");}
        foreach(var group in points.Where(p=>!p.Source.Id.StartsWith("auto:")).GroupBy(p=>p.SourceKey.Trim().ToUpperInvariant()))
            if(group.Any(p=>(p.Source with{OptionalMetadata=group.First().Source.OptionalMetadata})!=group.First().Source))throw new CnfException("Cùng mã nguồn phải có cùng thông tin chứng chỉ, đồng vị, hoạt độ và bán rã.");
        foreach(var group in points.GroupBy(p=>p.HalfLifeKey))
            if(group.Any(p=>p.Source.HalfLifeDays!=group.First().Source.HalfLifeDays||p.Source.HalfLifeSigmaDays!=group.First().Source.HalfLifeSigmaDays))throw new CnfException("Cùng dữ liệu bán rã phải dùng cùng giá trị và sai số.");
        foreach(var group in points.Where(p=>p.Gamma.Key(p.Source.Nuclide).Length>0).GroupBy(p=>p.Gamma.Key(p.Source.Nuclide)))
            if(group.Any(p=>(p.Gamma with{OptionalMetadata=group.First().Gamma.OptionalMetadata})!=group.First().Gamma))throw new CnfException("Cùng vạch và nguồn dữ liệu phải có cùng xác suất gamma/sai số.");
        for(int i=0;i<points.Length;i++)for(int j=0;j<i;j++)
            if(points[i].MeasurementId==points[j].MeasurementId&&points[i].RoiStart<=points[j].RoiEnd&&points[j].RoiStart<=points[i].RoiEnd)
                throw new CnfException("Không dùng lặp hoặc chồng ROI của cùng phép đo làm các điểm chuẩn độc lập.");
        if(points.Select(p=>p.Gamma.EnergyKeV).Distinct().Count()<degree+2)throw new CnfException("Cần ít nhất bậc+2 năng lượng khác nhau.");
        int n=points.Length,m=degree+1;var x=new double[n][];var y=new double[n];var noise=new double[n,n];
        double center=(Math.Log(points.Min(p=>p.Gamma.EnergyKeV)/1000)+Math.Log(points.Max(p=>p.Gamma.EnergyKeV)/1000))/2;
        double scale=(Math.Log(points.Max(p=>p.Gamma.EnergyKeV)/1000)-Math.Log(points.Min(p=>p.Gamma.EnergyKeV)/1000))/2;
        Guard.Positive(scale,"Độ rộng miền log năng lượng");
        var shared=points.Select(p=>p.Systematics()).ToArray();
        for(int i=0;i<n;i++)
        {
            x[i]=Basis(points[i].Gamma.EnergyKeV,m,center,scale);y[i]=Math.Log(points[i].Efficiency);
            for(int j=0;j<n;j++)noise[i,j]=(i==j?points[i].IndependentLogVariance:0)+shared[i].Sum(k=>shared[j].GetValueOrDefault(k.Key)*k.Value);
        }
        double[,] precision=LinearAlgebra.InversePositive(noise);
        var normal=new double[m,m];var rhs=new double[m];
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)for(int a=0;a<m;a++)
        {rhs[a]+=x[i][a]*precision[i,j]*y[j];for(int b=0;b<m;b++)normal[a,b]+=x[i][a]*precision[i,j]*x[j][b];}
        double[,] covariance=LinearAlgebra.InversePositive(normal);double[] c=LinearAlgebra.Multiply(covariance,rhs);
        var residuals=new double[n];var r=new double[n];
        for(int i=0;i<n;i++){r[i]=y[i]-x[i].Zip(c,(a,b)=>a*b).Sum();residuals[i]=r[i]/Math.Sqrt(noise[i,i]);}
        double chi2=r.Zip(LinearAlgebra.Multiply(precision,r),(a,b)=>a*b).Sum()/(n-m);
        // Convert the centered/scaled fit back to the documented ln(E/1000) coefficients.
        var transform=new double[m,m];
        for(int j=0;j<m;j++)for(int k=0;k<=j;k++)
        {
            double choose=1;for(int q=1;q<=k;q++)choose*= (double)(j-q+1)/q;
            transform[k,j]=choose*Math.Pow(-center,j-k)/Math.Pow(scale,j);
        }
        var publicC=LinearAlgebra.Multiply(transform,c);var publicCov=new double[m,m];
        for(int a=0;a<m;a++)for(int b=0;b<m;b++)for(int i=0;i<m;i++)for(int j=0;j<m;j++)publicCov[a,b]+=transform[a,i]*covariance[i,j]*transform[b,j];
        var model=new EfficiencyProfile(new(2,name,geometry,created??DateTimeOffset.UtcNow,degree,points),publicC,publicCov,residuals,chi2){center=center,scale=scale,fitCoefficients=c,fitCovariance=covariance};
        if(!double.IsFinite(chi2)||c.Any(v=>!double.IsFinite(v))||publicC.Any(v=>!double.IsFinite(v))||publicCov.Cast<double>().Any(v=>!double.IsFinite(v)))throw new CnfException("Fit hiệu suất không hữu hạn.");
        // Check every stationary point, including even-multiplicity derivative roots.
        var extrema=PolynomialRoots(c.Skip(1).Select((v,i)=>v*(i+1)).ToArray(),-1,1).Concat(new[]{-1d,1d});
        if(extrema.Any(t=>EnergyCalibration.Evaluate(c,t)>0))throw new CnfException("Đường fit dự đoán hiệu suất >1 trong miền chuẩn; giảm bậc hoặc kiểm tra điểm chuẩn.");
        return model;
    }
    private static IEnumerable<double> PolynomialRoots(double[] c,double lo,double hi)
    {
        int degree=c.Length-1;while(degree>0&&c[degree]==0)degree--;
        if(degree==0)return Array.Empty<double>();
        if(degree==1){double r=-c[0]/c[1];return r>=lo&&r<=hi?[r]:Array.Empty<double>();}
        var boundaries=PolynomialRoots(c.Take(degree+1).Skip(1).Select((v,i)=>v*(i+1)).ToArray(),lo,hi).Append(lo).Append(hi).Order().ToArray();
        var roots=new List<double>();
        foreach(double t in boundaries)if(Math.Abs(EnergyCalibration.Evaluate(c,t))<1e-12)roots.Add(t);
        for(int i=1;i<boundaries.Length;i++)
        {
            double a=boundaries[i-1],b=boundaries[i],fa=EnergyCalibration.Evaluate(c,a),fb=EnergyCalibration.Evaluate(c,b);
            if(Math.Sign(fa)==Math.Sign(fb)||fa==0||fb==0)continue;
            for(int j=0;j<80;j++){double mid=(a+b)/2,f=EnergyCalibration.Evaluate(c,mid);if(f==0){a=b=mid;break;}if(Math.Sign(f)==Math.Sign(fa)){a=mid;fa=f;}else b=mid;}
            roots.Add((a+b)/2);
        }
        return roots;
    }
    private static double[] Basis(double energy,int count,double center=0,double scale=1)
    {Guard.Positive(energy,"Năng lượng");double t=(Math.Log(energy/1000)-center)/scale;var b=new double[count];b[0]=1;for(int i=1;i<count;i++)b[i]=b[i-1]*t;return b;}
    public EfficiencyEstimate Evaluate(double energy)
    {
        if(!double.IsFinite(energy)||energy<MinEnergy||energy>MaxEnergy)throw new CnfException("Năng lượng ngoài miền chuẩn. Không cho phép ngoại suy.");
        double[] b=Basis(energy,Coefficients.Length,center,scale);double value=Math.Exp(b.Zip(fitCoefficients,(x,y)=>x*y).Sum());
        double variance=b.Zip(LinearAlgebra.Multiply(fitCovariance,b),(x,y)=>x*y).Sum();
        if(!double.IsFinite(value)||value<=0||value>1||!double.IsFinite(variance)||variance<0)throw new CnfException("Hiệu suất hoặc sai số mô hình không hợp lệ.");
        return new(value,value*Math.Sqrt(variance));
    }
    public double SharedLogResponse(double energy,string key)
    {
        // Sensitivity of fitted ln(efficiency) to one shared standardized input.
        int n=Definition.Points.Length,m=Coefficients.Length;
        var noise=new double[n,n];var shared=Definition.Points.Select(p=>p.Systematics()).ToArray();
        for(int i=0;i<n;i++)for(int j=0;j<n;j++)noise[i,j]=(i==j?Definition.Points[i].IndependentLogVariance:0)+shared[i].Sum(k=>shared[j].GetValueOrDefault(k.Key)*k.Value);
        var precision=LinearAlgebra.InversePositive(noise);var rhs=new double[m];
        for(int i=0;i<n;i++)
        {
            double weight=0;for(int j=0;j<n;j++)weight+=precision[i,j]*shared[j].GetValueOrDefault(key);
            var basis=Basis(Definition.Points[i].Gamma.EnergyKeV,m,center,scale);for(int a=0;a<m;a++)rhs[a]+=basis[a]*weight;
        }
        return Basis(energy,m,center,scale).Zip(LinearAlgebra.Multiply(fitCovariance,rhs),(a,b)=>a*b).Sum();
    }
    public void Save(string path)=>SaveDefinition(path,Definition);
    public static EfficiencyFile ReadDefinition(string path)
    {
        if(new FileInfo(path).Length>4*1024*1024)throw new CnfException("Bộ chuẩn JSON vượt 4 MB.");
        try
        {
            var file=JsonSerializer.Deserialize<EfficiencyFile>(File.ReadAllText(path))??throw new CnfException("JSON rỗng.");
            if(file.SchemaVersion is not (1 or 2)||file.Points is null||file.Geometry is null||file.Points.Length>500||file.Degree is <1 or >6||string.IsNullOrWhiteSpace(file.Name)||file.Points.Any(p=>p is null||p.Geometry is null||p.Source is null||p.Gamma is null))throw new CnfException("Schema bộ chuẩn không được hỗ trợ hoặc thiếu dữ liệu.");
            if(file.SchemaVersion==1&&(file.Geometry.OptionalMetadata||file.Points.Any(p=>p.Source.ActivityAtMeasurement||p.Source.OptionalMetadata||p.Gamma.OptionalMetadata||p.MissingInputs is not null)))throw new CnfException("Dữ liệu nhập gọn cần schema 2 để không bị hiểu sai chế độ hoạt độ.");
            file.Geometry.Validate();foreach(var p in file.Points){p.Validate();if(!p.Geometry.Matches(file.Geometry))throw new CnfException("Điểm trong JSON khác hình học bộ chuẩn.");}
            return file;
        }
        catch(Exception e)when(e is JsonException or NullReferenceException or ArgumentException){throw new CnfException("Bộ chuẩn JSON không hợp lệ: "+e.Message);}
    }
    public static EfficiencyProfile Load(string path)
    {var file=ReadDefinition(path);return Fit(file.Name,file.Geometry,file.Points,file.Degree,file.CreatedAt);}
    public static void SaveDefinition(string path,EfficiencyFile file)
    {
        file.Geometry.Validate();foreach(var p in file.Points)p.Validate();
        if(file.SchemaVersion is not (1 or 2)||file.Degree is <1 or >6||file.Points.Length>500||string.IsNullOrWhiteSpace(file.Name)||file.Points.Any(p=>!p.Geometry.Matches(file.Geometry)))throw new CnfException("Bộ chuẩn nháp không hợp lệ.");
        if(file.SchemaVersion==1&&(file.Geometry.OptionalMetadata||file.Points.Any(p=>p.Source.ActivityAtMeasurement||p.Source.OptionalMetadata||p.Gamma.OptionalMetadata||p.MissingInputs is not null)))throw new CnfException("Dữ liệu nhập gọn phải lưu schema 2.");
        string temp=Path.GetFullPath(path)+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temp,JsonSerializer.Serialize(file,new JsonSerializerOptions{WriteIndented=true}),new UTF8Encoding(false));File.Move(temp,path,true);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }

}
internal static class LinearAlgebra
{
    public static double[] Multiply(double[,] a,double[] b)
    {var r=new double[b.Length];for(int i=0;i<b.Length;i++)for(int j=0;j<b.Length;j++)r[i]+=a[i,j]*b[j];return r;}
    public static double[,] InversePositive(double[,] a)
    {
        int n=a.GetLength(0);var l=new double[n,n];
        for(int i=0;i<n;i++)for(int j=0;j<=i;j++)
        {
            double sum=a[i,j];for(int k=0;k<j;k++)sum-=l[i,k]*l[j,k];
            if(i==j){if(!double.IsFinite(sum)||sum<=Math.Max(1e-25,Math.Abs(a[i,i])*1e-13))throw new CnfException("Ma trận fit suy biến; cần thêm điểm độc lập/phân bố năng lượng rộng hơn hoặc giảm bậc.");l[i,j]=Math.Sqrt(sum);}
            else l[i,j]=sum/l[j,j];
        }
        var inv=new double[n,n];
        for(int col=0;col<n;col++)
        {
            var y=new double[n];var x=new double[n];
            for(int i=0;i<n;i++){double s=i==col?1:0;for(int k=0;k<i;k++)s-=l[i,k]*y[k];y[i]=s/l[i,i];}
            for(int i=n-1;i>=0;i--){double s=y[i];for(int k=i+1;k<n;k++)s-=l[k,i]*x[k];x[i]=s/l[i,i];}
            for(int i=0;i<n;i++)inv[i,col]=x[i];
        }
        return inv;
    }
}
