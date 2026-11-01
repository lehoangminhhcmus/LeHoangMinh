using System.Buffers.Binary;
using System.Text;
using GammaCnf.Core;

internal static class ChnChecks
{
    internal static byte[] Fixture()
    {
        var b=new byte[32+4*1024+512];
        void U16(int p,ushort v)=>BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(p,2),v);
        void U32(int p,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(p,4),v);
        U16(0,65535);U16(2,1);U16(4,1);Encoding.ASCII.GetBytes("06").CopyTo(b,6);
        Encoding.ASCII.GetBytes("16Nov2411709").CopyTo(b,16);U32(8,50000);U32(12,45000);U16(30,1024);
        for(int i=0;i<1024;i++)U32(32+4*i,(uint)Math.Round(10+1000*Math.Exp(-.5*Math.Pow((i-300)/5d,2))));
        U32(32,uint.MaxValue);U32(32+4*1023,123);
        int t=32+4*1024;U16(t,unchecked((ushort)-102));
        void F(int p,float v)=>U32(t+p,unchecked((uint)BitConverter.SingleToInt32Bits(v)));
        F(4,.5f);F(8,2);F(12,.0001f);b[t+320]=4;Encoding.ASCII.GetBytes("TEST").CopyTo(b,t+321);return b;
    }
    internal static void Run(Action<bool,string> check)
    {
        var b=Fixture();int t=32+4096;var s=SpectrumReader.Decode(b);
        void Reject(byte[] bytes,string name){try{SpectrumReader.Decode(bytes);}catch(CnfException){check(true,name);return;}throw new Exception("Expected rejection: "+name);}
        check(s.Format==SpectrumFormat.OrtecChn&&s.Counts.Length==1024&&s.Title=="TEST","Detect binary CHN and read sample description");
        check(s.Counts[0]==uint.MaxValue&&s.Counts[^1]==123&&s.Counts[300]==1010,"CHN preserves unsigned counts including edge channels");
        check(s.RealTime==1000&&s.LiveTime==900,"CHN times use 20 ms ticks");
        check(s.Started==new DateTime(2024,11,16,17,9,6)&&s.Started.Value.Kind==DateTimeKind.Unspecified,"CHN date, century and seconds preserve local acquisition time");
        check(Math.Abs(s.Energy(300)!.Value-609.5)<1e-5,"CHN quadratic IEEE energy calibration");
        check(s.Warnings.Count==0,"Standard CHN fixture imports without warnings");
        var session=new AnalysisSession(s);var roi=RoiAnalysis.Fit(session,new(275,325));
        check(roi.Success&&Math.Abs(roi.Centroid!.Value-300)<.1,"CHN spectrum supports Gaussian ROI analysis");
        session.SetBackground(s with{Format=SpectrumFormat.CanberraCnf,Counts=s.Counts.Select(c=>c/2).ToArray(),LiveTime=450},2);
        check(session.Scale==2&&session.Values[300]==0,"CHN sample can subtract aligned CNF background");
        session.ClearBackground();session.SetCalibration([61.657,2,0,0]);
        check(NuclideLibrary.LoadBundled().Suggest([RoiAnalysis.Fit(session,new(275,325))]).Any(h=>h.Nuclide=="CS-137"),"CHN ROI uses bundled isotope matching after session calibration");
        var empty=b[..t];check(SpectrumReader.Decode(empty).Calibration is null,"CHN without trailer retains counts without inventing calibration");
        var bad=(byte[])b.Clone();BinaryPrimitives.WriteUInt16LittleEndian(bad.AsSpan(28),1);Reject(bad,"Reject unsupported CHN first-channel offset");
        bad=(byte[])b.Clone();BinaryPrimitives.WriteUInt16LittleEndian(bad.AsSpan(30),0);Reject(bad,"Reject ambiguous zero-channel CHN");
        Reject(b[..31],"Reject truncated CHN header");Reject(b[..(t-1)],"Reject truncated CHN counts");Reject(b[..^1],"Reject partial CHN trailer");
        bad=(byte[])b.Clone();bad[23]=(byte)'x';check(SpectrumReader.Decode(bad).Started is null,"Invalid CHN century is not guessed");
        bad=(byte[])b.Clone();bad[23]=(byte)'0';check(SpectrumReader.Decode(bad).Started!.Value.Year==1924,"CHN twentieth-century flag");
        bad=(byte[])b.Clone();BinaryPrimitives.WriteSingleLittleEndian(bad.AsSpan(t+8),float.NaN);check(SpectrumReader.Decode(bad).Calibration is null,"Reject non-finite CHN calibration while retaining counts");
        bad=(byte[])b.Clone();BinaryPrimitives.WriteSingleLittleEndian(bad.AsSpan(t+8),-2);check(SpectrumReader.Decode(bad).Calibration is null,"Reject decreasing CHN calibration");
        bad=(byte[])b.Clone();BinaryPrimitives.WriteInt16LittleEndian(bad.AsSpan(t),-101);check(SpectrumReader.Decode(bad).Energy(300)==600.5,"CHN -101 trailer uses linear calibration only");
        string? actual=Environment.GetEnvironmentVariable("GAMMA_CHN_TEST_FILE");
        if(!string.IsNullOrEmpty(actual))
        {
            var sample=SpectrumReader.Read(actual);
            check(sample.Counts.Length==16384&&sample.LiveTime==86400&&sample.RealTime==88470.14,"Supplied Ortec CHN channel count and acquisition times");
            check(sample.Started==new DateTime(2024,11,16,17,9,6)&&sample.Calibration is not null,"Supplied Ortec CHN date and calibration");
            check(sample.Total==6623170&&sample.Warnings.Count==0&&Math.Abs(sample.Calibration![1]-.2056126743555069)<1e-12,"Supplied CHN total counts and IEEE calibration match binary fields");
        }
    }
}
