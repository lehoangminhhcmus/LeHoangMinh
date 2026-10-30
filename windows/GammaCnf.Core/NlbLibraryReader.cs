using System.Buffers.Binary;
using System.Text;
namespace GammaCnf.Core;

/// <summary>Read-only importer for the CAM NLB variants verified against the supplied libraries.</summary>
public static class NlbLibraryReader
{
    private sealed record Block(int Offset,int Size,int Used,int Count,int Fixed,int Header,bool First);
    public static IReadOnlyList<NuclideLine> Read(string path)
    {
        if(new FileInfo(path).Length>32*1024*1024)throw new CnfException("NLB vượt 32 MB.");
        byte[] b=File.ReadAllBytes(path);
        CnfException Invalid()=>new($"{Path.GetFileName(path)}: NLB không hợp lệ hoặc biến thể chưa hỗ trợ; chưa nạp để tránh gán sai đồng vị.");
        ushort U16(int o){if(o<0||o>b.Length-2)throw Invalid();return BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(o,2));}
        uint U32(int o){if(o<0||o>b.Length-4)throw Invalid();return BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(o,4));}
        int Int(uint v){if(v>int.MaxValue)throw Invalid();return (int)v;}
        if(b.Length<2048||U16(0)!=0x400)throw Invalid();
        var groups=new Dictionary<uint,List<Block>>{{0x12007,[]},{0x12008,[]}};
        int directoryEnd=2048;
        for(int o=0x70;o<=directoryEnd-48;o+=48)
        {
            uint type=U32(o);if(type==0)continue;
            int p=Int(U32(o+10)),size=Int(U32(o+6));
            if(p<2048||size<48||p>b.Length-size||U32(p)!=type)throw Invalid();
            directoryEnd=Math.Min(directoryEnd,p);
            if(!groups.TryGetValue(type,out var list))continue;
            if(list.Any(q=>q.Offset==p))throw Invalid();
            int used=U16(p+46),count=U16(p+30),fixedSize=U16(p+32),header=U16(p+34);
            bool first=(U16(o+4)&0x200)==0;
            if(used<48||used>size||fixedSize<1||first&&48+header>used)throw Invalid();
            list.Add(new(p,size,used,count,fixedSize,header,first));
        }
        if(groups.Any(g=>g.Value.Count==0)||groups.Any(g=>g.Value.Count(q=>q.First)!=1))throw Invalid();
        double Cam(byte[] data,int o)
        {
            if(o<0||o>data.Length-4)throw Invalid();
            uint bits=((uint)BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(o,2))<<16)|BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(o+2,2));
            double value=BitConverter.Int32BitsToSingle(unchecked((int)bits))/4d;if(!double.IsFinite(value))throw Invalid();return value;
        }
        var chunks=new List<List<(double Energy,double Intensity)>>();
        foreach(var q in groups[0x12008])
        {
            if(q.Fixed is not (65 or 69 or 113 or 129))throw Invalid();
            int start=q.Offset+48+(q.First?q.Header:0),end=q.Offset+q.Used;
            if(end-start!=q.Count*q.Fixed)throw Invalid();
            var chunk=new List<(double Energy,double Intensity)>();double previous=0;
            for(int o=start;o<end;o+=q.Fixed)
            {
                double e=Cam(b,o+1),intensity=Cam(b,o+5);
                if(b[o]!=1||e<=0||e>100000||e<previous||intensity<0)throw Invalid();
                chunk.Add((e,intensity));previous=e;
            }
            if(chunk.Count==0)throw Invalid();chunks.Add(chunk);
        }
        // Directory slots follow allocation history. Logical line IDs follow the ordered
        // energy runs. Sort whole runs to preserve identities of equal-energy records.
        var emissions=new List<(double Energy,double Intensity)>();
        foreach(var chunk in chunks.OrderBy(c=>c[0].Energy))
        {
            if(emissions.Count>0&&emissions[^1].Energy>chunk[0].Energy)throw Invalid();emissions.AddRange(chunk);
        }
        if(emissions.Count>200000)throw Invalid();
        var ns=groups[0x12007];int fixedNuclide=ns[0].Fixed;
        if(fixedNuclide is not (253 or 286 or 374 or 483)||ns.Any(q=>q.Fixed!=fixedNuclide))throw Invalid();
        using var joined=new MemoryStream();
        foreach(var q in ns){int start=q.Offset+48+(q.First?q.Header:0);joined.Write(b,start,q.Offset+q.Used-start);}
        byte[] data=joined.ToArray();int cursor=0,countNuclides=ns.Sum(q=>q.Count);var output=new List<NuclideLine>();
        for(int i=0;i<countNuclides;i++)
        {
            if(cursor>data.Length-fixedNuclide)throw Invalid();
            int length=BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(cursor,2));
            if(length<fixedNuclide||length>data.Length-cursor||(length-fixedNuclide)%3!=0||data[cursor+2]!=1)throw Invalid();
            string name=Encoding.ASCII.GetString(data,cursor+3,8).Trim(' ','\0');
            if(name.Length==0||name.Any(c=>!(char.IsAsciiLetterOrDigit(c)||c=='-')))throw Invalid();
            for(int o=cursor+fixedNuclide;o<cursor+length;o+=3)
            {
                int index=BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(o+1,2));
                if(data[o]!=1||index<1||index>emissions.Count)throw Invalid();
                var line=emissions[index-1];
                // Some NLB records have yields >100 (e.g. annihilation/group yields).
                // Keep their energy for matching, without treating that yield as a probability.
                output.Add(new(name,line.Energy,line.Intensity<=100?line.Intensity:null,"NLB: "+Path.GetFileName(path)+" (nguồn dữ liệu hạt nhân chưa xác minh)",Path.GetFileName(path)));
                if(output.Count>200000)throw Invalid();
            }
            cursor+=length;
        }
        if(cursor!=data.Length||output.Count==0)throw Invalid();return output;
    }
}
