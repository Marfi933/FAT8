namespace OS;

public class FAT
{
    internal Drive _drive { get; }
    private bool _isOpened { get; set; }
    private DirectoryTable[] _directoryTable { get; set; }
    internal BootBlock _bootBlock { get; set; }
    private int entries_per_table { get; set; }
    private int[] _fileAllocationTable { get; set; }
    private List<FileDescriptor> FileDescriptorList { get; } = new();


    // because first block is for file system, second block is for file allocation table and third block is for directory table

    // block 0 - boot block              -
    // block 1 - file allocation table   - > cluster 1 (0.th cluster, cause indexing from 0) 
    //                                   -
    // cluster 2 - directory table       -
    // cluster 3 and more - data         -
    // because of first cluster, i need minimum 2 sectors per cluster

    private void calculate_boot_block_values(ref ushort BPB_BytsPerSec, ref ushort BPB_SecPerClus,
        ref ushort BPB_RsvdSecCnt, ref int BPB_TotSec32, int _size)
    {
        var min = 2;
        var max = 10;

        BPB_SecPerClus = 2;
        BPB_BytsPerSec = 512;
        BPB_RsvdSecCnt = 2;
        BPB_TotSec32 = _size / 2 * 512 / BPB_BytsPerSec;
    }

    // create a new file system
    private void CreateNewFileSystem(ushort BPB_BytsPerSec = 512, ushort BPB_SecPerClus = 2, ushort BPB_RsvdSecCnt = 2,
        int BPB_TotSec32 = 8)
    {
        calculate_boot_block_values(ref BPB_BytsPerSec, ref BPB_SecPerClus, ref BPB_RsvdSecCnt, ref BPB_TotSec32,
            _drive.Size());
        var bootBlock = new BootBlock(_drive, 0 * BPB_SecPerClus, BPB_BytsPerSec, BPB_SecPerClus, BPB_RsvdSecCnt,
            BPB_TotSec32);
        _bootBlock = bootBlock;

        if (_drive.Size() * 512 < BPB_BytsPerSec * BPB_SecPerClus * BPB_TotSec32)
            throw new DriverException(
                $"Driver is too small for this file system. size = {_drive.Size() * 512}, needed = {BPB_BytsPerSec * BPB_SecPerClus * BPB_TotSec32}");


        entries_per_table = BPB_BytsPerSec * BPB_SecPerClus / 32;

        var fatSize = BPB_TotSec32 * BPB_SecPerClus;

        var fileAllocationTable = new byte[fatSize];
        var directoryTable = new DirectoryTable[entries_per_table];
        _directoryTable = directoryTable;

        for (var index = 0; index < fileAllocationTable.Length; index++) fileAllocationTable[index] = (byte)Flag.free;

        for (var i = 0; i < entries_per_table; i++) directoryTable[i] = new DirectoryTable("");

        _drive.WriteBlock(bootBlock);

        var fat = new Block(1 * _bootBlock.BPB_SecPerClus, _drive);

        fileAllocationTable[0] = (byte)Flag.reserved;
        fileAllocationTable[1] = (byte)Flag.reserved;

        for (var i = 3; i < fat.Data.Length; i++) fat.Data[i] = (byte)Flag.free;

        for (var i = 0; i < fatSize; i++) fat.Data[i] = fileAllocationTable[i];

        _fileAllocationTable = fileAllocationTable.Select(item => (int)item).ToArray();
        writeAlocationTable();

        _drive.WriteBlock(fat);
        writeDirectoryTable();
    }

    // set value to file allocation table
    public void set_AllocationTable(int index, int value)
    {
        _fileAllocationTable[index] = value;
    }

    // finds value in file allocation table
    public int get_AllocationTable(int cluster)
    {
        return _fileAllocationTable[cluster];
    }

    // write directory table to drive
    private void writeDirectoryTable()
    {
        var directoryTableBytes = new byte[_directoryTable.Length * 32];
        for (var i = 0; i < _directoryTable.Length; i++)
        {
            var entryBytes = _directoryTable[i].ToBytes();
            Array.Copy(entryBytes, 0, directoryTableBytes, i * 32, entryBytes.Length);
        }


        for (var i = 2; i < 2 * _bootBlock.BPB_SecPerClus; i++)
        {
            var block = new Block(i, _drive);
            var bytes = new byte[_bootBlock.BPB_BytsPerSec];
            Array.Copy(directoryTableBytes, (i - 2) * _bootBlock.BPB_BytsPerSec, bytes, 0, bytes.Length);
            block.Data = bytes;
            _drive.WriteBlock(block);
        }
    }

    // write file allocation table to drive
    public void writeAlocationTable()
    {
        var block = new Block(1, _drive);
        var fileAllocationTableBytes = new byte[_fileAllocationTable.Length];
        for (var i = 0; i < _fileAllocationTable.Length; i++)
            fileAllocationTableBytes[i] = (byte)_fileAllocationTable[i];
        block.Data = fileAllocationTableBytes;
        _drive.WriteBlock(block);
    }

