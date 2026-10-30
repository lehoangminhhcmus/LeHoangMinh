using GammaCnf.Core;
using System.Buffers.Binary;
internal static class NuclideChecks
{
    public static void Run(Action<bool,string> check)
    {
        string directory=Path.Combine(Path.GetTempPath(),"nuclide-"+Guid.NewGuid());Directory.CreateDirectory(directory);
        void Reject(Action a,string name){try{a();}catch(CnfException){check(true,name);return;}throw new Exception("Expected rejection: "+name);}
        RoiResult Roi(double energy,bool success=true,double fwhm=2)=>new(new((int)energy,(int)energy+10),success,"test",1000,1000,10,EnergyKeV:energy,FwhmKeV:fwhm);
        try
        {
            string path=Path.Combine(directory,"test.csv");
            File.WriteAllText(path,NuclideLibrary.CsvHeader+"\nCs-137,661.657,85.1,\"Synthetic, example only\"\nCo-60,1173.228,99.85,TEST\nCo-60,1332.492,99.98,TEST\nOther,1173.21,,TEST\n");
            var lib=NuclideLibrary.LoadDirectory(directory);var results=lib.Suggest([Roi(661.7),Roi(1173.25),Roi(1332.5)]);
            check(lib.Lines.Count==4&&lib.Lines[0].Reference.Contains(','),"Read UTF-8 CSV with quoted reference commas");
            check(results.Count==4&&results.First(r=>r.Roi==2).Nuclide=="Co-60"&&results.First(r=>r.Nuclide=="Co-60").SupportingRois==1,"Rank isotope with distinct supporting ROIs above closer single-line candidate");
            check(Math.Abs(results[0].DeltaKeV-.043)<1e-9,"Show signed measured minus library energy difference");
            check(lib.Suggest([Roi(661.7),Roi(661.7)]).All(r=>r.SupportingRois==0),"Duplicated overlapping ROI does not manufacture supporting isotope evidence");
            check(lib.Suggest([Roi(670)]).Count==0,"No match beyond energy tolerance");
            check(lib.Suggest([Roi(665.657,true,10)],1,.5).Count==1,"FWHM tolerance supports broad detector peaks");
            check(lib.Suggest([Roi(661.7,false),new(new(0,20),true,"test",100)]).Count==0,"Exclude failed ROI and missing energy calibration");
            check(lib.Suggest([Roi(663.657)],2,0).Count==1,"Include energy exactly at matching boundary");
            Reject(()=>lib.Suggest([],double.NaN),"Reject invalid isotope matching tolerance");
            File.WriteAllText(path,NuclideLibrary.CsvHeader+"\nCs-137,661.657,85.1,TEST\nCs-137,661.657,85.1,TEST\n");
            check(NuclideLibrary.LoadDirectory(directory).Lines.Count==1,"Duplicate library lines do not multiply suggestions");
            File.WriteAllText(path,NuclideLibrary.CsvHeader+"\nCs-137,661.657,200,TEST\n");
            Reject(()=>NuclideLibrary.LoadDirectory(directory),"Reject CSV yield above 100 as gamma probability");
            File.WriteAllText(path,NuclideLibrary.CsvHeader+"\nCo-60,not-number,10,TEST\n");
            Reject(()=>NuclideLibrary.LoadDirectory(directory),"Reject malformed library numeric fields");
            File.WriteAllText(path,NuclideLibrary.Template);Reject(()=>NuclideLibrary.LoadDirectory(directory),"Reject empty template library");
            string nlb=Path.Combine(directory,"synthetic.nlb");File.Delete(path);
            File.WriteAllBytes(nlb,Fixture());var imported=NuclideLibrary.LoadDirectory(directory);
            check(imported.Lines.Count==2&&imported.Lines.All(l=>l.Nuclide=="CO-60")&&imported.Lines.Min(l=>l.EnergyKeV)==1173.25,"NLB references map correctly when emission blocks are allocated out of energy order");
            check(imported.Suggest([Roi(1173.25),Roi(1332.5)]).All(s=>s.SupportingRois==1),"NLB energy lines support candidate at two separate ROIs");
            var bad=Fixture();BinaryPrimitives.WriteUInt16LittleEndian(bad.AsSpan(2048+48+286+1,2),99);File.WriteAllBytes(nlb,bad);
            Reject(()=>NuclideLibrary.LoadDirectory(directory),"Reject NLB out-of-range line reference instead of guessing isotope");
            File.WriteAllBytes(nlb,Fixture()[..3000]);Reject(()=>NuclideLibrary.LoadDirectory(directory),"Reject truncated NLB library");
            var spectrum=CnfReader.Decode(AnalysisChecks.Fixture()) with{Calibration=[1,.5,.0002,.0000001]};
            double targetChannel=300.25,targetEnergy=spectrum.Energy(targetChannel)!.Value;
            var referenceLines=new[]{
                new NuclideLine("TEST",targetEnergy,80,"TEST","a.csv"),
                new NuclideLine("TEST",targetEnergy,50,"TEST","a.csv"),
                new NuclideLine("TEST",spectrum.Energy(0)!.Value,10,"TEST","a.csv"),
                new NuclideLine("TEST",spectrum.Energy(spectrum.Counts.Length-1)!.Value,null,"TEST","a.csv"),
                new NuclideLine("TEST",spectrum.Energy(spectrum.Counts.Length-1)!.Value+1,20,"TEST","a.csv"),
                new NuclideLine("TEST",targetEnergy+20,0,"TEST","a.csv")};
            var markers=NuclideConfirmation.Markers(spectrum,referenceLines);
            check(markers.Count==3&&Math.Abs(markers.Single(m=>m.EnergyKeV==targetEnergy).Channel-targetChannel)<1e-8,"Confirmation line mapping inverts nonlinear calibration and deduplicates equal energies");
            check(Math.Abs(markers.First().Channel)<1e-8&&Math.Abs(markers.Last().Channel-(spectrum.Counts.Length-1))<1e-8,"Confirmation markers include spectrum boundaries and omit out-of-range or zero-probability lines");
            check(NuclideConfirmation.Markers(spectrum with{Calibration=null},referenceLines).Count==0,"Do not invent confirmation positions without energy calibration");
            string? actual=Environment.GetEnvironmentVariable("GAMMA_NLB_TEST_DIRECTORY");
            if(!string.IsNullOrEmpty(actual))
            {
                var expected=new Dictionary<string,int>{{"Naidemo.nlb",12},{"Ptcal.nlb",12},{"Demolib.nlb",59}};int files=0;
                foreach(string file in Directory.EnumerateFiles(actual,"*.nlb",SearchOption.AllDirectories))
                {
                    var loaded=NlbLibraryReader.Read(file);files++;check(loaded.Count>0,"Import supplied NLB: "+Path.GetFileName(file));
                    foreach(var pair in new[]{("CO-60",1173.2),("CS-137",661.65),("AM-241",59.54),("NA-22",1274.5),("K-40",1460.8)})
                        if(loaded.Any(l=>l.Nuclide==pair.Item1))check(loaded.Any(l=>l.Nuclide==pair.Item1&&Math.Abs(l.EnergyKeV-pair.Item2)<.3),"Verify known emission mapping: "+Path.GetFileName(file)+" "+pair.Item1);
                    if(expected.TryGetValue(Path.GetFileName(file),out int count))check(loaded.Count==count,"Supplied small NLB complete line associations: "+Path.GetFileName(file));
                }
                check(files==10,"Validate all ten supplied NLB files");
            }
        }
        finally{Directory.Delete(directory,true);}
    }
    internal static byte[] Fixture()
    {
        var b=new byte[8192];void U16(int o,ushort v)=>BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(o,2),v);void U32(int o,uint v)=>BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(o,4),v);
        void Cam(int o,float v){uint bits=unchecked((uint)BitConverter.SingleToInt32Bits(v*4));U16(o,(ushort)(bits>>16));U16(o+2,(ushort)bits);}
        U16(0,1024);
        void Block(int entry,int p,uint type,int used,int count,int fixedSize,bool first)
        {
            U32(entry,type);U16(entry+4,(ushort)(first?0x100:0x300));U32(entry+6,2048);U32(entry+10,(uint)p);
            U32(p,type);U16(p+30,(ushort)count);U16(p+32,(ushort)fixedSize);U16(p+46,(ushort)used);
        }
        Block(112,2048,0x12007,48+292,1,286,true);U16(2096,292);b[2098]=1;System.Text.Encoding.ASCII.GetBytes("CO-60   ").CopyTo(b,2099);
        b[2096+286]=1;U16(2096+287,1);b[2096+289]=1;U16(2096+290,2);
        Block(160,4096,0x12008,48+69,1,69,true);b[4144]=1;Cam(4145,1332.5f);Cam(4149,99.98f);
        Block(208,6144,0x12008,48+69,1,69,false);b[6192]=1;Cam(6193,1173.25f);Cam(6197,99.85f);
        return b;
    }
}
