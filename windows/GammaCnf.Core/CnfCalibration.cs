using System.Buffers.Binary;
using System.Text;

namespace GammaCnf.Core;

public static class EnergyCalibration
{
    public static double Evaluate(IReadOnlyList<double> c, double channel)
    {
        double value = 0;
        for (int i = c.Count-1; i >= 0; i--) value = value * channel + c[i];
        return value;
    }
    public static double[] Validate(IEnumerable<double> coefficients, int channels)
    {
        var c = coefficients.ToArray();
        if (c.Length != 4 || c.Any(x => !double.IsFinite(x))) throw new CnfException("Cần 4 hệ số hữu hạn a0, a1, a2, a3.");
        // Check the derivative over the continuous channel range, including its vertex.
        double end = Math.Max(1, channels-1), previous = Evaluate(c, 0);
        double D(double x) => c[1]+2*c[2]*x+3*c[3]*x*x;
        if (D(0) <= 0 || D(end) <= 0 || (c[3] > 0 && -c[2]/(3*c[3]) is double v && v > 0 && v < end && D(v) <= 0))
            throw new CnfException("Đường chuẩn phải tăng trên toàn miền kênh.");
        for (int i=1;i<=channels;i++)
        {
            double e = Evaluate(c,i);
            if (!double.IsFinite(e) || e <= previous) throw new CnfException("Đường chuẩn không hợp lệ hoặc không tăng.");
            previous=e;
        }
        return c;
    }
}

/// <summary>Patch only a verified standard ACQP polynomial layout; preserve all other bytes.</summary>
public static class CnfCalibration
{
    public static byte[] Patch(byte[] original, double[] coefficients)
    {
        var before = CnfReader.Decode(original);
        EnergyCalibration.Validate(coefficients, before.Counts.Length);
        var output = (byte[])original.Clone();
        ushort U16(int p) => BinaryPrimitives.ReadUInt16LittleEndian(original.AsSpan(p,2));
        uint U32(int p) => BinaryPrimitives.ReadUInt32LittleEndian(original.AsSpan(p,4));
        int patched=0;
        for(int i=0;i<28;i++)
        {
            int entry=0x70+48*i;
            if(U32(entry)!=0x12000) continue;
            int p=checked((int)U32(entry+10));
            // Only the layout verified against the supplied Genie 2000 file is writable.
            if(U16(p+0x10)!=0x30 || U16(p+0x22)!=0x2ea || U16(p+4)!=0x500 || p+0x30+0x46e>original.Length
                || U32(p+6)<0x30+0x46e || p+(long)U32(p+6)>original.Length)
                throw new CnfException("Biến thể ACQP này chỉ được hỗ trợ đọc; chưa thể ghi hiệu chuẩn an toàn.");
            int common=p+0x30;
            string Text(int offset,int n)=>Encoding.ASCII.GetString(original,common+offset,n).Trim(' ','\0');
            if(Text(0x5e,4)!="POLY" || Text(0xfb,4)!="POLY" || Text(0x346,3)!="keV")
                throw new CnfException("Chỉ ghi CNF đã có hiệu chuẩn POLY theo keV.");
            uint scaleBits=((uint)U16(common+0x312)<<16)|U16(common+0x314);
            if(BitConverter.Int32BitsToSingle(unchecked((int)scaleBits))/4f!=1f)
                throw new CnfException("Hệ số thang năng lượng CNF chưa được hỗ trợ ghi.");
            for(int j=0;j<4;j++)
            {
                float scaled=(float)(coefficients[j]*4);
                if(!float.IsFinite(scaled) || (scaled==0 && coefficients[j]!=0) || (scaled!=0 && Math.Abs(scaled)<1.17549435e-38f))
                    throw new CnfException("Hệ số vượt phạm vi số thực CAM 32-bit.");
                uint bits=unchecked((uint)BitConverter.SingleToInt32Bits(scaled));
                int dest=common+0x32e+4*j;
                BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(dest,2),(ushort)(bits>>16));
                BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(dest+2,2),(ushort)bits);
            }
            int count=4;while(count>2 && coefficients[count-1]==0)count--;
            BinaryPrimitives.WriteUInt16LittleEndian(output.AsSpan(common+0x46c,2),(ushort)count);
            BinaryPrimitives.WriteUInt32LittleEndian(output.AsSpan(common+0x32a,4),U32(common+0x32a)|1u);
            patched++;
        }
        if(patched==0) throw new CnfException("File không có khối ACQP để lưu hiệu chuẩn.");
        var decoded=CnfReader.Decode(output);
        EnergyCalibration.Validate(decoded.Calibration ?? throw new CnfException("Không đọc lại được hiệu chuẩn vừa ghi."), before.Counts.Length);
        if(!decoded.Counts.SequenceEqual(before.Counts))throw new CnfException("Ghi hiệu chuẩn làm thay đổi số đếm; đã hủy.");
        return output;
    }

    public static string? Save(string path, byte[] expectedOriginal, double[] coefficients, bool overwriteSource)
    {
        byte[] updated=Patch(expectedOriginal,coefficients);
        string target=Path.GetFullPath(path), dir=Path.GetDirectoryName(target)!;
        string temporary=Path.Combine(dir,"."+Guid.NewGuid().ToString("N")+".tmp");
        string? backup=null;
        try
        {
            using(var f=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { f.Write(updated);f.Flush(true); }
            if(File.Exists(target))
            {
                if(overwriteSource && !File.ReadAllBytes(target).SequenceEqual(expectedOriginal))
                    throw new CnfException("File CNF đã thay đổi bên ngoài app. Mở lại file trước khi ghi đè.");
                backup=target+"."+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+"."+Guid.NewGuid().ToString("N")[..6]+".bak";
                File.Replace(temporary,target,backup);
            }
            else
            {
                if(overwriteSource)throw new CnfException("File gốc không còn tồn tại. Hãy lưu thành file mới.");
                File.Move(temporary,target,false);
            }
            return backup;
        }
        finally { if(File.Exists(temporary))File.Delete(temporary); }
    }
}
