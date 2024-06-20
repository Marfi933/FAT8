namespace OS;
public class FileDescriptor
{
    public string filename { get; }
    public int size { get; set; }
    private int _currentPosition { get; set; }
    private readonly FAT _fat;
    
    public FileDescriptor(string filename, int size, FAT fat)
    {
        this.filename = filename;
        _fat = fat;
        if (size > -1) this.size = size;
        else
        {
            throw new Exception("File size must be greater than -1");
        }
    }

    public void fileSeek(int size)
    {
        if (size > this.size) throw new Exception("Seek size must be less than file size.");
        _currentPosition = size;
    }

    public int fileStat()
    {
        return size;
    }

    public int fileTell()
    {
        return _currentPosition;
    }
    
    public void fileTruncate(int size)
    {
 
        if (_fat.IsOpened()) _fat.ChangeSize(this, size);
        else
        {
            _fat.Open();
            _fat.ChangeSize(this, size);
            _fat.Close();
        }
    }
    
    public void fileWrite(byte[] data)
    {
        if (data.Length > size)
        {
            if (_fat.IsOpened()) _fat.ChangeSize(this, data.Length);
            else
            {
                _fat.Open();
                _fat.ChangeSize(this, data.Length);
                _fat.Close();
            }
        }
        
        var firstBlockID = _fat.findDirectoryTable(filename).first_cluster;
        Block block = new Block(firstBlockID* _fat._bootBlock.BPB_SecPerClus, _fat._drive);
        var count = data.Length / block.size;
        
        for (int i = 0; i < count; i++)
        {
            block.Data = data[i..(i+block.size)];
            _fat._drive.WriteBlock(block);
            block.blockId++;
        }

    }
    
    public byte[] fileRead(int size)
    {
        var first_cluster = _fat.findDirectoryTable(filename).first_cluster;
        List<byte> ret = new List<byte>();
        var blockId = first_cluster * _fat._bootBlock.BPB_SecPerClus;
        Block block = new Block(blockId, _fat._drive);
        var count = size / block.size;
        if (size % block.size != 0) count++;
        for (int i = 0; i < count; i++)
        {
            block = _fat._drive.ReadBlock(blockId);
            ret.AddRange(block.Data);
            block.blockId++;
        }
        
        _currentPosition += size;
        return ret.ToArray();
    }
    
}