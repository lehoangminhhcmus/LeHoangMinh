using System.Globalization;
using System.Text;

namespace GammaCnf.Core;

public sealed record RoiRange(int Start,int End);
public sealed record RoiResult(RoiRange Range,bool Success,string Status,double NetRoiSum,
    double? PeakArea=null,double? AreaError=null,double? AreaWithinRoi=null,double? Centroid=null,
    double? CentroidError=null,double? FwhmChannels=null,double? FwhmKeV=null,double? EnergyKeV=null,
    double? ReducedChiSquare=null,double[]? Parameters=null,RoiCountSummary? Counts=null,string Origin="Manual",string DetectionNote="")
{
    public double Model(double channel) => Parameters is { } p ? RoiAnalysis.Model(p,channel,(Range.Start+Range.End)/2.0) : double.NaN;
}

public static class RoiAnalysis
{
    private const double Root2Pi=2.5066282746310005;
    // Standard-normal CDF, Abramowitz-Stegun 7.1.26 (absolute error < 7.5e-8).
    private static double Cdf(double z)
    {
        double x=Math.Abs(z)/Math.Sqrt(2), t=1/(1+.3275911*x);
        double erf=1-(((((1.061405429*t-1.453152027)*t)+1.421413741)*t-.284496736)*t+.254829592)*t*Math.Exp(-x*x);
        return .5*(1+Math.CopySign(erf,z));
    }
    private static double Phi(double z)=>Math.Exp(-.5*z*z)/Root2Pi;
    private static double Bin(double x,double mu,double sigma)=>Cdf((x+.5-mu)/sigma)-Cdf((x-.5-mu)/sigma);
    public static double Model(double[] p,double x,double middle)=>p[0]*Bin(x,p[1],p[2])+p[3]+p[4]*(x-middle);
    private static double[] Derivatives(double[] p,double x,double mid)
    {
        double a=(x-.5-p[1])/p[2], b=(x+.5-p[1])/p[2];
        return [Bin(x,p[1],p[2]),p[0]*(Phi(a)-Phi(b))/p[2],p[0]*(a*Phi(a)-b*Phi(b))/p[2],1,x-mid];
    }
    private static double[]? Solve(double[,] matrix,double[] rhs)
    {
        int n=rhs.Length;var a=(double[,])matrix.Clone();var b=(double[])rhs.Clone();
        for(int c=0;c<n;c++)
        {
            int pivot=c;for(int r=c+1;r<n;r++)if(Math.Abs(a[r,c])>Math.Abs(a[pivot,c]))pivot=r;
            if(!double.IsFinite(a[pivot,c])||Math.Abs(a[pivot,c])<1e-18)return null;
            if(pivot!=c) { for(int j=0;j<n;j++)(a[c,j],a[pivot,j])=(a[pivot,j],a[c,j]);(b[c],b[pivot])=(b[pivot],b[c]); }
            double d=a[c,c];for(int j=c;j<n;j++)a[c,j]/=d;b[c]/=d;
            for(int r=0;r<n;r++)if(r!=c)
            { double f=a[r,c];for(int j=c;j<n;j++)a[r,j]-=f*a[c,j];b[r]-=f*b[c]; }
        }
        return b.All(double.IsFinite)?b:null;
    }
    public static RoiResult Fit(AnalysisSession session,RoiRange roi)
    {
        var y=session.Values;var variance=session.Variances;
        if(roi.Start<0||roi.End<roi.Start||roi.End>=y.Length)
            return new(roi,false,"ROI không nằm trong phổ hoặc bị đảo.",0);
        var counts=RoiCountSummary.Calculate(session,roi);
        double net=counts.Net;
        RoiResult Fail(string message)=>new(roi,false,message,net,Counts:counts);
        if(roi.End-roi.Start<8||roi.End-roi.Start>4095)
            return Fail("Có tổng số đếm; fit Gaussian cần 9–4096 kênh.");
        int n=roi.End-roi.Start+1;double mid=(roi.Start+roi.End)/2.0;
        double low=Enumerable.Range(roi.Start,3).Average(i=>y[i]),high=Enumerable.Range(roi.End-2,3).Average(i=>y[i]);
        double slope=(high-low)/(n-3), baseline=(low+high)/2;
        double area=0, highest=double.NegativeInfinity;int peak=roi.Start;
        for(int i=roi.Start;i<=roi.End;i++)
        {
            double signal=y[i]-baseline-slope*(i-mid);area+=Math.Max(0,signal);
            if(signal>highest) { highest=signal;peak=i; }
        }
        if(highest<=0||area<=0)return Fail("Không có đỉnh dương trên nền tuyến tính.");
        double sigma=Math.Clamp(area/(highest*Root2Pi),.6,n/5.0);
        double[] p=[area,peak,sigma,baseline,slope];
        double Cost(double[] q)
        {
            if(q.Any(v=>!double.IsFinite(v))||q[0]<=0||q[1]<=roi.Start||q[1]>=roi.End||q[2]<.35||q[2]>n/2.0)return double.PositiveInfinity;
            double sum=0;for(int i=roi.Start;i<=roi.End;i++){double d=y[i]-Model(q,i,mid);sum+=d*d/Math.Max(1,variance[i]);}return sum;
        }
        (double[,],double[]) Normal(double[] q)
        {
            var a=new double[5,5];var b=new double[5];
            for(int i=roi.Start;i<=roi.End;i++)
            {
                double weight=1/Math.Max(1,variance[i]),residual=y[i]-Model(q,i,mid);double[] d=Derivatives(q,i,mid);
                for(int j=0;j<5;j++){b[j]+=weight*d[j]*residual;for(int k=0;k<5;k++)a[j,k]+=weight*d[j]*d[k];}
            }
            return(a,b);
        }
        double cost=Cost(p),lambda=.001;bool converged=false;
        if(!double.IsFinite(cost))return Fail("Đỉnh nằm ở biên ROI; hãy mở rộng vùng.");
        for(int iteration=0;iteration<250;iteration++)
        {
            var (normal,gradient)=Normal(p);
            var damped=(double[,])normal.Clone();for(int j=0;j<5;j++)damped[j,j]+=lambda*Math.Max(normal[j,j],1e-12);
            double[]? step=Solve(damped,gradient);if(step is null)break;
            double[] candidate=p.Zip(step,(v,d)=>v+d).ToArray();double next=Cost(candidate);
            if(next<=cost)
            {
                double improvement=cost-next;
                p=candidate;cost=next;lambda=Math.Max(1e-12,lambda/3);
                if(improvement<1e-9*(1+cost)&&step.Select((d,j)=>Math.Abs(d)/(1+Math.Abs(p[j]))).Max()<1e-5){converged=true;break;}
            }
            else { lambda*=10;if(lambda>1e15)break; }
        }
        if(!converged)return Fail("Fit không hội tụ; điều chỉnh ROI hoặc kiểm tra đỉnh chồng.");
        var (information,_)=Normal(p);var covariance=new double[5,5];
        for(int i=0;i<5;i++)
        {
            var unit=new double[5];unit[i]=1;var col=Solve(information,unit);
            if(col is null)return Fail("Không xác định được sai số fit (ma trận suy biến).");
            for(int j=0;j<5;j++)covariance[j,i]=col[j];
        }
        if(covariance[0,0]<=0||covariance[1,1]<=0||covariance[2,2]<=0)return Fail("Sai số fit không hợp lệ.");
        double areaError=Math.Sqrt(covariance[0,0]);
        if(p[0]/areaError<3)return Fail("Đỉnh không đủ ý nghĩa thống kê (diện tích < 3σ).");
        double fwhm=2*Math.Sqrt(2*Math.Log(2))*p[2];
        if(p[1]-fwhm/2<=roi.Start||p[1]+fwhm/2>=roi.End||p[2]<=.351||p[2]>=n/2.01)
            return Fail("Đỉnh/độ rộng chạm giới hạn ROI; hãy mở rộng vùng.");
        double reduced=cost/(n-5);
        double? e=session.Sample.Energy(p[1]);
        double? width=e.HasValue ? session.Sample.Energy(p[1]+fwhm/2)-session.Sample.Energy(p[1]-fwhm/2) : null;
        string status=reduced>3 ? "Cảnh báo: χ²/ν > 3; có thể có đỉnh chồng hoặc mô hình nền không phù hợp." : "Fit hội tụ";
        return new(roi,true,status,net,p[0],areaError,p[0]*(Cdf((roi.End+.5-p[1])/p[2])-Cdf((roi.Start-.5-p[1])/p[2])),
            p[1],Math.Sqrt(covariance[1,1]),fwhm,width,e,reduced,p,counts);
    }

