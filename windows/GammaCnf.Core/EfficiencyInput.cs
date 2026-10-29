using System.Globalization;
using System.Text.RegularExpressions;
namespace GammaCnf.Core;

/// <summary>Shared Windows/macOS form parsing. Missing metadata never becomes a fabricated certificate or a measured zero distance.</summary>
public static class EfficiencyInput
{
    public static double Number(string? text,string label,double? emptyDefault=null)
    {
        if(string.IsNullOrWhiteSpace(text)&&emptyDefault.HasValue)return emptyDefault.Value;
        if(!double.TryParse((text??"").Trim().Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double value)||!double.IsFinite(value))
            throw new CnfException("Nhập số hợp lệ cho "+label+" (không dùng dấu hàng nghìn).");
        return value;
    }
    public static MeasurementGeometry Geometry(string? detector,string? configuration,string? distance,string? notes)
    {
        var geometry=new MeasurementGeometry((detector??"").Trim(),(configuration??"").Trim(),Number(distance,"khoảng cách",0),(notes??"").Trim(),true,!string.IsNullOrWhiteSpace(distance));
        geometry.Validate();return geometry;
    }
    public static EfficiencyObservation Build(AnalysisSession session,RoiResult roi,string path,string measurementId,string? background,
        IReadOnlyDictionary<string,string?> fields,bool certificateDecay)
    {
        string T(string key)=>fields.GetValueOrDefault(key)?.Trim()??"";
        double N(string key,string label,double? fallback=null)=>Number(T(key),label,fallback);
        var missing=new List<string>();
        void Missing(string key,string label){if(T(key).Length==0)missing.Add(label);}
        double Optional(string key,string label,double fallback=0){Missing(key,label);return N(key,label,fallback);}
        DateTimeOffset ReadDate(string key,bool required)
        {
            string value=T(key);
            if(!required&&(value.Length==0||Regex.IsMatch(value,@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")))
            {missing.Add(key=="MeasuredAt"?"múi giờ/ngày đầu đo chưa cung cấp (không dùng để quy đổi hoạt độ)":"ngày chứng nhận không dùng trong chế độ A tại lúc đo");return DateTimeOffset.MinValue;}
            if(!Regex.IsMatch(value,@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$",RegexOptions.IgnoreCase)||!DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.None,out var date))
                throw new CnfException("Cần ngày/giờ có múi giờ để hiệu chỉnh phân rã: "+key+", ví dụ 2026-10-29T10:30:00+07:00.");
            return date;
        }
        if(!roi.Success||roi.EnergyKeV is null||roi.FwhmKeV is null)throw new CnfException("Chọn ROI Gaussian đạt và có chuẩn năng lượng.");
        var geometry=Geometry(T("DetectorText"),T("GeometryText"),T("DistanceText"),T("GeometryNotes"));
        foreach(var pair in new[]{("DetectorText","detector chưa khai báo"),("GeometryText","hình học chưa khai báo"),("DistanceText","khoảng cách chưa khai báo"),("CertificateText","chứng chỉ chưa cung cấp"),("SourceNuclide","đồng vị chưa khai báo"),("NuclearReference","tham chiếu dữ liệu hạt nhân chưa cung cấp")})Missing(pair.Item1,pair.Item2);
        string id=T("SourceId");if(id.Length==0){id="auto:"+measurementId+":"+T("SourceNuclide").ToUpperInvariant();missing.Add("mã nguồn chưa khai báo: sai số hoạt độ giữa các điểm coi độc lập");}
        double activity=N("ActivityText",certificateDecay?"hoạt độ tại ngày chứng nhận (Bq)":"hoạt độ tại đầu phép đo (Bq)");
        double sigma=Optional("ActivitySigmaText","sai số hoạt độ nguồn chưa cung cấp")/100;
        double coverage=N("ActivityCoverageText","hệ số phủ k",1);
        if(T("ActivityCoverageText").Length==0&&T("ActivitySigmaText").Length>0)missing.Add("k bỏ trống: sai số hoạt độ được hiểu là 1σ (k=1)");
        var measured=ReadDate("MeasuredAt",certificateDecay);
        var reference=certificateDecay?ReadDate("ReferenceDateText",true):DateTimeOffset.MinValue;
        double half=N("HalfLifeText","bán rã (ngày)",certificateDecay?null:0);
        if(!certificateDecay&&half==0)missing.Add("chưa hiệu chỉnh phân rã trong phép đo: không nhập bán rã");
        if(half==0&&T("HalfLifeText").Length>0)throw new CnfException("Bán rã nếu nhập phải dương; để trống nếu không hiệu chỉnh phân rã.");
        if(id.StartsWith("auto:"))id+=":"+activity.ToString("R",CultureInfo.InvariantCulture)+":"+half.ToString("R",CultureInfo.InvariantCulture);
        double halfSigma=Optional("HalfLifeSigmaText","sai số bán rã chưa cung cấp");
        double real=N("RealText","real time",0),live=N("LiveText","live time");
        if(T("RealText").Length>0&&real<=0)throw new CnfException("Real time nếu nhập phải dương; để trống nếu chưa có.");
        if(real==0)missing.Add("real time chưa cung cấp");
        if(half>0&&real<=0)throw new CnfException("Cần real time khi nhập bán rã để hiệu chỉnh phân rã trong phép đo; bỏ bán rã nếu chỉ tính ε=N/(A×t×Pγ).");
        var source=new StandardSource(id,T("SourceNuclide"),T("CertificateText"),activity,activity*sigma/coverage,reference,half,halfSigma,coverage,T("NuclearReference"),!certificateDecay,true);
        double energy=T("GammaEnergy").Length==0?roi.EnergyKeV.Value:N("GammaEnergy","năng lượng keV");
        var gamma=new GammaEmission(energy,N("GammaProbability","xác suất gamma (%)")/100,Optional("GammaSigma","sai số xác suất gamma chưa cung cấp")/100,T("NuclearReference"),true);
        double correction=N("StandardCorrection","K",1),correctionSigma=N("StandardCorrectionSigma","sai số K",0);
        if(correction!=1&&T("StandardCorrectionSigma").Length==0)missing.Add("sai số K chưa cung cấp");
        if((correction!=1||correctionSigma!=0)&&T("StandardCorrectionNotes").Length==0)missing.Add("K hiệu chỉnh chưa có ghi chú căn cứ");
        var observation=new EfficiencyObservation(geometry,source,gamma,measured,live,real,roi.PeakArea!.Value,roi.AreaError!.Value,roi.EnergyKeV.Value,roi.FwhmKeV.Value,roi.ReducedChiSquare!.Value,roi.Range.Start,roi.Range.End,measurementId,path,background,session.Scale,correction,correctionSigma,T("StandardCorrectionNotes"),missing.ToArray());
        observation.Validate();return observation;
    }
}
