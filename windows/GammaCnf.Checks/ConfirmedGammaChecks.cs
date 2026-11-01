using GammaCnf.Core;
internal static class ConfirmedGammaChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var session=new AnalysisSession(CnfReader.Decode(AnalysisChecks.Fixture()));
        var roi=RoiAnalysis.Fit(session,new(270,330));double e=roi.EnergyKeV!.Value;
        var choice=new NuclideSuggestion(1,"SYNTHETIC",e,e,0,2,85.1,0,"TEST EVALUATION","test.csv");
        var confirmed=ConfirmedGammaLine.Create(session,roi,choice);var gamma=confirmed.Emission(session,roi);
        check(Math.Abs(gamma.Probability-.851)<1e-12&&gamma.EnergyKeV==e,"Confirmed library percent becomes fraction exactly once");
        check(gamma.Reference.Contains("test.csv")&&gamma.Reference.Contains("TEST EVALUATION")&&gamma.Reference.Contains("chưa cung cấp sai số"),"Library gamma retains source and unknown uncertainty provenance");
        void Reject(Action action,string name){try{action();}catch(CnfException){check(true,name);return;}throw new Exception("Expected rejection: "+name);}
        foreach(double? p in new double?[]{null,0,-1,100.1,double.NaN})
            Reject(()=>ConfirmedGammaLine.Create(session,roi,choice with{IntensityPercent=p}).Emission(session,roi),"Do not invent library Pgamma: "+p);
        check(ConfirmedGammaLine.Create(session,roi,choice with{IntensityPercent=100}).Emission(session,roi).Probability==1,"Accept 100 percent gamma probability");
        Reject(()=>ConfirmedGammaLine.Create(session,roi,choice with{LibraryKeV=double.NaN}),"Reject non-finite library energy in confirmation");
        Reject(()=>ConfirmedGammaLine.Create(session,roi,choice with{MeasuredKeV=e+1}),"Reject stale measured energy before confirming line");
        Reject(()=>confirmed.Emission(session,roi with{Range=new(271,330)}),"Confirmation cannot transfer to another ROI");
        var other=new AnalysisSession(session.Sample with{Counts=session.Sample.Counts.Select(c=>c+1).ToArray()});
        Reject(()=>confirmed.Emission(other,roi),"Confirmation cannot transfer to another measurement");
        Reject(()=>confirmed.Emission(new AnalysisSession(session.Sample),roi),"Confirmation is scoped to the original analysis session even for identical spectra");
        session.SetCalibration([1,.5,0,0]);Reject(()=>confirmed.Emission(session,roi),"Analysis revision invalidates prior gamma confirmation");
    }
}