    public static void ExportReport(string path,AnalysisSession session,string samplePath,string? backgroundPath,string scaleMode,IEnumerable<RoiResult> results)
    {
        using var w=new StreamWriter(path,false,new UTF8Encoding(true));
        string F(double? x)=>x?.ToString("G12",CultureInfo.InvariantCulture)??"NA";
        string Clean(string s)=>s.Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');
        w.WriteLine("Gamma CNF Reader 0.6.0 — ROI report (UTF-8, tab separated)");
        w.WriteLine("Exported_UTC\t"+DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture));
        w.WriteLine("Sample\t"+Clean(samplePath));w.WriteLine("Sample_live_seconds\t"+F(session.Sample.LiveTime));
        w.WriteLine("Background\t"+Clean(backgroundPath??"None"));w.WriteLine("Background_live_seconds\t"+F(session.Background?.LiveTime));
        w.WriteLine("Background_scale\t"+F(session.Scale));
        w.WriteLine("Background_original_seconds\t"+F(session.BackgroundOriginalSeconds));w.WriteLine("Background_target_seconds\t"+F(session.BackgroundTargetSeconds));w.WriteLine("Scale_mode\t"+Clean(scaleMode));
        w.WriteLine("Channel_only_background_confirmed\t"+session.ChannelOnlyBackground);
        w.WriteLine("Calibration_a0_a1_a2_a3_keV\t"+(session.Sample.Calibration is null?"NA":string.Join("\t",session.Sample.Calibration.Select(x=>F(x)))));
        w.WriteLine("Method\tWeighted least squares: bin-integrated single Gaussian + linear continuum. Channels start at 0; ROI bounds inclusive.");
        w.WriteLine("Variance\tsample + scale^2 * background; floor 1 for fitting. Scale treated as exact. Errors: approximate 1-sigma covariance; low-count spectra may need Poisson methods.");
        w.WriteLine("Peak_area\tFull fitted Gaussian area; Area_in_ROI excludes fitted tails outside ROI. Gross_ROI_sum is the raw sample sum; scaled background and net sums are direct inclusive channel sums with no continuum removal. Net_ROI_sum includes residual continuum. Unresolved multiplets are not separated.");
        w.WriteLine("ID\tStart_channel\tEnd_channel\tSuccess\tNet_ROI_sum\tPeak_area\tArea_1sigma\tArea_in_ROI\tCentroid_channel\tCentroid_1sigma\tEnergy_keV\tFWHM_channel\tFWHM_keV\tChi2_per_dof\tStatus\tGross_ROI_sum\tScaled_background_ROI_sum\tNet_ROI_sigma\tOrigin\tDetection_note");
        int id=0;foreach(var r in results)w.WriteLine(string.Join("\t",++id,r.Range.Start,r.Range.End,r.Success,F(r.NetRoiSum),F(r.PeakArea),F(r.AreaError),F(r.AreaWithinRoi),F(r.Centroid),F(r.CentroidError),F(r.EnergyKeV),F(r.FwhmChannels),F(r.FwhmKeV),F(r.ReducedChiSquare),Clean(r.Status),r.Counts?.Gross.ToString(CultureInfo.InvariantCulture)??"NA",F(r.Counts?.ScaledBackground),F(r.Counts?.NetSigma),Clean(r.Origin),Clean(r.DetectionNote)));
    }
}
