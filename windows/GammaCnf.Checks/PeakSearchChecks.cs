using GammaCnf.Core;
internal static class PeakSearchChecks
{
    public static void Run(Action<bool,string> check,string? samplePath)
    {
        void Reject(Action a,string name){try{a();}catch(CnfException){check(true,name);return;}throw new Exception("Expected rejection: "+name);}
        var options=new PeakSearchOptions();
        var flat=Enumerable.Repeat(100d,1024).ToArray();
        check(PeakSearch.Find(flat,flat,options).Peaks.Count==0,"Peak search rejects flat continuum");
        var ramp=Enumerable.Range(0,1024).Select(i=>100d+i*.25).ToArray();
        check(PeakSearch.Find(ramp,ramp,options).Peaks.Count==0,"Peak search rejects linear sloping continuum including edges");
        double[] Signal(params (double Center,double Sigma,double Height)[] peaks)=>Enumerable.Range(0,1024).Select(i=>100d+.05*i+peaks.Sum(p=>p.Height*Math.Exp(-.5*Math.Pow((i-p.Center)/p.Sigma,2)))).ToArray();
        var data=Signal((300.3,4.2,1000));var found=PeakSearch.Find(data,data,options);
        check(found.Peaks.Count==1&&Math.Abs(found.Peaks[0].Channel-300.3)<2,"Find one Gaussian without duplicate scale-space detections");
        check(Math.Abs(found.Peaks[0].FwhmChannels-9.89)<1,"Estimate Gaussian width for automatic ROI");
        check(found.Rois.Count==1&&found.Rois[0].Range.Start<288&&found.Rois[0].Range.End>312,"Automatic ROI includes peak and continuum sidebands");
        var baseSpectrum=CnfReader.Decode(AnalysisChecks.Fixture());
        var session=new AnalysisSession(baseSpectrum with{Counts=data.Select(v=>(uint)Math.Round(v)).ToArray()});
        var fit=PeakSearch.Analyze(session,found.Rois[0]);
        check(fit.Success&&Math.Abs(fit.Centroid!.Value-300.3)<.1&&fit.Origin=="Automatic"&&fit.Counts is not null,"Automatic ROI supports Gaussian fit and direct totals");
        data=Signal((180,2,800),(480,5,500),(800,10,300));found=PeakSearch.Find(data,data,options);
        check(found.Peaks.Count==3&&new[]{180,480,800}.All(c=>found.Peaks.Any(p=>Math.Abs(p.Channel-c)<3)),"Find separated peaks with different widths");
        data=Signal((300,3,1000),(318,3,800));found=PeakSearch.Find(data,data,options);
        check(found.Peaks.Any(p=>Math.Abs(p.Channel-300)<3)&&found.Peaks.Any(p=>Math.Abs(p.Channel-318)<3),"Detect nearby resolved local maxima");
        check(found.Rois.Any(r=>r.IsGroup),"Group overlapping automatic ROI instead of double-counting");
        var group=found.Rois.First(r=>r.IsGroup);var grouped=PeakSearch.Analyze(session,group);
        check(!grouped.Success&&grouped.Counts is not null&&grouped.DetectionNote.Contains("grouped"),"Do not present single-Gaussian quantification for automatic peak groups");
        data=Signal((1,3,1000));found=PeakSearch.Find(data,data,options);
        check(found.Peaks.Count>0&&found.Rois.All(r=>r.Range.Start>=0&&r.Range.End<1024),"Edge peak detection keeps ROI in valid channel bounds");
        var net=Signal((500,4,700)).Select(v=>v-300).ToArray();var variance=Enumerable.Repeat(500d,1024).ToArray();
        found=PeakSearch.Find(net,variance,options);
        check(found.Peaks.Any(p=>Math.Abs(p.Channel-500)<2),"Detect peak on negative continuum after background subtraction");
        check(PeakSearch.Find(Enumerable.Repeat(-100d,1024).ToArray(),variance,options).Peaks.Count==0,"Negative flat net spectrum does not generate peaks");
        var merge=PeakSearch.Merge(found.Rois,[found.Rois[0].Range]);check(merge.Accepted.Count==0&&merge.SkippedOverlap>0,"Preserve existing manual ROI when automatic proposals overlap");
        merge=PeakSearch.Merge(found.Rois,[new RoiRange(0,10)],1);check(merge.Accepted.Count==0&&merge.SkippedCapacity>0,"Automatic ROI respects document capacity");
        data=Signal((180,2,800),(480,5,500),(800,10,300));found=PeakSearch.Find(data,data,options with{MaximumPeaks=1});
        check(found.Peaks.Count==1&&found.Truncated,"Report automatic search truncation at peak limit");
        var random=new Random(8347);var noise=Enumerable.Range(0,2048).Select(_=>100+Math.Sqrt(100)*Math.Sqrt(-2*Math.Log(1-random.NextDouble()))*Math.Cos(2*Math.PI*random.NextDouble())).ToArray();
        check(PeakSearch.Find(noise,Enumerable.Repeat(100d,2048).ToArray(),options).Peaks.Count==0,"Seeded counting-noise continuum does not trigger default threshold");
        for(int seed=1;seed<=4;seed++)
        {
            random=new Random(seed);var signal=Signal((250,3.5,400),(740,7,250));
            var noisy=signal.Select(v=>Math.Max(0,v+Math.Sqrt(v)*Math.Sqrt(-2*Math.Log(1-random.NextDouble()))*Math.Cos(2*Math.PI*random.NextDouble()))).ToArray();
            found=PeakSearch.Find(noisy,signal,options);
            check(new[]{250,740}.All(c=>found.Peaks.Any(p=>Math.Abs(p.Channel-c)<4)),"Noisy two-peak detection seed "+seed);
        }
        Reject(()=>PeakSearch.Find(flat,flat,options with{MinimumFwhm=0}),"Reject invalid peak-search width");
        Reject(()=>PeakSearch.Find(flat,flat,options with{Threshold=double.NaN}),"Reject non-finite peak threshold");
        Reject(()=>PeakSearch.Find(flat,[1,2],options),"Reject mismatched peak-search variance array");
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();bool cancelled=false;
        try{PeakSearch.Find(flat,flat,options,cancellation.Token);}catch(OperationCanceledException){cancelled=true;}
        check(cancelled,"Peak search supports cancellation");
        if(samplePath is not null)
        {
            var measured=new AnalysisSession(CnfReader.Read(samplePath));var peaks=PeakSearch.Find(measured.Values,measured.Variances,options);
            check(peaks.Peaks.Any(p=>Math.Abs(p.Channel-1740)<=4),"Private CNF strongest peak found automatically");
            check(peaks.Rois.All(r=>r.Range.Start>=0&&r.Range.End<measured.Values.Length),"Private CNF automatic ROI bounds valid");
            Console.WriteLine($"Private CNF automatic search: {peaks.Peaks.Count} candidates, {peaks.Rois.Count} ROI groups");
        }
        string path=Path.GetTempFileName();try
        {
            RoiAnalysis.ExportReport(path,session,"sample",null,"None",[fit]);var report=File.ReadAllText(path);
            check(report.Contains("Detection_note")&&report.Contains("Automatic")&&report.Contains("multiscale"),"ROI TXT records automatic origin and search parameters");
        }
        finally{File.Delete(path);}
    }
}
