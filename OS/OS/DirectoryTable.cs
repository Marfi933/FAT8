using System.Text;

namespace OS;

public class DirectoryTable
{
    public string name { get; set; }
    internal int first_cluster { get; set; }
    internal int size { get; set; }
    internal int type { get; set; }
    
    public DirectoryTable(string name, int first_cluster, int size, int type)
    {
        if (name.Length > 11) throw new Exception("Name is too long.");
        
        this.name = name;
        this.first_cluster = first_cluster;
        this.size = size;
        this.type = type;
    }

    public DirectoryTable(string name)
    {
        if (name.Length > 11) throw new Exception("Name is too long.");
        
        this.name = name;
        this.first_cluster = -1;
        this.size = -1;
        this.type = -1;
    }
    
    public byte[] ToBytes()
    {
        List<byte> bytes = new List<byte>();
        byte[] nameBytes = Encoding.ASCII.GetBytes(name.PadRight(11, '\0'));
        bytes.AddRange(nameBytes);
        bytes.AddRange(BitConverter.GetBytes(first_cluster));
        bytes.AddRange(BitConverter.GetBytes(size));
        bytes.AddRange(BitConverter.GetBytes(type));

        return bytes.ToArray();
    }
    
    public static DirectoryTable FromBytes(byte[] bytes)
    {
        string name = Encoding.ASCII.GetString(bytes, 0, 11).TrimEnd('\0');
        int firstCluster = BitConverter.ToInt32(bytes, 11);
        int size = BitConverter.ToInt32(bytes, 15);
        int type = BitConverter.ToInt32(bytes, 19);

        return new DirectoryTable(name, firstCluster, size, type);
    }
}