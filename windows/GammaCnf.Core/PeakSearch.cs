using System.Globalization;
namespace GammaCnf.Core;

public sealed record PeakSearchOptions(double MinimumFwhm=3,double MaximumFwhm=40,double Threshold=5,double RoiSigma=3.5,int MaximumPeaks=100)
{
    public void Validate()
    {
        if(!double.IsFinite(MinimumFwhm)||!double.IsFinite(MaximumFwhm)||MinimumFwhm<2||MaximumFwhm>256||MaximumFwhm<MinimumFwhm)
            throw new CnfException("Khoảng FWHM dò đỉnh phải từ 2 đến 256 kênh, min ≤ max.");
        if(!double.IsFinite(Threshold)||Threshold<3||Threshold>30)throw new CnfException("Ngưỡng SNR bộ lọc phải từ 3 đến 30 (khuyến nghị 5).");
        if(!double.IsFinite(RoiSigma)||RoiSigma<2.5||RoiSigma>6||MaximumPeaks<1||MaximumPeaks>100)
            throw new CnfException("Biên ROI cần 2,5–6 sigma; tối đa 1–100 đỉnh.");
    }
    public string Description=>FormattableString.Invariant($"multiscale Gaussian/linear-continuum filter; FWHM={MinimumFwhm:G6}..{MaximumFwhm:G6} ch; SNR>={Threshold:G6}; ROI={RoiSigma:G6} sigma");
}
public sealed record PeakCandidate(int Channel,double FilterSnr,double FwhmChannels,bool Clipped);
public sealed record DetectedRoi(RoiRange Range,IReadOnlyList<PeakCandidate> Peaks,PeakSearchOptions Options)
{
    public bool IsGroup=>Peaks.Count>1;
    public string Note=>Options.Description+"; candidates="+string.Join("; ",Peaks.Select(p=>FormattableString.Invariant($"ch {p.Channel}, filter SNR {p.FilterSnr:G5}, estimated FWHM {p.FwhmChannels:G5}")))+
        (IsGroup?"; overlapping automatic ROI grouped: single-Gaussian fit disabled":"")+(Peaks.Any(p=>p.Clipped)?"; ROI/search support touches spectrum edge":"");
}
public sealed record PeakSearchResult(IReadOnlyList<PeakCandidate> Peaks,IReadOnlyList<DetectedRoi> Rois,int SuppressedCandidates,bool Truncated);
public sealed record RoiMergeResult(IReadOnlyList<DetectedRoi> Accepted,int SkippedOverlap,int SkippedCapacity);