    // read directory table from drive
    private void readDirectoryTable()
    {
        var data = new List<byte>();

        for (var i = 2; i < 2 * _bootBlock.BPB_SecPerClus; i++)
        {
            var block_ = _drive.ReadBlock(i);
            data.AddRange(block_.Data);
        }

        var directoryTableBytes = data.ToArray();

        for (var i = 0; i < _directoryTable.Length; i++)
        {
            var entryBytes = new byte[32];
            Array.Copy(directoryTableBytes, i * 32, entryBytes, 0, entryBytes.Length);
            _directoryTable[i] = DirectoryTable.FromBytes(entryBytes);
        }
    }

    // read file allocation table from drive (if exists)
    private void ReadFileSystem()
    {
        var bootBlock = new BootBlock(_drive, 0);
        _bootBlock = bootBlock;

        var bytesPerFATEntry = 1;


        entries_per_table = _bootBlock.BPB_BytsPerSec * _bootBlock.BPB_SecPerClus / 32;

        var fatSize = _bootBlock.BPB_TotSec32 * bytesPerFATEntry;

        var fileAllocationTable = new byte[fatSize];
        var directoryTable = new DirectoryTable[entries_per_table];

        var al_t = _drive.ReadBlock(1);

        for (var i = 0; i < fatSize; i++) fileAllocationTable[i] = al_t.Data[i];

        for (var i = 0; i < entries_per_table; i++) directoryTable[i] = new DirectoryTable("");
        _directoryTable = directoryTable;

        readDirectoryTable();

        _fileAllocationTable = fileAllocationTable.Select(item => (int)item).ToArray();
    }

    // find free directory table
    private DirectoryTable FindFreeDirectoryTable(string name, int first_cluster, int size, int type)
    {
        foreach (var t in _directoryTable)
            if (t is { name: "", first_cluster: -1, size: -1, type: -1 })
            {
                t.name = name;
                t.first_cluster = first_cluster;
                t.size = size;
                t.type = type;
                return t;
            }

        return null;
    }

    // find free block in file allocation table (cluster =2 because first cluster is for file allocation table, 0.th is for boot block and fat)
    internal int findFreeBlock(int cluster = 2)
    {
        for (var i = 0; i < _bootBlock.BPB_TotSec32; i++)
            if (_fileAllocationTable[i] == (int)Flag.free && i >= cluster)
                return i;

        return -1;
    }

    // create file in file system
    public void CreateFile(string filename)
    {
        var first_cluster = findFreeBlock();

        if (first_cluster == -1) throw new DriverException("There is no free block. You can't create file.");

        foreach (var t in _directoryTable)
            if (t.name == filename)
                throw new DriverException("File with this name already exists.");

        var freeDirectoryTable = FindFreeDirectoryTable(filename, first_cluster, 0, 0);
        set_AllocationTable(first_cluster, (int)Flag.end_of_file);

        if (freeDirectoryTable == null)
            throw new DriverException("There is no free directory table. You can't create file.");

        writeDirectoryTable();
        writeAlocationTable();
    }

    // open file in file system
    public FileDescriptor OpenFile(string filename)
    {
        var file = find_file(filename);
        if (file != null) return file;
        CreateFile(filename);
        return OpenFile(filename);
    }

    // find file in file system
    public FileDescriptor find_file(string filename)
    {
        foreach (var t in _directoryTable)
            if (t.name == filename)
            {
                var file = new FileDescriptor(filename, t.size, this);
                if (FileDescriptorList.Contains(file)) throw new DriverException("File is already opened.");
                FileDescriptorList.Add(file);
                return file;
            }

        return null;
    }

    // find directory table in file system
    public DirectoryTable findDirectoryTable(string filename)
    {
        foreach (var t in _directoryTable)
            if (t.name == filename)
                return t;

        return null;
    }

    // count used clusters in file system
    public int countUsedCluster(DirectoryTable dt)
    {
        var count = 0;
        var cluster = dt.first_cluster;

        while (cluster != (int)Flag.end_of_file)
        {
            count++;
            cluster = get_AllocationTable(cluster);
        }

        return count;
    }

    public void allocateClusters(int usedClusters, int clusters, DirectoryTable dt)
    {
        for (var i = 0;; i++)
        {
            if (clusters == usedClusters)
            {
                break;
            }

            else if (clusters > usedClusters)
            {
                var new_cluster = findFreeBlock();
                var last_cluster = dt.first_cluster;
                while (get_AllocationTable(last_cluster) != (int)Flag.end_of_file)
                    last_cluster = get_AllocationTable(last_cluster);
                set_AllocationTable(last_cluster, new_cluster);
                set_AllocationTable(new_cluster, (int)Flag.end_of_file);
                var bytes = new byte[_bootBlock.BPB_BytsPerSec * _bootBlock.BPB_SecPerClus];

                for (var j = new_cluster; j < new_cluster * _bootBlock.BPB_SecPerClus; j++)
                {
                    var block = new Block(j, _drive);
                    block.Data = bytes[i..(i + block.size)];
                    _drive.WriteBlock(block);
                }

                usedClusters++;
            }
        }
    }

