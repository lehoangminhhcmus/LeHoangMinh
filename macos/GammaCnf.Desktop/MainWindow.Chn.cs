using System.Buffers.Binary;
using System.Text;
using System.IO;
using GammaCnf.Core;
namespace GammaCnf.App;
public partial class MainWindow
{
    private void RunChnSmokeChecks(string sourcePath,string directory)
    {
        var expected=CnfReader.Read(sourcePath);
        int n=expected.Counts.Length,t=32+4*n;
        var bytes=new byte[t+512];
        BinaryPrimitives.WriteInt16LittleEndian(bytes,-1);
        Encoding.ASCII.GetBytes("06").CopyTo(bytes,6);
        Encoding.ASCII.GetBytes("16Nov2411709").CopyTo(bytes,16);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8),50000);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12),45000);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(30),(ushort)n);
        for(int i=0;i<n;i++)BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32+4*i),expected.Counts[i]);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(t),-102);
        for(int i=0;i<3;i++)BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(t+4+4*i),(float)(expected.Calibration?[i]??(i==1?1:0)));
        string path=Path.Combine(directory,"synthetic-ortec.Chn");File.WriteAllBytes(path,bytes);
        var previous=active;LoadFile(path);
        if(session is null||session.Sample.Format!=SpectrumFormat.OrtecChn||!session.Sample.Counts.SequenceEqual(expected.Counts)||SaveCnfButton.IsEnabled)
            throw new Exception("CHN UI load/count preservation/read-only save control failed.");
        AddRoi(270,330);
        if(rows.Count!=1||!rows[0].Result.Success)throw new Exception("CHN UI ROI fit failed.");
        var background=SpectrumReader.Read(path) with{Counts=expected.Counts.Select(c=>c/2).ToArray(),LiveTime=450};
        pendingBackground=background;pendingBackgroundPath="independent-chn-background.Chn";TimeBasisChoice.SelectedIndex=0;ApplyBackground();
        if(session.Scale!=2)throw new Exception("CHN UI background live-time normalization failed.");
        ClearBackgroundClick(this,new());
        A0.Text="1";A1.Text="0.5";A2.Text="0";A3.Text="0";ApplyCalibration();
        if(calibrationDirty||SaveCnfButton.IsEnabled||!File.ReadAllBytes(path).SequenceEqual(bytes))
            throw new Exception("CHN session calibration changed the source or enabled CNF saving.");
        session.ExportCsv(Path.Combine(directory,"chn-spectrum.csv"));
        string? actual=Environment.GetEnvironmentVariable("GAMMA_CHN_TEST_FILE");
        if(!string.IsNullOrEmpty(actual))
        {
            LoadFile(actual);
            if(session?.Sample.Format!=SpectrumFormat.OrtecChn||session.Sample.Counts.Length!=16384||SaveCnfButton.IsEnabled)
                throw new Exception("Supplied CHN did not open in UI.");
        }
        if(previous is not null)Activate(previous);
        File.WriteAllText(Path.Combine(directory,"chn-ui-success.txt"),"PASS: CHN multi-open, original counts, ROI, CHN background/live-time scaling, session calibration, disabled CNF saving, CSV export");
    }
}
