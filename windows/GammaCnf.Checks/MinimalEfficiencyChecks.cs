using System.Text.Json;
using System.Text.Json.Nodes;
using GammaCnf.Core;
internal static class MinimalEfficiencyChecks
{
    public static void Run(Action<bool,string> check)
    {
        void Reject(Action action,string name){try{action();}catch(CnfException){check(true,name);return;}throw new Exception("Expected rejection: "+name);}
        var sample=CnfReader.Decode(AnalysisChecks.Fixture());var session=new AnalysisSession(sample);
        var roi=RoiAnalysis.Fit(session,new(270,330));if(!roi.Success)throw new Exception("Fixture ROI did not fit.");
        var fields=new Dictionary<string,string?>{{"ActivityText","10000"},{"GammaProbability","50"},{"LiveText","1000"}};
        EfficiencyObservation Build(bool decay=false)=>EfficiencyInput.Build(session,roi,"synthetic.cnf","MEAS-1",null,fields,decay);
        var point=Build();
        check(Math.Abs(point.Efficiency/(roi.PeakArea!.Value/(10000*1000*.5))-1)<1e-12,"Minimal efficiency needs only ROI, live time, activity at measurement and probability");
        check(point.Source.ActivityAtMeasurement&&point.Source.HalfLifeDays==0&&point.RealSeconds==0&&point.MeasuredAt==DateTimeOffset.MinValue,"Missing decay/timing data does not fabricate dates or real time");
        check(!point.Geometry.DistanceSpecified&&point.Geometry.Detector==""&&point.Source.Certificate==""&&point.Gamma.Reference=="","Missing calibration metadata remains empty and distance is unknown rather than measured zero");
        check(point.Gamma.EnergyKeV==roi.EnergyKeV&&point.MissingInputs!.Any(s=>s.Contains("sai số hoạt độ")),"Energy defaults to fitted ROI and missing uncertainties are recorded");
        fields["ActivityText"]="";Reject(()=>Build(),"Activity is still required for efficiency");fields["ActivityText"]="10000";
        fields["GammaProbability"]="";Reject(()=>Build(),"Gamma probability is still required");fields["GammaProbability"]="50";
        fields["GammaProbability"]="101";Reject(()=>Build(),"Reject nonphysical probability in minimal mode");fields["GammaProbability"]="50";
        fields["ActivitySigmaText"]="4";fields["ActivityCoverageText"]="2";fields["GammaSigma"]="0.5";
        var uncertain=Build();check(uncertain.Source.ActivitySigmaBq==200&&uncertain.Gamma.ProbabilitySigma==.005,"Optional certificate U/k and gamma percentage-point uncertainty converted once");
        check(uncertain.Systematics().Values.All(v=>v==0)&&Math.Abs(uncertain.IndependentLogVariance-(Math.Pow(roi.AreaError!.Value/roi.PeakArea.Value,2)+.02*.02+.01*.01))<1e-12,"Unknown source/nuclear identities do not manufacture shared covariance");
        fields["ActivitySigmaText"]="bad";Reject(()=>Build(),"Optional values still reject invalid entered numbers");fields["ActivitySigmaText"]="";
        fields["StandardCorrection"]="1.1";var corrected=Build();check(Math.Abs(corrected.Efficiency/point.Efficiency-1.1)<1e-12&&corrected.MissingInputs!.Any(s=>s.Contains("K hiệu chỉnh")),"Optional correction notes do not block calculation and omission is disclosed");fields["StandardCorrection"]="";
        fields["DistanceText"]="0";check(Build().Geometry.DistanceSpecified,"Explicit zero distance differs from missing distance");fields["DistanceText"]="";
        fields["RealText"]="0";Reject(()=>Build(),"Explicit zero real time is invalid rather than silently omitted");fields["RealText"]="";
        fields["HalfLifeText"]="100";Reject(()=>Build(),"Real time required only when decay during counting is requested");fields["RealText"]="1000";
        var averaged=Build();check(Math.Abs(averaged.Efficiency/BuildWithoutHalf()/ (1/RadioactiveDecay.AverageFactor(100,1000))-1)<1e-12,"Activity-at-measurement mode optionally corrects decay during counting");
        double BuildWithoutHalf(){fields["HalfLifeText"]="";double result=Build().Efficiency;fields["HalfLifeText"]="100";return result;}
        fields["HalfLifeText"]="";Reject(()=>Build(true),"Certificate correction does not invent dates or half-life");
        fields["MeasuredAt"]="2026-04-11T00:00:00+07:00";fields["ReferenceDateText"]="2026-01-01T00:00:00+07:00";fields["HalfLifeText"]="100";
        var certificate=Build(true);check(Math.Abs(certificate.Efficiency/(point.Efficiency*2/RadioactiveDecay.AverageFactor(100,1000))-1)<1e-12,"Optional certificate mode decays activity over one half-life without requiring certificate metadata");
        fields["MeasuredAt"]="2026-04-11T00:00:00";Reject(()=>Build(true),"Certificate decay requires explicit time zones");
        var minimal=new Dictionary<string,string?>{{"ActivityText","10000"},{"GammaProbability","50"},{"LiveText","1000"}};
        var first=EfficiencyInput.Build(session,roi,"synthetic.cnf","MEAS-1",null,minimal,false);
        var points=new[]{100d,200d,400d,800d}.Select((energy,i)=>
        {
            double area=10000*1000*.5*.01*Math.Pow(energy/200,-.7);
            return first with{Gamma=first.Gamma with{EnergyKeV=energy},CentroidKeV=energy,FwhmKeV=2,Area=area,AreaSigma=Math.Sqrt(area),RoiStart=i*40,RoiEnd=i*40+20};
        }).ToArray();
        var profile=EfficiencyProfile.Fit("Minimal",first.Geometry,points,1);
        check(Math.Abs(profile.Evaluate(300).Value/(.01*Math.Pow(300d/200,-.7))-1)<1e-9,"Minimal points recover a known efficiency curve using ROI uncertainty only");
        check(profile.InputWarnings.Length>0&&profile.InputSummary.Contains("chưa phải ngân sách đầy đủ"),"Partial uncertainty is disclosed on the calibration profile");
        string folder=Path.Combine(Path.GetTempPath(),"gamma-minimal-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            string json=Path.Combine(folder,"minimal.json"),report=Path.Combine(folder,"report.txt");profile.Save(json);var loaded=EfficiencyProfile.Load(json);
            check(loaded.Definition.SchemaVersion==2&&loaded.Definition.Points[0].Source.ActivityAtMeasurement&&loaded.InputWarnings.SequenceEqual(profile.InputWarnings)&&Math.Abs(loaded.Evaluate(300).Value/profile.Evaluate(300).Value-1)<1e-12,"Schema 2 roundtrip preserves basic-mode calculation and unknown-input provenance");
            EfficiencyReports.Calibration(report,loaded);string text=File.ReadAllText(report);
            check(text.Contains("Input_completeness")&&text.Contains("Activity at measurement start")&&text.Contains("Missing_inputs")&&text.Contains("NA"),"TXT reports basic formula mode and missing inputs rather than invented timestamps");
            var legacy=EfficiencyChecks.Points();var node=JsonNode.Parse(JsonSerializer.Serialize(new EfficiencyFile(1,"Legacy",EfficiencyChecks.Geometry,DateTimeOffset.UtcNow,2,legacy)))!.AsObject();
            void StripGeometry(JsonObject obj){obj.Remove("OptionalMetadata");obj.Remove("DistanceSpecified");}
            StripGeometry(node["Geometry"]!.AsObject());
            foreach(var item in node["Points"]!.AsArray())
            {
                var o=item!.AsObject();StripGeometry(o["Geometry"]!.AsObject());o.Remove("MissingInputs");o["Source"]!.AsObject().Remove("ActivityAtMeasurement");o["Source"]!.AsObject().Remove("OptionalMetadata");o["Gamma"]!.AsObject().Remove("OptionalMetadata");
            }
            File.WriteAllText(json,node.ToJsonString());var old=EfficiencyProfile.Load(json);
            check(!old.Definition.Points[0].Source.ActivityAtMeasurement&&old.Definition.Geometry.DistanceSpecified&&Math.Abs(old.Evaluate(400).Value/EfficiencyChecks.TrueEfficiency(400)-1)<1e-8,"Unmodified v0.7 schema 1 profiles retain certificate decay and known geometry");
            var spoof=JsonNode.Parse(JsonSerializer.Serialize(profile.Definition))!.AsObject();spoof["SchemaVersion"]=1;File.WriteAllText(json,spoof.ToJsonString());Reject(()=>EfficiencyProfile.Load(json),"Basic mode cannot masquerade as old schema 1 and silently change formula");
        }
        finally{Directory.Delete(folder,true);}
    }
}
