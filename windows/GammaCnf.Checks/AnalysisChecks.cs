using System.Buffers.Binary;
using System.Text;
using GammaCnf.Core;

internal static class AnalysisChecks
{
    public static byte[] Fixture()
    {
        var b=new byte[12288];
        void U16(int p,ushort v)=>BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(p,2),v);
        void U32(int p,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(p,4),v);
        void Float(int p,float v) { uint bits=unchecked((uint)BitConverter.SingleToInt32Bits(v*4));U16(p,(ushort)(bits>>16));U16(p+2,(ushort)bits); }
        U16(0,0x400);U32(0x70,0x12005);U32(0x7a,2048);U32(2048,0x12005);U32(2054,8192);
        U16(2064,0x30);U16(2090,1024);
        for(int i=0;i<1024;i++)U32(2096+4*i,(uint)Math.Round(100+1000*Math.Exp(-.5*Math.Pow((i-300.3)/4.2,2))));
        U32(0xa0,0x12000);U32(0xaa,8192);U32(8192,0x12000);U16(8196,0x500);U32(8198,2048);U16(8208,0x30);U16(8226,0x2ea);U16(8228,0x1fb);
        int c=8240;U32(c+0x89,1024);Encoding.ASCII.GetBytes("POLY").CopyTo(b,c+0x5e);Encoding.ASCII.GetBytes("POLY").CopyTo(b,c+0xfb);Encoding.ASCII.GetBytes("keV").CopyTo(b,c+0x346);
        Float(c+0x312,1);Float(c+0x32e,-.5f);Float(c+0x332,.5f);U16(c+0x46c,2);U32(c+0x32a,7);
        BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(c+0x1fb+9,8),-1005L*10000000);
        BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(c+0x1fb+17,8),-1000L*10000000);
        return b;
    }
    public static void Run(Action<bool,string> check,string? realFile)
    {
        void Reject(Action action,string name)
        { try{action();}catch(CnfException){check(true,name);return;}throw new Exception("Expected rejection: "+name); }
        var data=Fixture();var sample=CnfReader.Decode(data);var session=new AnalysisSession(sample);
        check(sample.Counts.Length==1024&&sample.LiveTime==1000,"Synthetic calibrated CNF acquisition");
        Reject(()=>EnergyCalibration.Validate([0,-1,0,0],1024),"Reject decreasing calibration");
        Reject(()=>EnergyCalibration.Validate([0,double.NaN,0,0],1024),"Reject non-finite calibration");
        double[] target=[1.25,.51,1e-7,0];var patched=CnfCalibration.Patch(data,target);var reread=CnfReader.Decode(patched);
        check(reread.Counts.SequenceEqual(sample.Counts)&&reread.LiveTime==sample.LiveTime,"Calibration CNF roundtrip preserves counts and time");
        check(Math.Abs(reread.Energy(100)!.Value-EnergyCalibration.Evaluate(target,100))<1e-5,"CAM coefficient encoding roundtrip");
        check(Enumerable.Range(0,data.Length).Where(i=>data[i]!=patched[i]).All(i=>i>=8240+0x32e&&i<8240+0x33e||i>=8240+0x46c&&i<8240+0x46e),"CNF changes confined to calibration fields");
        var unknown=(byte[])data.Clone();BinaryPrimitives.WriteUInt16LittleEndian(unknown.AsSpan(8226,2),0);
        Reject(()=>CnfCalibration.Patch(unknown,target),"Refuse unverified CNF write layout");
        var tooLarge=new[]{0d,1e100,0,0};Reject(()=>CnfCalibration.Patch(data,tooLarge),"Reject coefficient overflow");
        string dir=Path.Combine(Path.GetTempPath(),"gamma-check-"+Guid.NewGuid());Directory.CreateDirectory(dir);
        try
        {
            string file=Path.Combine(dir,"original.cnf");File.WriteAllBytes(file,data);
            string? backup=CnfCalibration.Save(file,data,target,true);
            check(backup is not null&&File.ReadAllBytes(backup).SequenceEqual(data),"Overwrite creates exact backup");
            check(File.ReadAllBytes(file).SequenceEqual(patched),"Atomic replacement contains verified calibration");
            Reject(()=>CnfCalibration.Save(file,data,target,true),"Reject stale original when file changed externally");
            string copy=Path.Combine(dir,"copy.cnf");CnfCalibration.Save(copy,data,target,false);
            check(File.ReadAllBytes(copy).SequenceEqual(patched),"Save CNF as new file");
            var bg=sample with {Counts=Enumerable.Repeat(100u,1024).ToArray(),LiveTime=500};
            check(AnalysisSession.LiveTimeScale(sample,bg)==2,"Live-time background normalization");
            session.SetBackground(bg,2);
            check(session.Values[0]==-100&&session.Variances[0]==500,"Negative subtraction retained and Poisson variances propagated");
            Reject(()=>session.SetBackground(bg with{Counts=[1,2]},1),"Reject background channel mismatch");
            Reject(()=>session.SetBackground(bg with{Calibration=[1,1,0,0]},1),"Reject misaligned energy scales");
            Reject(()=>session.SetBackground(bg,-1),"Reject invalid subtraction scale");
            Reject(()=>session.SetBackground(bg with{Calibration=null},1),"Require explicit uncalibrated channel alignment");
            session.SetBackground(bg with{Calibration=null},1,true);check(session.ChannelOnlyBackground,"Explicit channel-only background");
            session.SetBackground(bg,2);session.ExportCsv(Path.Combine(dir,"net.csv"));
            check(File.ReadAllLines(Path.Combine(dir,"net.csv"))[1].Contains(",-100,"),"CSV preserves negative net counts");
            var fit=RoiAnalysis.Fit(session,new(270,330));
            check(fit.Success,"Gaussian fit converges: "+fit.Status);
            check(Math.Abs(fit.Centroid!.Value-300.3)<.03,"Gaussian centroid recovery");
            check(Math.Abs(fit.FwhmChannels!.Value-2.354820045*4.2)<.08,"Gaussian FWHM recovery");
            check(Math.Abs(fit.PeakArea!.Value/(1000*4.2*Math.Sqrt(2*Math.PI))-1)<.005,"Gaussian peak area recovery");
            check(fit.AreaError>0&&fit.CentroidError>0,"Finite fit uncertainties");
            for(int seed=1;seed<=5;seed++)
            {
                var random=new Random(seed);
                double Noise()=>Math.Sqrt(-2*Math.Log(1-random.NextDouble()))*Math.Cos(2*Math.PI*random.NextDouble());
                var noisy=Enumerable.Range(0,1024).Select(i=>
                {
                    double expected=100+.05*(i-300)+1000*Math.Exp(-.5*Math.Pow((i-300.3)/4.2,2));
                    return (uint)Math.Max(0,Math.Round(expected+Math.Sqrt(expected)*Noise()));
                }).ToArray();
                var noisyFit=RoiAnalysis.Fit(new AnalysisSession(sample with{Counts=noisy}),new(270,330));
                check(noisyFit.Success&&Math.Abs(noisyFit.Centroid!.Value-300.3)<.2&&Math.Abs(noisyFit.FwhmChannels!.Value-9.89024)<.5&&Math.Abs(noisyFit.PeakArea!.Value/(1000*4.2*Math.Sqrt(2*Math.PI))-1)<.04,"Noisy Gaussian + sloping continuum recovery seed "+seed);
            }
            var doubled=Enumerable.Range(0,1024).Select(i=>(uint)Math.Round(100+1000*Math.Exp(-.5*Math.Pow((i-295)/3.0,2))+800*Math.Exp(-.5*Math.Pow((i-305)/3.0,2)))).ToArray();
            var overlap=RoiAnalysis.Fit(new AnalysisSession(sample with{Counts=doubled}),new(270,330));
            check(!overlap.Success||overlap.ReducedChiSquare>3,"Overlapping peaks rejected or flagged as poor fit");
            check(!RoiAnalysis.Fit(session,new(298,308)).Success,"Reject truncated peak touching ROI boundary");
            Reject(()=>AnalysisSession.LiveTimeScale(sample with{LiveTime=null},bg),"Missing live time requires manual scale");

            check(Math.Abs(fit.FwhmKeV!.Value-fit.FwhmChannels.Value*.5)<1e-7,"FWHM energy conversion");
            check(!RoiAnalysis.Fit(session,new(300,304)).Success,"Reject undersized ROI");
            var flat=new AnalysisSession(sample with{Counts=Enumerable.Repeat(10u,1024).ToArray()});
            var failed=RoiAnalysis.Fit(flat,new(270,330));check(!failed.Success,"No invented peak on flat data");
            RoiAnalysis.ExportReport(Path.Combine(dir,"roi.txt"),session,"sample.cnf","bg.cnf","Live-time ratio",[fit,failed]);
            string report=File.ReadAllText(Path.Combine(dir,"roi.txt"));
            check(report.Contains("False")&&report.Contains("True")&&report.Contains("Background_scale\t2")&&report.Contains("FWHM_keV"),"TXT includes successful and failed ROI plus normalization context");
            session.ClearBackground();check(session.Values[0]==sample.Counts[0]&&session.Variances[0]==sample.Counts[0],"Clear background restores sample and variance");
            if(realFile is not null)
            {
                byte[] real=File.ReadAllBytes(realFile);var s=CnfReader.Decode(real);
                var changed=CnfCalibration.Patch(real,[.1,.52,1e-8,0]);var rt=CnfReader.Decode(changed);
                check(rt.Counts.SequenceEqual(s.Counts)&&rt.Title==s.Title&&rt.Started==s.Started,"Private CNF calibration roundtrip preserves data and metadata");
                check(real.Length==changed.Length,"Private CNF patch keeps length unchanged");
            }
        }
        finally{Directory.Delete(dir,true);}
    }
}
