using GammaCnf.Core;

internal static class EfficiencyChecks
{
    public static MeasurementGeometry Geometry=>new("HPGe-DEMO","Point source at 100 mm",100,"SYNTHETIC TEST ONLY");
    public static StandardSource Source=>new("DEMO-001","Demo-100","SYNTHETIC — not a real certificate",10000,200,new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero),100,.05,1,"SYNTHETIC nuclear data v1");
    public static double TrueEfficiency(double e)=>Math.Exp(Math.Log(.01)-.8*Math.Log(e/1000)+.02*Math.Pow(Math.Log(e/1000),2));
    public static EfficiencyObservation[] Points()
    {
        double[] energies=[100,200,400,800,1400];
        return energies.Select((e,i)=>
        {
            var g=new GammaEmission(e,.5,.005,"SYNTHETIC nuclear data v1");
            DateTimeOffset at=Source.ReferenceDate.AddDays(100);
            double area=RadioactiveDecay.Exposure(Source,at,1000,1200)*g.Probability*TrueEfficiency(e);
            return new EfficiencyObservation(Geometry,Source,g,at,1000,1200,area,Math.Sqrt(area),e,2,1,30*i,30*i+20,"SYNTHETIC-CNF-ID","synthetic.cnf",null,0);
        }).ToArray();
    }
    public static void Run(Action<bool,string> check)
    {
        void Reject(Action a,string name){try{a();}catch(CnfException){check(true,name);return;}throw new Exception("Expected rejection: "+name);}
        var identitySpectrum=CnfReader.Decode(AnalysisChecks.Fixture());
        check(MeasurementIdentity.Create(identitySpectrum)==MeasurementIdentity.Create(identitySpectrum with{Calibration=[0,1,0,0],Title="renamed"}),"Measurement identity survives energy calibration/title edits");
        var different=(uint[])identitySpectrum.Counts.Clone();different[0]++;
        check(MeasurementIdentity.Create(identitySpectrum)!=MeasurementIdentity.Create(identitySpectrum with{Counts=different}),"Measurement identity changes for different counts");
        var points=Points();var p=EfficiencyProfile.Fit("Synthetic test",Geometry,points,2);
        check(Math.Abs(RadioactiveDecay.AverageFactor(100,100*86400)-.5/Math.Log(2))<1e-12,"Decay averaged over one half-life");
        check(Math.Abs(RadioactiveDecay.AverageFactor(1e10,1)-1)<1e-12,"Stable decay factor for long half-life");
        check(Math.Abs(RadioactiveDecay.Exposure(Source,Source.ReferenceDate.AddDays(100),1000,1200)/(5000*1000*RadioactiveDecay.AverageFactor(100,1200))-1)<1e-12,"Certificate activity corrected for one half-life");
        check(RadioactiveDecay.Exposure(Source,points[0].MeasuredAt.ToOffset(TimeSpan.FromHours(7)),1000,1200)==RadioactiveDecay.Exposure(Source,points[0].MeasuredAt,1000,1200),"Time-zone invariant decay correction");
        check(Math.Abs((points[0] with{Area=points[0].Area/1.1,Correction=1.1,CorrectionSigma=.01,CorrectionNotes="Synthetic correction"}).Efficiency/points[0].Efficiency-1)<1e-12,"Traceable calibration peak correction factor");
        Reject(()=>(points[0] with{Correction=1.1}).Validate(),"Reject undocumented standard peak correction");
        Reject(()=>(Source with{CoverageFactor=.5}).Validate(),"Reject certificate coverage factor below one");
        Reject(()=>ActivityAnalysis.Calculate(p,new(new(1,20),false,"fail",0),new("x",points[0].Gamma,Geometry,"other",Source.ReferenceDate,Source.ReferenceDate,1,1,1,0,1,0,"",null,0)),"Reject invalid ROI in activity calculation");
        check(points.All(o=>Math.Abs(o.Efficiency/TrueEfficiency(o.Gamma.EnergyKeV)-1)<1e-12),"Efficiency point from area, activity, probability and counting time");
        check(Math.Abs(p.Coefficients[0]-Math.Log(.01))<1e-8&&Math.Abs(p.Coefficients[1]+.8)<1e-8&&Math.Abs(p.Coefficients[2]-.02)<1e-8,"Recover known log-polynomial efficiency curve");
        check(Math.Abs(p.Evaluate(600).Value/TrueEfficiency(600)-1)<1e-8,"Interpolate efficiency between standard lines");
        check(p.Evaluate(600).Sigma/p.Evaluate(600).Value>.02,"Shared certificate uncertainty does not shrink below 2 percent");
        check(p.ChiSquarePerDof<1e-10&&p.ResidualSigma.All(x=>Math.Abs(x)<1e-6),"GLS residuals and degrees of freedom");
        Reject(()=>p.Evaluate(99),"Reject low-energy extrapolation");Reject(()=>p.Evaluate(1401),"Reject high-energy extrapolation");
        Reject(()=>EfficiencyProfile.Fit("Too few",Geometry,points.Take(3),2),"Reject underdetermined efficiency curve");
        Reject(()=>EfficiencyProfile.Fit("Duplicate",Geometry,points.Append(points[0]),2),"Reject duplicated ROI as independent point");
        Reject(()=>EfficiencyProfile.Fit("Mismatch",Geometry,points.Select((o,i)=>i==0?o with{Geometry=Geometry with{DistanceMm=50}}:o),2),"Reject mixed calibration geometry");
        Reject(()=>EfficiencyProfile.Fit("Certificate conflict",Geometry,points.Select((o,i)=>i==0?o with{Source=Source with{ActivityBq=20000}}:o),2),"Reject inconsistent source certificate data");
        Reject(()=>(points[0] with{Gamma=points[0].Gamma with{Probability=2}}).Validate(),"Reject gamma probability outside fraction range");
        Reject(()=>(points[0] with{ReducedChiSquare=4}).Validate(),"Reject poor ROI for efficiency calibration");
        Reject(()=>(points[0] with{CentroidKeV=110}).Validate(),"Reject incorrectly assigned gamma line");
        Reject(()=>(points[0] with{LiveSeconds=1300}).Validate(),"Reject live time greater than real time");
        Reject(()=>(points[0] with{Area=1e12}).Validate(),"Reject efficiency above one");
        var roirange=new RoiRange(390,410);double energy=400,live=900,real=1000,truth=2000;
        var gamma=points[2].Gamma;var at=Source.ReferenceDate.AddDays(200);
        double area2=truth*live*gamma.Probability*TrueEfficiency(energy)*RadioactiveDecay.AverageFactor(100,real);
        var roi=new RoiResult(roirange,true,"Synthetic",area2,area2,Math.Sqrt(area2),area2,400,.05,4,2,energy,1);
        var request=new ActivityRequest(Source.Nuclide,gamma,Geometry,"INDEPENDENT-SAMPLE-ID",at,at.AddDays(-100),live,real,100,.05,1,0,"",2,.01,Source.HalfLifeReference);
        var result=ActivityAnalysis.Calculate(p,roi,request);
        check(Math.Abs(result.BqAtStart/truth-1)<1e-8,"Recover sample activity at measurement start");
        check(Math.Abs(result.BqAtReportDate/(2*truth)-1)<1e-8,"Decay-correct activity to earlier reference date");
        check(Math.Abs(result.BqPerKg!.Value/truth-1)<1e-8&&result.SigmaBqPerKg>0,"Specific activity Bq per kg and uncertainty");
        var naive=Math.Sqrt(Math.Pow(roi.AreaError!.Value/roi.PeakArea!.Value,2)+Math.Pow(result.Efficiency.Sigma/result.Efficiency.Value,2)+Math.Pow(gamma.ProbabilitySigma/gamma.Probability,2));
        var exactHalf=ActivityAnalysis.Calculate(p,roi,request with{HalfLifeSigmaDays=0,ReportDate=at,HalfLifeReference="Independent test value"});
        check(exactHalf.SigmaBqAtStart/exactHalf.BqAtStart<naive,"Shared gamma probability covariance included in sample activity");
        Reject(()=>ActivityAnalysis.Calculate(p,roi,request with{Geometry=Geometry with{Detector="Other"}}),"Reject sample detector mismatch");
        Reject(()=>ActivityAnalysis.Calculate(p,roi,request with{MeasurementId=points[0].MeasurementId}),"Reject using calibration spectrum as independent activity sample");
        Reject(()=>ActivityAnalysis.Calculate(p,roi,request with{MassKg=0}),"Reject zero sample mass");
        Reject(()=>ActivityAnalysis.Calculate(p,roi,request with{Correction=1.2}),"Require provenance for non-unit correction factor");
        Reject(()=>ActivityAnalysis.Calculate(p,roi,request with{Gamma=gamma with{Probability=.4}}),"Reject inconsistent identical nuclear data reference");
        Reject(()=>ActivityAnalysis.Calculate(p,roi,request with{HalfLifeDays=120}),"Reject inconsistent shared half-life reference");
        var independentHalf=ActivityAnalysis.Calculate(p,roi,request with{HalfLifeReference="Independent assessment"});
        check(result.SigmaBqAtReportDate<independentHalf.SigmaBqAtReportDate,"Shared half-life covariance retained in reported activity");
        var corrected=ActivityAnalysis.Calculate(p,roi,request with{Correction=1.2,CorrectionSigma=.02,CorrectionNotes="Synthetic validated correction"});
        check(Math.Abs(corrected.BqAtStart/(truth*1.2)-1)<1e-8,"Explicit correction factor multiplies activity");
        var bad=points.Select((o,i)=>i==2?o with{Area=o.Area*1.8}:o).ToArray();var badProfile=EfficiencyProfile.Fit("Bad",Geometry,bad,2);
        check(badProfile.ChiSquarePerDof>3,"Flag inconsistent efficiency calibration points");
        Reject(()=>ActivityAnalysis.Calculate(badProfile,roi,request),"Block activity from poor efficiency calibration");
        string folder=Path.Combine(Path.GetTempPath(),"efficiency-check-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        try
        {
            string json=Path.Combine(folder,"calibration.json");p.Save(json);var loaded=EfficiencyProfile.Load(json);
            check(Math.Abs(loaded.Evaluate(600).Value-p.Evaluate(600).Value)<1e-12,"JSON roundtrip recomputes curve from traceable inputs");
            check(loaded.Definition.Points[0].Source.Certificate==Source.Certificate&&loaded.Definition.Geometry==Geometry,"JSON preserves source certificate and geometry");
            EfficiencyProfile.SaveDefinition(json,p.Definition with{Points=points.Take(1).ToArray()});
            check(EfficiencyProfile.ReadDefinition(json).Points.Length==1,"Save and reopen draft calibration with one point");
            Reject(()=>EfficiencyProfile.Load(json),"Draft cannot be used as fitted calibration");
            File.WriteAllText(json,"{\"SchemaVersion\":99}");Reject(()=>EfficiencyProfile.Load(json),"Reject unsupported JSON schema");
            EfficiencyReports.Calibration(Path.Combine(folder,"eff.txt"),p);EfficiencyReports.Activity(Path.Combine(folder,"activity.txt"),p,roi,request,result,"sample.cnf","background.cnf",2);
            string report=File.ReadAllText(Path.Combine(folder,"eff.txt"));
            check(report.Contains("Covariance_row_0")&&report.Contains(Source.Certificate)&&report.Contains("Residual_log_sigma"),"Efficiency TXT includes covariance, certificate and residuals");
            report=File.ReadAllText(Path.Combine(folder,"activity.txt"));
            check(report.Contains("Activity_report_Bq")&&report.Contains("Gamma_keV")&&report.Contains("Correction_K")&&report.Contains("Background_scale\t2"),"Activity report includes provenance and corrections");
        }
        finally{Directory.Delete(folder,true);}
    }
}
