using System.Text;
using OS;

public class Program
{
    private static void drv_manufacture(string fileName, int size)
    {
        var drive = new Drive(fileName, size);
    }

    private static Drive drv_open(string fileName)
    {
        var drive = new Drive(fileName);
        drive.Open();
        return drive;
    }

    private static int drv_stat(Drive drive)
    {
        return drive.IsOpened() ? drive.Size() : -1;
    }

    private static byte[] drv_read(Drive drive, int blockId)
    {
        return drive.ReadBlock(blockId).Data;
    }

    private static string drv_read_string(Drive drive, int blockId)
    {
        return Encoding.UTF8.GetString(drv_read(drive, blockId));
    }

    public static string drv_read_string(Drive drive, int blockId, TypeCoding typeCoding)
    {
        return typeCoding switch
        {
            TypeCoding.UTF8 => Encoding.UTF8.GetString(drv_read(drive, blockId)),
            TypeCoding.ASCII => Encoding.ASCII.GetString(drv_read(drive, blockId)),
            _ => Encoding.UTF8.GetString(drv_read(drive, blockId))
        };
    }

    private static void drv_write(Drive drive, int blockId, byte[] data)
    {
        drive.WriteBlock(blockId, data);
    }

    private static void drv_write_string(Drive drive, int blockId, string data)
    {
        drv_write(drive, blockId, Encoding.UTF8.GetBytes(data));
    }

    private static void drv_close(Drive drive)
    {
        drive.Close();
    }

    private static void fs_format(Drive drive)
    {
        var fat = new FAT(drive);
    }

    private static FAT fs_open(Drive drive)
    {
        var fat = new FAT(drive);
        fat.Open();
        return fat;
    }

    private static void fs_close(FAT fat)
    {
        fat.Close();
    }

    private static FileDescriptor file_open(FAT fat, string filename)
    {
        return fat.OpenFile(filename);
    }

    private static void file_truncate(FileDescriptor file, int size)
    {
        file.fileTruncate(size);
    }

    private static void file_seek(FileDescriptor file, int size)
    {
        file.fileSeek(size);
    }

    private static int file_stat(FileDescriptor file)
    {
        return file.fileStat();
    }

    private static int file_tell(FileDescriptor file)
    {
        return file.fileTell();
    }

    private static void file_write(FileDescriptor file, byte[] data)
    {
        file.fileWrite(data);
    }

    private static byte[] file_read(FileDescriptor file, int size)
    {
        return file.fileRead(size);
    }

    private static List<string> file_readdir(FAT fat)
    {
        return fat.ListFiles();
    }

    private static void file_delete(FAT fat, string filename)
    {
        fat.file_delete(filename);
    }


    public static void TestDrive(string filename)
    {
        Console.WriteLine($"TestDrive: {filename}");
        drv_manufacture(filename, 10);
        var drive = drv_open(filename);

        var data =
            "Vítr skoro nefouká a tak by se na první pohled mohlo zdát, že se balónky snad vůbec nepohybují. Jenom tak klidně levitují ve vzduchu. Jelikož slunce jasně září a na obloze byste od východu k západu hledali mráček marně, balónky působí jako jakási fata morgána uprostřed pouště. Zkrátka široko daleko nikde nic, jen zelenkavá tráva, jasně modrá obloha a tři křiklavě barevné pouťové balónky, které se téměř nepozorovatelně pohupují ani ne moc vysoko, ani moc nízko nad zemí. Kdyby pod balónky nebyla sytě zelenkavá tráva, ale třeba suchá silnice či beton, možná by bylo vidět jejich barevné stíny - to jak přes poloprůsvitné barevné balónk prochází ostré sluneční paprsky.";

        drv_write_string(drive, 0, data);
        Console.Write(drv_read_string(drive, 0));
        Console.WriteLine(drv_read_string(drive, 1));

        drv_write_string(drive, 3, "Hello world! 2");
        Console.WriteLine(drv_read_string(drive, 3));

        Console.WriteLine(drv_stat(drive));

        drv_close(drive);
    }

    public static void ThreadTestDrive()
    {
        var thread = new Thread(() => TestDrive("C.txt"));
        var thread2 = new Thread(() => TestDrive("D.txt"));

        thread.Start();
        thread2.Start();

        thread.Join();
        thread2.Join();
    }

    public static void FAT_test()
    {
        var drive = new Drive("D.txt", 25);
        var fat = new FAT(drive);
        fat.Open();
        var st =
            "Uvidět tak balónky náhodný kolemjdoucí, jistě by si pomyslel, že už tu takhle poletují snad tisíc let. Stále si víceméně drží výšku a ani do stran se příliš nepohybují. Proti slunci to vypadá, že se slunce pohybuje k západu rychleji než balónky, a možná to tak skutečně je. Nejeden filozof by mohl tvrdit, že balónky se sluncem závodí, ale fyzikové by to jistě vyvrátili. Z fyzikálního pohledu totiž balónky působí zcela nezajímavě. Nejvíc bezpochyby zaujmou děti - jedna malá holčička zrovna včera div nebrečela, že by snad balónky mohly prasknout. A co teprve ta stuha. Stuha, kterou je každý z trojice balónků uvázán, aby se nevypustil. Očividně je uvázaná dostatečně pevně, protože balónky skutečně neucházejí. To ale není nic zvláštního. Překvapit by však mohl fakt, že nikdo, snad krom toho, kdo balónky k obloze vypustil, netuší, jakou má ona stuha barvu. Je totiž tak lesklá, že za světla se v ní odráží nebe a za tmy zase není vidět vůbec. Když svítí slunce tak silně jako nyní, tak se stuha třpytí jako kapka rosy a jen málokdo vydrží dívat se na ni přímo déle než pár chvil. Jak vlastně vypadají ony balónky?. Ptají se často lidé. Inu jak by vypadaly - jako běžné pouťové balónky střední velikosti, tak akorát nafouknuté. Červený se vedle modrého a zeleného zdá trochu menší, ale to je nejspíš jen optický klam, a i kdyby byl skutečně o něco málo menší, tak vážně jen o trošičku. Vítr skoro nefouká a tak by se na první pohled mohlo zdát, že se balónky snad vůbec nepohybují. Jenom tak klidně levitují ve vzduchu. Jelikož slunce jasně září a na obloze byste od východu k západu hledali mráček marně, balónky působí jako jakási fata morgána uprostřed pouště. Zkrátka široko daleko nikde nic, jen zelenkavá tráva, jasně modrá obloha a tři křiklavě barevné pouťové balónky, které se téměř nepozorovatelně pohupují ani ne moc vysoko, ani moc nízko nad zemí.";
        var data = Encoding.UTF8.GetBytes(st);
        var file = fat.OpenFile("my_file.txt");
        fat.ChangeSize(file, 100);
        file.fileWrite(data);
        var data3 = file.fileRead(500);
        fat.set_AllocationTable(6, 7);
        fat.set_AllocationTable(7, 8);
        fat.set_AllocationTable(8, 9);
        fat.set_AllocationTable(9, (int)Flag.end_of_file);
        fat.Defragmentate();
        Console.WriteLine(Encoding.UTF8.GetString(data3));
        var list = fat.ListFiles();
        fat.file_delete(file.filename);
        fat.Close();
    }

    public static void Main()
    {
        //TestDrive("D.txt");
        //ThreadTestDrive();
        FAT_test();
        
    }
}