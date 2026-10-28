using System.Globalization;
using System.Text;
namespace GammaCnf.Core;

public sealed class AnalysisSession
{
    public Spectrum Sample { get; private set; }
    public Spectrum? Background { get; private set; }
    public double Scale { get; private set; }
    public double[] Values { get; private set; }
    public double[] Variances { get; private set; }
    public bool ChannelOnlyBackground { get; private set; }
    public AnalysisSession(Spectrum sample)
    { Sample=sample;Values=[];Variances=[];Recalculate(); }
    public void SetCalibration(double[] coefficients)
    {
        var candidate=Sample with { Calibration=EnergyCalibration.Validate(coefficients,Sample.Counts.Length) };
        if(Background is not null)CheckAlignment(candidate,Background,ChannelOnlyBackground);
        Sample=candidate;Recalculate();
    }
    public static double LiveTimeScale(Spectrum sample,Spectrum background)
    {
        if(!(sample.LiveTime>0) || !(background.LiveTime>0))throw new CnfException("Thiếu thời gian sống hợp lệ; nhập hệ số trừ phông thủ công.");
        return sample.LiveTime.Value/background.LiveTime.Value;
    }
    public static void CheckAlignment(Spectrum sample,Spectrum background,bool allowChannelOnly=false)
    {
        if(sample.Counts.Length!=background.Counts.Length)throw new CnfException("Phổ mẫu và phông phải cùng số kênh. App chưa tự rebin.");
        if(sample.Calibration is null || background.Calibration is null)
        {
            if(!allowChannelOnly)throw new CnfException("Thiếu hiệu chuẩn. Chỉ trừ theo kênh khi bạn xác nhận hai phổ dùng cùng cấu hình MCA.");
            return;
        }
        EnergyCalibration.Validate(sample.Calibration,sample.Counts.Length);
        EnergyCalibration.Validate(background.Calibration,background.Counts.Length);
        for(int i=0;i<sample.Counts.Length;i++)
        {
            double width=sample.Energy(i+1)!.Value-sample.Energy(i)!.Value;
            if(Math.Abs(sample.Energy(i)!.Value-background.Energy(i)!.Value)>Math.Abs(width)*.05)
                throw new CnfException("Hiệu chuẩn mẫu và phông lệch hơn 0,05 kênh. Cần căn chỉnh hai phổ trước khi trừ; app chưa tự rebin.");
        }
    }
    public void SetBackground(Spectrum background,double scale,bool allowChannelOnly=false)
    {
        if(!double.IsFinite(scale)||scale<=0||scale>1e12)throw new CnfException("Hệ số trừ phông phải dương, hữu hạn và không quá 1e12.");
        CheckAlignment(Sample,background,allowChannelOnly);
        Background=background;Scale=scale;ChannelOnlyBackground=allowChannelOnly;Recalculate();
    }
    public void ClearBackground() { Background=null;Scale=0;ChannelOnlyBackground=false;Recalculate(); }
    private void Recalculate()
    {
        Values=new double[Sample.Counts.Length];Variances=new double[Values.Length];
        for(int i=0;i<Values.Length;i++)
        {
            double b=Background?.Counts[i]??0;
            Values[i]=Sample.Counts[i]-Scale*b;
            Variances[i]=Sample.Counts[i]+Scale*Scale*b;
        }
    }
    public void ExportCsv(string path)
    {
        using var w=new StreamWriter(path,false,new UTF8Encoding(true));
        w.WriteLine("channel,energy_keV,sample_counts,scaled_background,net_counts,sigma_counts");
        string F(double x)=>x.ToString("G17",CultureInfo.InvariantCulture);
        for(int i=0;i<Values.Length;i++) w.WriteLine($"{i},{(Sample.Energy(i) is double e?F(e):"")},{Sample.Counts[i]},{F(Scale*(Background?.Counts[i]??0))},{F(Values[i])},{F(Math.Sqrt(Variances[i]))}");
    }
}
