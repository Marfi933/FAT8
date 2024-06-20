namespace OS;

public class BootBlock: Block
{
    public ushort BPB_BytsPerSec { get; set; } = 512;
    public ushort BPB_SecPerClus { get; set; } = 2;
    public ushort BPB_RsvdSecCnt { get; set; } = 1;
    public int BPB_TotSec32 { get; set; } = 2;

    public BootBlock(Drive drive, int id=0, ushort BPB_BytsPerSec=512, ushort BPB_SecPerClus=2, ushort BPB_RsvdSecCnt=1, int BPB_TotSec32=2, bool write=true) : base(id, drive)
    {
        blockId = id;
        Data = new byte[512];
        this.BPB_BytsPerSec = BPB_BytsPerSec;
        this.BPB_SecPerClus = BPB_SecPerClus;
        this.BPB_RsvdSecCnt = BPB_RsvdSecCnt;
        this.BPB_TotSec32 = BPB_TotSec32;
        
        var bytes = BitConverter.GetBytes(BPB_BytsPerSec);
        Data[11] = bytes[0];
        Data[12] = bytes[1];
        
        bytes = BitConverter.GetBytes(BPB_SecPerClus);
        Data[13] = bytes[0];
        
        bytes = BitConverter.GetBytes(BPB_RsvdSecCnt);
        Data[14] = bytes[0];
        Data[15] = bytes[1];
        
        bytes = BitConverter.GetBytes(BPB_TotSec32);
        Data[32] = bytes[0];
        Data[33] = bytes[1];
        Data[34] = bytes[2];
        Data[35] = bytes[3];
        
        if (write) drive.WriteBlock(this);
    }

    public BootBlock(Drive drive, int id = 0) : base(id, drive)
    {
        blockId = id;
        Data = drive.ReadBlock(id).Data;
        
        byte[] bytes2 = new byte[2];
        byte[] bytes4 = new byte[4];
        
        bytes2[0] = Data[11];
        bytes2[1] = Data[12];
        BPB_BytsPerSec = BitConverter.ToUInt16(bytes2, 0);

        bytes2[1] = new byte();
        bytes2[0] = Data[13];
        BPB_SecPerClus = BitConverter.ToUInt16(bytes2, 0);
        
        bytes2[0] = Data[14];
        bytes2[1] = Data[15];
        BPB_RsvdSecCnt = BitConverter.ToUInt16(bytes2, 0);
        
        bytes4[0] = Data[32];
        bytes4[1] = Data[33];
        bytes4[2] = Data[34];
        bytes4[3] = Data[35];
        BPB_TotSec32 = BitConverter.ToInt32(bytes4, 0);
    }
    
}