    public void releaseClusters(int usedClusters, int clusters, DirectoryTable dt)
    {
        for (var i = 0;; i++)
        {
            if (clusters == usedClusters)
            {
                break;
            }

            else if (clusters < usedClusters)
            {
                var last_cluster = dt.first_cluster;
                var previous_cluster = dt.first_cluster;

                while (get_AllocationTable(last_cluster) != (int)Flag.end_of_file)
                    last_cluster = get_AllocationTable(last_cluster);
                set_AllocationTable(last_cluster, (int)Flag.free);
                last_cluster = dt.first_cluster;
                while (true)
                {
                    previous_cluster = last_cluster;
                    last_cluster = get_AllocationTable(last_cluster);
                    if (last_cluster == (int)Flag.free ||
                        get_AllocationTable(last_cluster) == (int)Flag.free) break;
                }

                set_AllocationTable(previous_cluster, (int)Flag.end_of_file);

                usedClusters--;
            }
        }
    }

    // change size of file in file system
    public void ChangeSize(FileDescriptor file, int size)
    {
        var dt = findDirectoryTable(file.filename);

        var sizePerCluster = _bootBlock.BPB_BytsPerSec * _bootBlock.BPB_SecPerClus;

        var clusters = size / sizePerCluster;

        if (size % sizePerCluster != 0) clusters++;

        var usedClusters = countUsedCluster(dt);

        if (size > file.size)
            allocateClusters(usedClusters, clusters, dt);
        else if (size < file.size) releaseClusters(usedClusters, clusters, dt);

        file.size = size;
        dt.size = size;
        writeDirectoryTable();
    }

    // list files in file system (filenames)
    public List<string> ListFiles()
    {
        var list = new List<string>();
        foreach (var t in _directoryTable)
            if (t.name != "")
                list.Add(t.name);

        return list;
    }

    // delete file in file system
    public void file_delete(string filename)
    {
        var file = find_file(filename);
        ChangeSize(file, 0);
        var dt = findDirectoryTable(filename);
        set_AllocationTable(dt.first_cluster, (int)Flag.free);
        dt.name = "";
        dt.first_cluster = -1;
        dt.size = -1;
        dt.type = -1;
        writeDirectoryTable();
    }

    public FAT(Drive drive)
    {
        _drive = drive ?? throw new ArgumentNullException(nameof(drive));
        if (_drive.Size() <= 0)
            throw new DriverException("Driver hasn't any blocks. That means it's not initialized. (size = 0)");
        _drive.Open();
        var first_block = _drive.ReadBlock(0);

        /*for (int i = 0; i < entries_per_table; i++)
        {
            _directoryTable[i] = new DirectoryTable("abcefghijkk");
        }*/

        // i assume that first block is fat, so if it's not, then it's not initialized (there are 512 bytes of space)
        // it means that first block is empty and second block is empty too (there are 512 bytes of space)
        if (first_block.Data.All(item => item == 0))
        {
            CreateNewFileSystem();
            return;
        }

        ReadFileSystem();
        _drive.Close();
    }

    public void Open()
    {
        _isOpened = true;
        _drive.Open();
    }

    public void Close()
    {
        _isOpened = false;
        _drive.Close();
    }

    public bool IsOpened()
    {
        return _isOpened;
    }
    
    /// <summary>
    /// Defragmentate file system, release all unused clusters
    /// </summary>
    public void Defragmentate()
    {
        var usedClusters = new List<int>();

        for (var i = 0; i < _fileAllocationTable.Length; i++)
            // if cluster is not free or reserved, then it's used, so add to list
            if (_fileAllocationTable[i] != (int)Flag.free && _fileAllocationTable[i] != (int)Flag.reserved)
                usedClusters.Add(i);

        foreach (var dt in _directoryTable)
        {
            // remove clusters from list, which are used by file
            if (dt != null && dt.name != "") {
                int cluster = dt.first_cluster;
                while (cluster != (int)Flag.end_of_file)
                {
                    usedClusters.Remove(cluster);
                    cluster = get_AllocationTable(cluster);
                }
            }
            
            // when was write file, but not saved, so it's not ib dt -> release clusters
            else if (dt == null || dt.name == "") {
                break;
            }
        }
        
        // release clusters
        foreach (var cluster in usedClusters) {
            set_AllocationTable(cluster, (int)Flag.free);
        }
        
        writeAlocationTable();
    }
}