public static class PeakSearch
{
    private const double FwhmFactor=2.3548200450309493;
    public static PeakSearchResult Find(double[] values,double[] variances,PeakSearchOptions options,CancellationToken cancellation=default)
    {
        options.Validate();
        if(values.Length!=variances.Length||values.Length<9||values.Length>32768||values.Any(v=>!double.IsFinite(v))||variances.Any(v=>!double.IsFinite(v)||v<0))
            throw new CnfException("Dữ liệu dò đỉnh cần 9–32768 kênh và variance hữu hạn, không âm.");
        int n=values.Length;double minSigma=options.MinimumFwhm/FwhmFactor;
        var smooth=Smooth(values,minSigma,cancellation);
        var anchors=new List<int>();
        for(int i=0;i<n;i++)if((i==0||smooth[i]>smooth[i-1])&&(i==n-1||smooth[i]>=smooth[i+1]))anchors.Add(i);
        var best=new Dictionary<int,PeakCandidate>();
        var widths=new List<double>();
        for(double w=options.MinimumFwhm;w<options.MaximumFwhm;w*=1.4)widths.Add(w);
        if(widths.Count==0||Math.Abs(widths[^1]-options.MaximumFwhm)>.01)widths.Add(options.MaximumFwhm);
        foreach(double width in widths)
        {
            cancellation.ThrowIfCancellationRequested();
            double sigma=width/FwhmFactor;int radius=Math.Max(4,(int)Math.Ceiling(4*sigma));
            var gaussian=Enumerable.Range(-radius,2*radius+1).Select(t=>Math.Exp(-.5*t*t/(sigma*sigma))).ToArray();
            var z=new double[n];
            for(int center=0;center<n;center++)
            {
                if((center&127)==0)cancellation.ThrowIfCancellationRequested();
                int a=Math.Max(-radius,-center),b=Math.Min(radius,n-1-center),count=b-a+1;
                // Project the Gaussian against constant and linear terms, including at edges.
                double sx=0,sxx=0,sg=0,sxg=0;
                for(int t=a;t<=b;t++){double g=gaussian[t+radius];sx+=t;sxx+=(double)t*t;sg+=g;sxg+=t*g;}
                double det=count*sxx-sx*sx;
                if(det<=0)continue;
                double base0=(sg*sxx-sxg*sx)/det,base1=(count*sxg-sx*sg)/det;
                double signal=0,variance=0;
                for(int t=a;t<=b;t++)
                {
                    double weight=gaussian[t+radius]-base0-base1*t;
                    signal+=weight*values[center+t];variance+=weight*weight*Math.Max(1,variances[center+t]);
                }
                z[center]=variance>0?signal/Math.Sqrt(variance):0;
            }
            for(int i=0;i<n;i++)
            {
                if(z[i]<options.Threshold||(i>0&&z[i]<z[i-1])||(i<n-1&&z[i]<=z[i+1]))continue;
                int anchor=-1;double nearest=double.PositiveInfinity;
                foreach(int candidate in anchors)
                {
                    double distance=Math.Abs(candidate-i);
                    if(distance<=Math.Max(2,width/2)&&distance<nearest){anchor=candidate;nearest=distance;}
                }
                if(anchor<0)continue;
                var peak=new PeakCandidate(anchor,z[i],width,anchor-radius<0||anchor+radius>=n);
                if(!best.TryGetValue(anchor,out var previous)||peak.FilterSnr>previous.FilterSnr)best[anchor]=peak;
            }
        }
        var selected=new List<PeakCandidate>();int suppressed=0;
        foreach(var candidate in best.Values.OrderByDescending(p=>p.FilterSnr).ThenBy(p=>p.Channel))
        {
            cancellation.ThrowIfCancellationRequested();
            var estimated=Estimate(candidate,smooth,options);
            if(selected.Any(p=>Math.Abs(p.Channel-estimated.Channel)<Math.Max(2,.65*Math.Min(p.FwhmChannels,estimated.FwhmChannels)))){suppressed++;continue;}
            selected.Add(estimated);
        }
        bool truncated=selected.Count>options.MaximumPeaks;
        selected=selected.Take(options.MaximumPeaks).OrderBy(p=>p.Channel).ToList();
        var rois=new List<DetectedRoi>();
        foreach(var peak in selected)
        {
            int half=Math.Max(4,(int)Math.Ceiling(options.RoiSigma*peak.FwhmChannels/FwhmFactor));
            int start=Math.Max(0,peak.Channel-half),end=Math.Min(n-1,peak.Channel+half);
            // Ensure a useful minimum fit span when the peak is close to a boundary.
            if(end-start<8){start=Math.Max(0,Math.Min(start,n-9));end=Math.Min(n-1,Math.Max(end,start+8));}
            var roi=new DetectedRoi(new(start,end),[peak],options);
            if(rois.Count>0&&roi.Range.Start<=rois[^1].Range.End)
            {
                var previous=rois[^1];
                rois[^1]=new(new(previous.Range.Start,Math.Max(previous.Range.End,roi.Range.End)),previous.Peaks.Concat(roi.Peaks).ToArray(),options);
            }
            else rois.Add(roi);
        }
        return new(selected,rois,suppressed,truncated);
    }
    private static double[] Smooth(double[] values,double sigma,CancellationToken cancellation)
    {
        int radius=Math.Max(2,(int)Math.Ceiling(3*sigma));var kernel=Enumerable.Range(-radius,2*radius+1).Select(i=>Math.Exp(-.5*i*i/(sigma*sigma))).ToArray();
        var result=new double[values.Length];
        for(int i=0;i<values.Length;i++)
        {
            if((i&255)==0)cancellation.ThrowIfCancellationRequested();double sum=0,weight=0;
            for(int t=Math.Max(-radius,-i);t<=Math.Min(radius,values.Length-1-i);t++){sum+=kernel[t+radius]*values[i+t];weight+=kernel[t+radius];}
            result[i]=sum/weight;
        }
        return result;
    }
    private static PeakCandidate Estimate(PeakCandidate candidate,double[] smooth,PeakSearchOptions options)
    {
        int center=candidate.Channel,radius=Math.Max(5,(int)Math.Ceiling(candidate.FwhmChannels*1.7));
        int a=Math.Max(0,center-radius),b=Math.Min(smooth.Length-1,center+radius);
        double Baseline(int x)=>smooth[a]+(smooth[b]-smooth[a])*(x-a)/Math.Max(1,b-a);
        double height=smooth[center]-Baseline(center);
        if(height<=0)return candidate;
        double half=height/2;int lo=center,hi=center;
        while(lo>a&&smooth[lo]-Baseline(lo)>half)lo--;
        while(hi<b&&smooth[hi]-Baseline(hi)>half)hi++;
        bool missingLeft=lo==center||smooth[lo]-Baseline(lo)>half,missingRight=hi==center||smooth[hi]-Baseline(hi)>half;
        if(missingLeft||missingRight)return candidate with{Clipped=true};
        double leftHeight=smooth[lo]-Baseline(lo),rightHeight=smooth[hi]-Baseline(hi);
        double xleft=lo+(half-leftHeight)/Math.Max(1e-12,smooth[lo+1]-Baseline(lo+1)-leftHeight);
        double xright=hi-(half-rightHeight)/Math.Max(1e-12,smooth[hi-1]-Baseline(hi-1)-rightHeight);
        // Remove only the known smoothing broadening, not any instrument width.
        double broadened=Math.Max(1,xright-xleft),smoothFwhm=options.MinimumFwhm;
        double width=Math.Sqrt(Math.Max(options.MinimumFwhm*options.MinimumFwhm,broadened*broadened-smoothFwhm*smoothFwhm));
        return candidate with{FwhmChannels=Math.Clamp(width,options.MinimumFwhm,options.MaximumFwhm)};
    }
    public static RoiMergeResult Merge(IReadOnlyList<DetectedRoi> proposed,IEnumerable<RoiRange> existing,int capacity=100)
    {
        if(capacity<1||capacity>100)throw new CnfException("Giới hạn ROI phải từ 1 đến 100.");
        var occupied=existing.ToList();var accepted=new List<DetectedRoi>();int overlap=0,full=0;
        foreach(var roi in proposed)
        {
            if(occupied.Any(r=>roi.Range.Start<=r.End&&r.Start<=roi.Range.End)){overlap++;continue;}
            if(occupied.Count>=capacity){full++;continue;}
            accepted.Add(roi);occupied.Add(roi.Range);
        }
        return new(accepted,overlap,full);
    }
    public static RoiResult Analyze(AnalysisSession session,DetectedRoi roi)
    {
        RoiResult result;
        if(roi.IsGroup)
        {
            var counts=RoiCountSummary.Calculate(session,roi.Range);
            result=new(roi.Range,false,$"Nhóm {roi.Peaks.Count} đỉnh/ROI chồng: chỉ tổng số đếm; cần chia ROI hoặc mô hình nhiều đỉnh.",counts.Net,Counts:counts);
        }
        else result=RoiAnalysis.Fit(session,roi.Range);
        return result with{Origin="Automatic",DetectionNote=roi.Note};
    }
}
