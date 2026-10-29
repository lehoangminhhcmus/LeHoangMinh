using System.Buffers.Binary;
using System.Globalization;
using GammaCnf.Core;

if(args.Length==2 && args[0]=="--write-fixture") { File.WriteAllBytes(args[1],AnalysisChecks.Fixture());return; }
int passed=0;
void Check(bool ok,string name) { if(!ok) throw new Exception("FAIL: "+name); Console.WriteLine("PASS: "+name);passed++; }
void Reject(byte[] data,string name)
{ try { CnfReader.Decode(data); } catch(CnfException) { Check(true,name);return; } throw new Exception("Expected rejection: "+name); }
byte[] Fixture()
{
    var b=new byte[4096];
    void U16(int p,ushort v)=>BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(p,2),v);
    void U32(int p,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(p,4),v);
    U16(0,0x400);U32(0x70,0x12005);U32(0x7a,2048);U32(2048,0x12005);
    U16(2048+0x10,0x30);U16(2048+0x2a,4);
    U32(2096,0);U32(2100,7);U32(2104,uint.MaxValue);U32(2108,12);
    return b;
}
var basic=CnfReader.Decode(Fixture());
Check(basic.Counts.SequenceEqual(new uint[]{0,7,uint.MaxValue,12}),"UInt32 counts preserve full precision");
Check(basic.Total==4294967314UL,"Total uses UInt64");
Check(basic.Calibration is null && basic.Energy(1) is null,"No invented energy calibration");
Reject([1,2,3],"Wrong format");Reject(Fixture()[..2100],"Truncated counts");
var invalid=Fixture();BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(0x7a,4),uint.MaxValue);Reject(invalid,"Invalid pointer");
invalid=Fixture();invalid[2048]=0;Reject(invalid,"Block mismatch");
invalid=Fixture();BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(0x70,4),0);Reject(invalid,"Missing spectrum");
string output=Path.GetTempFileName();
try
{
    CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("vi-VN");
    basic.ExportCsv(output);string[] rows=File.ReadAllLines(output);
    Check(rows.Length==5 && rows[3]=="2,,4294967295","CSV all channels and unsigned values");
    var calibrated=basic with { Calibration=[-.5,.25,0,0] };
    Check(calibrated.Energy(2)==0,"Horner polynomial evaluation");
    calibrated.ExportCsv(output);Check(File.ReadAllLines(output)[2]=="1,-0.25,7","CSV decimal is invariant under Vietnamese locale");
    if(args.Length>0)
    {
        var s=CnfReader.Read(args[0]);
        Check(s.Counts.Length==4096 && s.Total==256546,"Private sample channels and total");
        Check(s.Counts[1740]==1360 && s.Counts.Max()==1360,"Private sample maximum");
        Check(s.LiveTime==1000 && Math.Abs(s.RealTime!.Value-1005)<.001,"Live and real durations");
        Check(Math.Abs(s.Energy(1740)!.Value-897.4130512475967)<1e-9,"CAM energy coefficients");
        Check(s.Started==new DateTime(1983,5,5,8,53,0),"CAM date epoch");
        Check(s.Warnings.Count==0,"No sample decoding warnings");
        s.ExportCsv(output);Check(File.ReadAllLines(output).Length==4097,"Sample CSV length");
        if(args.Length>1) s.ExportCsv(args[1]);
    }
    else Console.WriteLine("NOT RUN: private sample checks (pass path as argument)");
}
finally { File.Delete(output); }
AnalysisChecks.Run(Check,args.Length>0?args[0]:null);
EfficiencyChecks.Run(Check);
Console.WriteLine($"{passed} checks passed");
