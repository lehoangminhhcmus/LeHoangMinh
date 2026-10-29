using GammaCnf.Core;
internal static class Version5Checks
{
    public static void Run(Action<bool,string> check)
    {
        void Reject(Action a,string name){try{a();}catch(CnfException){check(true,name);return;}throw new Exception("Expected rejection: "+name);}
        var baseSpectrum=CnfReader.Decode(AnalysisChecks.Fixture());
        var sample=baseSpectrum with{Counts=[10,20,30,40,50,60,70,80,90,100],LiveTime=1000,RealTime=1200};
        var bg=sample with{Counts=[1,2,3,4,5,6,7,8,9,10],LiveTime=500,RealTime=1000};
        var a=new AnalysisSession(sample);a.SetTimedBackground(bg);
        check(a.Scale==2&&a.TimeBasis==BackgroundTimeBasis.LiveTime&&a.BackgroundOriginalSeconds==500&&a.BackgroundTargetSeconds==1000,"Automatic background matches sample live time");
        check(a.Values[0]==8&&a.Variances[0]==14,"Timed subtraction scales counts and propagates Poisson variance");
        var other=new AnalysisSession(sample with{LiveTime=2000,RealTime=2400});other.SetTimedBackground(bg);
        check(other.Scale==4&&a.Scale==2&&a.Values[0]==8,"Same background scales independently for different spectra");
        a.SetTimedBackground(bg,BackgroundTimeBasis.RealTime);
        check(a.Scale==1.2&&a.BackgroundOriginalSeconds==1000&&a.BackgroundTargetSeconds==1200,"Explicit real-time background normalization");
        check(bg.LiveTime==500&&bg.RealTime==1000&&bg.Counts[0]==1,"Background source counts and times remain unchanged");
        Reject(()=>a.SetTimedBackground(bg with{LiveTime=null}),"Missing live time rejected instead of silently using scale one");
        check(a.Scale==1.2&&a.TimeBasis==BackgroundTimeBasis.RealTime,"Failed time normalization leaves previous valid subtraction intact");
        Reject(()=>a.SetTimedBackground(bg with{LiveTime=0}),"Reject zero background duration");
        Reject(()=>a.SetTimedBackground(bg with{LiveTime=double.NaN}),"Reject non-finite background duration");
        Reject(()=>a.SetTimedBackground(bg,(BackgroundTimeBasis)42),"Reject invalid time basis");
        a.SetTimedBackground(bg);
        var counts=RoiCountSummary.Calculate(a,new(0,2));
        check(counts.Gross==60&&counts.ScaledBackground==12&&counts.Net==48,"Direct ROI sums include both endpoints and scaled background");
        check(Math.Abs(counts.NetSigma-Math.Sqrt(84))<1e-12,"ROI sum uncertainty equals square root of summed count variances");
        var single=RoiAnalysis.Fit(a,new(1,1));
        check(!single.Success&&single.Counts?.Gross==20&&single.NetRoiSum==16,"One-channel ROI retains count sums without Gaussian fit");
        var fit=RoiAnalysis.Fit(a,new(0,9));
        check(fit.Counts?.Gross==550&&fit.Counts.ScaledBackground==110&&fit.NetRoiSum==440,"ROI sums survive failed or non-peak Gaussian analysis");
        var negative=new AnalysisSession(sample);negative.SetTimedBackground(bg,BackgroundTimeBasis.LiveTime);
        negative.SetBackground(bg,20);
        var nc=RoiCountSummary.Calculate(negative,new(0,2));
        check(nc.Net==-60&&nc.Gross==60&&nc.ScaledBackground==120&&nc.NetSigma>0,"Negative ROI sum preserved after scaled background subtraction");
        Reject(()=>RoiCountSummary.Calculate(a,new(3,1)),"Reject inverted ROI for summation");
        Reject(()=>RoiCountSummary.Calculate(a,new(-1,2)),"Reject out-of-range ROI for summation");
        var large=new AnalysisSession(sample with{Counts=Enumerable.Repeat(uint.MaxValue,10).ToArray()});
        check(RoiCountSummary.Calculate(large,new(0,9)).Gross==10UL*uint.MaxValue,"Gross ROI sum uses UInt64 precision");
        a.ClearBackground();var raw=RoiCountSummary.Calculate(a,new(0,2));
        check(raw.Net==60&&raw.ScaledBackground==0&&Math.Abs(raw.NetSigma-Math.Sqrt(60))<1e-12&&a.TimeBasis is null,"Clear background restores raw ROI sums and timing state");
        string report=Path.GetTempFileName();
        try
        {
            a.SetTimedBackground(bg);RoiAnalysis.ExportReport(report,a,"sample","background","Automatic live-time ratio",[RoiAnalysis.Fit(a,new(0,2))]);
            string text=File.ReadAllText(report);
            check(text.Contains("Gross_ROI_sum")&&text.Contains("Scaled_background_ROI_sum")&&text.Contains("Net_ROI_sigma"),"TXT report includes raw, scaled background and net ROI sum uncertainties");
            check(text.Contains("Background_original_seconds\t500")&&text.Contains("Background_target_seconds\t1000"),"TXT report records automatic background time normalization");
            check(text.Contains("False")&&text.Contains("\t60\t12\t"),"TXT exports sums even when Gaussian fit is unavailable");
        }
        finally{File.Delete(report);}
    }
}
