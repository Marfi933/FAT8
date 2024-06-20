using System.IO.MemoryMappedFiles;

namespace OS;

// I'm encoding or decoding in UTF-8 - due the czech language (diacritics) - it has got sometimes more than 1 byte per char
public class Drive
{
    private readonly string _fileName;
    private readonly string _filepath;
    private MemoryMappedFile _memoryMappedFile { get; set; }

    private int _size;

    public int size
    {
        get => _size;
        set
        {
            if (value <= 0) throw new DriverException("Size cannot be less or equal to 0!");

            _size = value;
            ChangeSizeOfFile();
        }
    }

    private readonly int _blockSize = 0x0000000200;

    private bool _isOpened { get; set; }
    

    private void ChangeSizeOfFile()
    {
        using (var fileStream = File.OpenWrite(_filepath))
        {
            fileStream.SetLength(_blockSize * _size);
        }
    }

    private void CreateFileIfNotExists()
    {
        if (!File.Exists(_fileName))
            using (var fileStream = File.Create(_filepath))
            {
                fileStream.SetLength(0x0000000200 * _size);
            }
    }

    public Drive(string fileName)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _filepath = Path.GetFullPath(fileName);

        if (File.Exists(_fileName))
            using (var file = File.OpenRead(_fileName))
            {
                _size = (int)(file.Length / _blockSize);
            }
        else
            CreateFileIfNotExists();
    }


    public Drive(string fileName, int size)
    {
        _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        _filepath = Path.GetFullPath(fileName);
        this.size = size;


        if (File.Exists(fileName))
            using (var file = File.OpenRead(_fileName))
            {
                _size = (int)(file.Length / _blockSize);
            }
        else
            CreateFileIfNotExists();
    }

    public void WriteBlock(Block block)
    {
        WriteBlock(block.blockId, block.Data);
    }

    public Block ReadBlock(int blockId)
    {
        if (IsOpened())
        {
            var block = new Block(blockId, this);
            long offset = blockId * _blockSize;
            var accessor = _memoryMappedFile.CreateViewAccessor(offset, _blockSize);
            using (accessor)
            {
                accessor.ReadArray(0, block.Data, 0, block.Data.Length);
            }

            return block;
        }

        throw new DriverException("Cannot read, because drive is not opened!");
    }

    public void WriteBlock(int blockId, byte[] data)
    {
        if (IsOpened())
        {
            long offset = blockId * _blockSize;
            var accessor = _memoryMappedFile.CreateViewAccessor(offset, _blockSize);
            var length = data.Length < 512 ? data.Length : 512;
            using (accessor)
            {
                accessor.WriteArray(0, data[..length], 0, length);
            }

            return;
        }

        throw new DriverException("Cannot write, because drive is not opened!");
    }

    public void Open()
    {
        _isOpened = true;
        try
        {
            _memoryMappedFile = MemoryMappedFile.CreateFromFile(_filepath, FileMode.Open);
        }
        catch (IOException)
        {
        }
    }

    public void Close()
    {
        _isOpened = false;
        _memoryMappedFile.Dispose();
    }

    public bool IsOpened()
    {
        return _isOpened;
    }

    public int Size()
    {
        return size;
    }
}