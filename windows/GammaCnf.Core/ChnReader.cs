using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace GammaCnf.Core;

/// <summary>Read-only ORTEC binary CHN: 32-byte header, UInt32 counts, optional standard trailer.</summary>
public static class ChnReader
{
    public static Spectrum Decode(byte[] data)
    {
        if(data.Length<32||BinaryPrimitives.ReadInt16LittleEndian(data)!=-1)
            throw new CnfException("Không phải file ORTEC CHN được hỗ trợ.");
        ushort U16(int p)=>BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(p,2));
        uint U32(int p)=>BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(p,4));
        int count=U16(30),offset=U16(28);
        if(count<1||count>32768)throw new CnfException("CHN cần số kênh khai báo từ 1 đến 32.768; không tự đoán số kênh.");
        if(offset!=0)throw new CnfException("CHN có kênh bắt đầu khác 0 chưa được hỗ trợ; xuất lại toàn phổ để tránh lệch kênh.");
        int end=32+4*count;
        if(data.Length<end)throw new CnfException("CHN bị cắt ngắn phần số đếm.");
        int trailer=data.Length-end;
        if(trailer is not (0 or 256 or 512))throw new CnfException("Độ dài phần cuối CHN chưa được hỗ trợ hoặc file bị cắt ngắn.");
        var counts=new uint[count];for(int i=0;i<count;i++)counts[i]=U32(32+4*i);
        var warnings=new List<string>();
        double real=U32(8)/50d,live=U32(12)/50d;
        if(live>real)warnings.Add("Thời gian sống lớn hơn thời gian thực; kiểm tra file gốc.");
        DateTime? started=null;
        try
        {
            string Text(int p,int n)=>Encoding.ASCII.GetString(data,p,n);
            int century=data[23] switch{(byte)'0'=>1900,(byte)'1'=>2000,_=>throw new FormatException()};
            string date=Text(16,7)+" "+Text(24,4)+Text(6,2);
            if(!DateTime.TryParseExact(date,"ddMMMyy HHmmss",CultureInfo.InvariantCulture,DateTimeStyles.None,out var parsed))throw new FormatException();
            started=new DateTime(century+int.Parse(Text(21,2),CultureInfo.InvariantCulture),parsed.Month,parsed.Day,parsed.Hour,parsed.Minute,parsed.Second,DateTimeKind.Unspecified);
        }
        catch(Exception e)when(e is FormatException or ArgumentOutOfRangeException)
        {warnings.Add("Ngày/giờ bắt đầu CHN thiếu hoặc không hợp lệ; không tự suy diễn múi giờ/thế kỷ.");}
        double[]? calibration=null;string title="";
        if(trailer>0)
        {
            short type=BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(end,2));
            if(type is -101 or -102)
            {
                double F(int p)=>BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(end+p,4)));
                try{calibration=EnergyCalibration.Validate([F(4),F(8),type==-102?F(12):0,0],count);}
                catch(CnfException){warnings.Add("Hiệu chuẩn CHN không hợp lệ/không tăng; hiển thị theo kênh.");}
            }
            else warnings.Add("Kiểu trailer CHN chưa hỗ trợ hiệu chuẩn; hiển thị theo kênh.");
            if(trailer==512)
            {
                int length=data[end+320];
                if(length<=63)title=Encoding.Latin1.GetString(data,end+321,length).Trim('\0',' ');
                else warnings.Add("Mô tả mẫu CHN có độ dài không hợp lệ.");
            }
        }
        if(calibration is null)warnings.Add("Không có hiệu chuẩn năng lượng CHN hợp lệ.");
        return new(counts,title,calibration,real,live,started,warnings){Format=SpectrumFormat.OrtecChn};
    }
}

public static class SpectrumReader
{
    public static Spectrum Read(string path)
    {
        using var stream=File.OpenRead(path);
        if(stream.Length>64*1024*1024)throw new CnfException("File vượt giới hạn 64 MB.");
        var data=new byte[checked((int)stream.Length)];stream.ReadExactly(data);return Decode(data);
    }
    public static Spectrum Decode(byte[] data)=>data.Length>=2&&BinaryPrimitives.ReadInt16LittleEndian(data)==-1
        ?ChnReader.Decode(data):CnfReader.Decode(data);
}
