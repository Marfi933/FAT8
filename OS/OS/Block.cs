namespace OS;

public class Block
{
    public int size { get; } = 0x0000000200;

    public byte[] Data
    {
        get => _data;
        set
        {
            if (value.Length > size)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            } 
        
            _data = value;
        }
    }

    private byte[] _data;
    public int blockId { get; set; }
    
    public Block(int id, Drive drive)
    {
        if (id < 0 || id > drive.Size()) throw new ArgumentOutOfRangeException(nameof(id));
        blockId = id;
        Data = new byte[size];
    }
}