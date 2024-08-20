using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Text;
namespace OS_3
{
    public class DirectoryEntry
    {

        public char[] File_Name = new char[11];
        public byte File_Att;
        public byte[] File_Empty = new byte[12];
        public int File_Size = 0;
        public int First_Clustre;
        public DirectoryEntry()
        {

        }
        public DirectoryEntry(char[] fn, byte fa, int fc, int fz)
        {
            string s = new string(fn);
            if (s.Length < 11)
            {
                for (int i = s.Length; i < 11; i++)
                    s+=" ";

            }
            this.File_Name = s.ToCharArray();
            this.File_Att = fa;
            this.First_Clustre = fc;
            this.File_Size = fz;

        }

        //public int FirstClustreProperty
        //{
        //    get
        //    {
        //        return this.First_Clustre;
        //    }
        //    set
        //    {
        //        this.First_Clustre = value;
        //    }

        //}

        public byte[] getBytes()
        {
            byte[] b = new byte[32];
            byte[] FirstClustre_Array = BitConverter.GetBytes(First_Clustre);
            byte[] FileSize_Array = BitConverter.GetBytes(File_Size);
            for (int i = 0; i < File_Name.Length; i++)
            {
                b[i] = Convert.ToByte(File_Name[i]);
            }

            b[11] = File_Att;

            for (int i = 12, c = 0; c < File_Empty.Length; i++, c++)
            {
                b[i] = File_Empty[c];
            }

            for (int i = 24, c = 0; i < 28; i++, c++)
            {
                b[i] = FirstClustre_Array[c];
            }

            for (int i = 28, c = 0; i < 32; i++, c++)
            {
                b[i] = FileSize_Array[c];
            }

            return b;

        }
        public DirectoryEntry getDirEntry(byte[] b)
        {
            DirectoryEntry d = new DirectoryEntry();
            byte[] FirstClustre_Array = new byte[4];
            byte[] FileSize_Array = new byte[4];
            for (int i = 0; i < File_Name.Length; i++)
            {
                d.File_Name[i] = Convert.ToChar(b[i]);
            }

            d.File_Att = b[11];

            for (int i = 12, c = 0; c < File_Empty.Length; i++, c++)
            {
                d.File_Empty[c] = b[i];
            }

            for (int i = 24, c = 0; i < 28; i++, c++)
            {
                FirstClustre_Array[c] = b[i];
            }
            d.First_Clustre = BitConverter.ToInt32(FirstClustre_Array, 0);

            for (int i = 28, c = 0; i < 32; i++, c++)
            {
                FileSize_Array[c] = b[i];
            }
            d.File_Size = BitConverter.ToInt32(FileSize_Array, 0);


            return d;
        }

        public DirectoryEntry get_DirEntry()
        {
            DirectoryEntry d = new DirectoryEntry();

            for (int i = 0; i < File_Name.Length; i++)
            {
                d.File_Name[i] = this.File_Name[i];
            }

            d.File_Att = this.File_Att;

            for (int i = 0; i < File_Empty.Length; i++)
            {
                d.File_Empty[i] = this.File_Empty[i];
            }

            d.First_Clustre = this.First_Clustre;


            d.File_Size = this.File_Size;


            return d;
        }
    }
    public class Directory : DirectoryEntry
    {
        public List<DirectoryEntry> dirTable;
        //dirTable.Add(new dirEntry() { fName = "mina", PartId = 1234 });

        public Directory parent;
        public Directory(char[] fn, byte fa, int fc, int fz, Directory p) : base(fn, fa, fc, fz)
        {
            dirTable = new List<DirectoryEntry>();

            if (p != null)
            { this.parent = p; }

        }

        //FAT obj = new FAT();
        //Virtual_Disk obj1 = new Virtual_Disk();
        public void writeDir()
        {
            byte[] DirTable_Bytes = new byte[32 * dirTable.Count];
            for (int i = 0; i < dirTable.Count; i++)
            {
                byte[] DE_Bytes = dirTable[i].getBytes();

                for (int j = i * 32, c = 0; c < 32; c++, j++)
                {
                    DirTable_Bytes[j] = DE_Bytes[c];
                }
            }
            double numberOfRequiredBlocks = (DirTable_Bytes.Length / 1024.0);
            double num_of_required_blocks = Math.Ceiling(numberOfRequiredBlocks);
            int num_of_fullsize_blocks = (DirTable_Bytes.Length / 1024);
            int num_of_reminder_blocks = (DirTable_Bytes.Length % 1024);



            if (num_of_required_blocks <= FAT.getAvailableBlocks())
            {
                int FAT_index;
                int last_index = -1;
                //int First_Clustre = FirstClustreProperty;

                if (First_Clustre != 0)
                { FAT_index = First_Clustre; }
                else
                {
                    FAT_index = FAT.GetAvailableBlock();
                    First_Clustre = FAT_index;
                }

                List<byte[]> b = new List<byte[]>();
                for (int i = 0; i < num_of_fullsize_blocks; i++)    //num_of_required_blocks or num_of_fullsize_blocks?
                {
                    byte[] inter_byte = new byte[1024];
                    for (int p = 0; p < inter_byte.Length; p++)
                    {


                        inter_byte[p] = DirTable_Bytes[(1024 * i) + p];
                    }

                    b.Add(inter_byte);
                }
                if (num_of_reminder_blocks > 0)
                {
                    int start = num_of_fullsize_blocks * 1024;
                    byte[] bb = new byte[1024];
                    for (int i = start; i < (start + num_of_reminder_blocks); i++)
                    {

                        bb[i] = DirTable_Bytes[i % 1024];

                    }
                    b.Add(bb);



                }

                for (int i = 0; i < b.Count; i++)
                {
                    Virtual_Disk.WriteBlock(b[i], FAT_index);

                    FAT.SetNext(FAT_index, -1);

                    if (last_index != -1)
                    { FAT.SetNext(last_index, FAT_index); }

                    last_index = FAT_index;
                    FAT_index = FAT.GetAvailableBlock();
                }
                if (dirTable.Count == 0)
                {
                    if (First_Clustre != 0)
                    {
                        FAT.SetNext(First_Clustre, 0);
                        First_Clustre = 0;


                    }


                }


                FAT.Write_In_FT();
            }

        }

        public void readDir()
        {
            dirTable = new List<DirectoryEntry>();
            List<byte> Ls = new List<byte>();
            int FAT_index = First_Clustre;
            int next = FAT.GetNext(FAT_index);
            if (First_Clustre != 0 && FAT.GetNext(First_Clustre) != 0)
            {
                do
                {
                    Ls.AddRange(Virtual_Disk.GetBlock(FAT_index));
                    FAT_index = next;
                    if (FAT_index != -1)
                    { next = FAT.GetNext(FAT_index); }
                } while (next != -1);

                byte[] b = new byte[32];
                for (int i = 0; i < Ls.Count; i++)
                {
                    b[i % 32] = Ls[i];
                    if ((i + 1) % 32 == 0)
                    {
                        DirectoryEntry d = this.getDirEntry(b);
                        if (d.File_Name[0] != '\0')
                        {
                            dirTable.Add(d);
                        }

                    }
                }
                //for (int i = 0; i < Ls.Count; i++)
                //{
                //    //byte[] b = new byte[32];
                //    for (int p = 0; p < b.Length; p++)
                //    {
                //        if (((32 * i) + p) >= Ls.Count)
                //        { break; }

                //        b[p] = Ls[(32 * i) + p];
                //    }
                //    dirTable.Add(getDirEntry(b));
                //}
            }
        }
        public int searchDir(string fname)
        {
            readDir();
            if (fname.Length < 11)
            {
                for (int i = fname.Length; i < 11; i++)
                {
                    fname += " ";
                }
            }
            else if (fname.Length > 11)
            {
                Console.WriteLine("The Length of file name is > 11");
                return 0;
            }

            for (int i = 0; i < dirTable.Count; i++)
            {
                string filename = new string(dirTable[i].File_Name);
                if (string.Equals(filename, fname))
                { return i; }

            }

            return -1;
        }

        public void update_content(DirectoryEntry d)
        {
            readDir();
            string filename = new string(d.File_Name);
            int index = searchDir(filename);

            if (index != -1)
            {
                dirTable.RemoveAt(index);
                dirTable.Insert(index, d);
            }
            else
            {
                Console.WriteLine("The Directory not found to update");
            }
        }

        public void deleteDirectory()
        {
            if (First_Clustre != 0)
            {
                int index = First_Clustre;
                int next = FAT.GetNext(index);
                do
                {
                    FAT.SetNext(index, 0);
                    index = next;
                    if (index != -1)
                    { next = FAT.GetNext(index); }

                } while (index != -1);
            }


            if (this.parent != null)
            {
                parent.readDir();
                string filename = new string(File_Name);
                int i = parent.searchDir(filename);
                if (i != -1)
                { parent.dirTable.RemoveAt(i); }
                parent.writeDir();
            }
            FAT.Write_In_FT();
        }

    }

    //public class FileEntry : DirectoryEntry
    //{
    //    public int fat_index;
    //    public int last_index;
    //    public Directory parent;
    //    public string File_Content;
    //    public FileEntry(char[] fn, byte fa, int fc, int fz, Directory p, string fcontent) : base(fn, fa, fc, fz)
    //    {
    //        this.File_Content = fcontent;
    //        this.parent = p;

    //    }
    //    public void write_file()
    //    {
    //        byte[] contentBytes = Encoding.ASCII.GetBytes(File_Content);
    //        double numOfBlocks = contentBytes.Length / 1024.0;
    //        int numOfRquiredBlock = Convert.ToInt32(Math.Ceiling(numOfBlocks));
    //        int numOfFullSizeBlock = Convert.ToInt32(Math.Floor(numOfBlocks));
    //        double reminder = contentBytes.Length * 1024;
    //        if (numOfBlocks <= FAT.getAvailableBlocks())
    //        {
    //            if (First_Clustre != 0)
    //            {
    //                fat_index = First_Clustre;
    //            }
    //            else
    //            {
    //                fat_index = FAT.getAvailableBlocks();
    //                First_Clustre = fat_index;
    //            }
    //            List<byte[]> ls = new List<byte[]>();
    //            for (int i = 0; i < numOfFullSizeBlock; i++)
    //            {
    //                byte[] b = new byte[1024];
    //                for (int j = 0; j < File_Content.Length; i++)
    //                {
    //                    b[j % 1024] = contentBytes[j];
    //                    if ((j + 1) % 1024 == 0)
    //                    {
    //                        ls.Add(b);
    //                    }
    //                }
    //            }
    //            if (reminder > 0)
    //            {
    //                byte[] b = new byte[1024];
    //                int start = numOfFullSizeBlock * 1024;
    //                for (int j = 0; j < reminder; j++)
    //                {
    //                    b[j % 1024] = contentBytes[start];
    //                    ls.Add(b);

    //                }
    //                for (int i = 0; i < ls.Count; i++)
    //                {
    //                    Virtual_Disk.WriteBlock(ls[i], fat_index);
    //                    FAT.SetNext(fat_index, -1);
    //                    if (last_index != -1)
    //                    {
    //                        FAT.SetNext(last_index, fat_index);
    //                    }
    //                    last_index = fat_index;

    //                    fat_index = FAT.GetAvailableBlock();

    //                }

    //            }

    //        }
    //        FAT.Write_In_FT();
    //    }


    //    public void readFile()
    //    {
    //        if (First_Clustre != 0 && FAT.GetNext(First_Clustre) != 0)
    //        {
    //            List<byte> ls = new List<byte>();
    //            int next;
    //            fat_index = First_Clustre;
    //            next = FAT.GetNext(fat_index);
    //            do
    //            {
    //                ls.AddRange(Virtual_Disk.GetBlock(fat_index));
    //                fat_index = next;
    //                if (fat_index != -1)
    //                {
    //                    next = FAT.GetNext(fat_index);
    //                }

    //            } while (next != -1);
    //            byte[] arr = new byte[32];
    //            for (int i = 0; i < ls.Count; i++)
    //            {
    //                arr[i % 32] = ls[i];
    //                if ((i + 1) % 32 == 0)
    //                {
    //                    File_Content += arr.ToString();
    //                }
    //            }
    //        }
    //}   }
    //    public void deletedirectory(string name)
    //    {
    //        int next;
    //        int index;
    //        //Q?--> do directory full or empty? 
    //        //ans--> by first_cluster , if it is ==0 -->empty else full
    //        if (first_cluster != 0)
    //        {
    //            //index == fatindex هو هو

    //            index = first_cluster;
    //            next = fat_table_class.get_block_index(index);
    //            //delet all subdirectoreys that exit in directory
    //            do
    //            {
    //                fat_table_class.set_next_index(index, 0);
    //                index = next;
    //                if (index != -1)
    //                {
    //                    next = fat_table_class.get_block_index(index);
    //                }

    //            }
    //            while (index != -1);
    //        }
    //        //check if directory is supdirectory (has a parent) and delet

    //        if (Parent != null) //directory has parent
    //        {
    //            //call readDirectory method to return dir_table that belong to parent
    //            Parent.readDirectory();
    //            //call search method to search in parent(dir_table بتاع الاب) about supdirectory by pathing file name or file directory and delet supdirectoey
    //            //return row where supdirectory exit
    //            index = Parent.searchdirectory(name);

    //            if (index != -1)
    //            {
    //                //remove row where supdirectory exit
    //                Parent.Dir_Table.RemoveAt(index);
    //                //write directory again after update because delet something(supdirectory) from it
    //                Parent.writeDirectory();

    //            }
    //            //call writeDirectory to update

    //        }
    //        fat_table_class.write_in_fattable();
    //    }


    //}
    public class FileEntry : DirectoryEntry
    {
        public Directory parent;
        public string File_Content;
        public FileEntry(char[] fn, byte fa, int fc, int fz, Directory p, string fcontent) : base(fn, fa, fc, fz)
        {
            this.File_Content = fcontent;
            this.parent = p;

        }

        public void write_file()
        {
            byte[] fileContent = Encoding.UTF8.GetBytes(File_Content);

            double numberOfRequiredBlocks = (fileContent.Length / 1024.0);
            double num_of_required_blocks = Math.Ceiling(numberOfRequiredBlocks);
            double num_of_fullsize_blocks = Math.Floor(numberOfRequiredBlocks);
            int num_of_reminder_blocks = (fileContent.Length % 1024);

            if (num_of_required_blocks <= FAT.getAvailableBlocks())
            {
                int FAT_index;
                int last_index = -1;

                FAT_index = First_Clustre;
                //if (First_Clustre != 0)
                //{ FAT_index = First_Clustre; }
                //else
                //{

                //}

                List<byte[]> b = new List<byte[]>();
                for (int i = 0; i < num_of_required_blocks; i++)    //num_of_required_blocks or num_of_fullsize_blocks?
                {
                    byte[] inter_byte = new byte[1024];
                    for (int p = 0; p < inter_byte.Length; p++)
                    {
                        if (((1024 * i) + p) >= fileContent.Length)
                        { break; }

                        inter_byte[p] = fileContent[(1024 * i) + p];
                    }

                    b.Add(inter_byte);


                    Virtual_Disk.WriteBlock(b[i], FAT_index);

                    FAT.SetNext(FAT_index, -1);

                    if (last_index != -1)
                    { FAT.SetNext(last_index, FAT_index); }

                    last_index = FAT_index;
                    FAT_index = FAT.GetAvailableBlock();
                }


            }
        }
        public void readFile()
        {
            int FATIndex;
            int Next;
            //List<Dir_Entry> dirEntry = new List<Dir_Entry>() ;
            List<byte> fileBytes = new List<byte>();
            FATIndex = First_Clustre;
            Next = FAT.GetNext(FATIndex);

            do
            {
                fileBytes.AddRange(Virtual_Disk.GetBlock(FATIndex));
                FATIndex = Next;
                if (FATIndex != -1)
                { Next = FAT.GetNext(FATIndex); }
            }
            while (Next != -1);

            ASCIIEncoding encoding = new ASCIIEncoding();
            File_Content = encoding.GetString(fileBytes.ToArray());
            // content = System.Text.Encoding.UTF8.GetString(fileBytes);
        }
        public void deleteFile()
        {
            if (First_Clustre != 0)
            {
                int index = First_Clustre;
                int next = FAT.GetNext(index);
                do
                {
                    FAT.SetNext(index, 0);
                    index = next;
                    if (index != -1)
                    { next = FAT.GetNext(index); }

                } while (index != -1);
            }


            if (this.parent != null)
            {
                parent.readDir();
                string filename = new string(File_Name);
                int i = parent.searchDir(filename);
                if (i != -1)
                { parent.dirTable.RemoveAt(i); }
                parent.writeDir();
            }
            FAT.Write_In_FT();
        }
    }

    public class Virtual_Disk
    {

        public static void Initialize()
        {
            if (!File.Exists(@"D:\OS-3\fat.txt"))
            {
                FileStream fWrite = new FileStream(@"H:\hadir\هدير\منهج 3 ترم ثاني\os\fat.txt",
                        FileMode.Create, FileAccess.Write, FileShare.None);

                char[] Super_Block = new char[1024];
                char[] Fat_Table = new char[1024 * 4];
                char[] Data_File = new char[1024 * 1019];
                for (int i = 0; i < Super_Block.Length; i++)
                {
                    Super_Block[i] = '0';
                }
                for (int i = 0; i < Fat_Table.Length; i++)
                {
                    Fat_Table[i] = '*';
                }
                for (int i = 0; i < Data_File.Length; i++)
                {
                    Data_File[i] = '#';
                }
                byte[] writeSB = Encoding.UTF8.GetBytes(Super_Block);

                byte[] writeFT = Encoding.UTF8.GetBytes(Fat_Table);
                byte[] writeDF = Encoding.UTF8.GetBytes(Data_File);
                fWrite.Write(writeSB, 0, Super_Block.Length);
                fWrite.Write(writeFT, 0, Fat_Table.Length);
                fWrite.Write(writeDF, 0, Data_File.Length);
                fWrite.Close();

                FAT.initialize();

                char[] root_name = { 'H' };
                Directory root = new Directory(root_name, 0x01, 5, 0, null);
                Program.current_directory = root;
                root.writeDir();
                //FAT.Write_In_FT();
            }
            else
            {
                FAT.FAT_TABLE = FAT.Get_FT();
                char[] root_name = { 'H' };
                Directory root = new Directory(root_name, 0x01, 5, 0, null);
                root.readDir();
                Program.current_directory = root;

                //Console.WriteLine("The file is exsist");
            }


        }

        public static void WriteBlock(byte[] data, int index)
        {
            FileStream fwrite = new FileStream(@"H:\hadir\هدير\منهج 3 ترم ثاني\os\fat.txt",
                     FileMode.Open, FileAccess.Write, FileShare.Write);
            int size;
            fwrite.Seek(1024 * index, SeekOrigin.Begin);

            if (data.Length > 1024)
            { size = 1024; }
            else
            { size = data.Length; }

            fwrite.Write(data, 0, size);
            fwrite.Close();

        }

        public static byte[] GetBlock(int index)
        {
            FileStream fRead = new FileStream(@"H:\hadir\هدير\منهج 3 ترم ثاني\os\fat.txt",
                    FileMode.Open, FileAccess.Read, FileShare.Read);

            fRead.Seek(1024 * index, SeekOrigin.Begin);

            byte[] readBl = new byte[1024];

            fRead.Read(readBl, 0, readBl.Length);
            fRead.Close();
            return readBl;


        }
    }
    public static class FAT
    {
        public static int[] FAT_TABLE;
        //public FAT()
        //{
        //    FAT_TABLE = new int[1024];

        //}
        public static void initialize()
        {
            FAT_TABLE = new int[1024];
            for (int i = 0; i < FAT_TABLE.Length; i++)
            {
                if (i < 5)
                { FAT_TABLE[i] = -1; }
                else
                { FAT_TABLE[i] = 0; }
            }
        }

        public static void Write_In_FT()
        {
            FileStream fWrite = new FileStream(@"H:\hadir\هدير\منهج 3 ترم ثاني\os\fat.txt",
                    FileMode.Open, FileAccess.Write, FileShare.Write);
            fWrite.Seek(1024, SeekOrigin.Begin);

            byte[] arr = new byte[1024 * 4];
            Buffer.BlockCopy(FAT_TABLE, 0, arr, 0, 1024 * sizeof(int));
            fWrite.Write(arr, 0, arr.Length);
            fWrite.Close();


        }
        public static int[] Get_FT()
        {
            FAT.initialize();
            FileStream fRead = new FileStream(@"H:\hadir\هدير\منهج 3 ترم ثاني\os\fat.txt",
                    FileMode.Open, FileAccess.Read, FileShare.Read);

            fRead.Seek(1024, SeekOrigin.Begin);
            //int[] FT = new int[1024];
            byte[] readft = new byte[4 * 1024];

            fRead.Read(readft, 0, readft.Length);
            Buffer.BlockCopy(readft, 0, FAT_TABLE, 0, readft.Length);


            fRead.Close();
            return FAT_TABLE;


        }

        public static void Print_FT()
        {
           // int[] FT = Get_FT();
            for (int i = 0; i < FAT_TABLE.Length; i++)
            {
                Console.WriteLine(i + " | " + FAT_TABLE[i]);
            }
        }

        public static int GetAvailableBlock()
        {
           // int[] FT = Get_FT();
            for (int i = 0; i < FAT_TABLE.Length; i++)
            {
                if (FAT_TABLE[i] == 0)
                {
                    return i;
                }

            }
            return 0;
        }
        public static int GetNext(int index)
        {
           // int[] FT = Get_FT();
            return FAT_TABLE[index];
        }
        public static void SetNext(int index, int value)
        {

            FAT_TABLE[index] = value;


        }

        public static int getAvailableBlocks()
        {
            int c = 0;
            for (int i = 0; i < 1024; i++)
            {
                if (FAT_TABLE[i] == 0)
                    c++;

            }
            return c;
        }

        static public int getFreeSpace()
        {
            int f;
            f = getAvailableBlocks() * 1024;
            return f;
        }

    }
    class Program
    {
        static public Directory current_directory;
        static public string current_path;
        static void Main(string[] args)
        {
            //current_path = new string(current_directory.File_Name);
            //string CurrenrDirectory = Environment.CurrentDirectory;
            //string[] commands = { "cls", "help", "exite", "del", "copy", "md", "rd", "rename", "type", "import", "export" };
            string c;
            bool x;
            var My_commands = new List<string>()
            {
                "cls", "help", "exite", "del", "copy","dir", "md", "cd","rd", "rename", "type", "import", "export"
            };

            //FAT obj = new FAT();
            //obj.initialize();
            //obj.Write_In_FT();

            Virtual_Disk.Initialize();
            //Console.WriteLine(current_directory.dirTable.Count);
            //Console.WriteLine(current_directory.First_Clustre);
            //FAT.Print_FT();

            //Console.WriteLine( obj.getAvailableBlocks());
            // obj.SetNext(5,-1);
            //int [] arr = obj.Get_FT();


            //Virtual_Disk obj1 = new Virtual_Disk();
            //obj1.Initialize();

            //DirectoryEntry d = new DirectoryEntry("Yomna", 0x01, 7);

            //char[] s = { 'a', 'b'};
            //string ss = "asdasdasd.txt";
            //Directory d = new Directory(s, 0x10, 5,null);
            //d.searchDir(ss);

            //byte[] bytes = d.getBytes();
            //for (int i = 0; i < bytes.Length; i++)
            //{
            //    Console.WriteLine(Convert.ToString(bytes[i]));
            //}

            current_path = new string(current_directory.File_Name);

            while (true)
            {
                Console.Write(current_path.Trim() + ':' + '>');
                c = Console.ReadLine().ToLower();
                string[] data = c.Split(' ');
                x = My_commands.Contains(data[0]);
                if (x)
                {
                    if (data[0] == "cls")
                    { Console.Clear(); }
                    else if (data[0] == "type")
                    {
                        if (data.Length > 1)
                        {
                            string name = data[1];
                            char[] n = name.ToCharArray();
                            int index = current_directory.searchDir(name);
                            if (index != -1)
                            {
                                int first_clustre = current_directory.dirTable[index].First_Clustre;
                                int file_size = current_directory.dirTable[index].File_Size;
                                string content = null;
                                FileEntry f = new FileEntry(n, 0x0, 0, 0, Program.current_directory, content);
                                f.readFile();
                                Console.WriteLine($" {f.File_Content}");

                            }
                            else
                            { Console.WriteLine("The system can not find the file spcified"); }

                        }
                    }
                    else if (data[0] == "import")
                    {
                        if (data.Length > 1)
                        {
                            if (File.Exists(data[1]))
                            {
                                string[] da = data[1].Split('\\');
                                int sizeOfd = da.Length;
                                string name = da[sizeOfd - 1];
                                string content = File.ReadAllText(data[1]);
                                int size = content.Length;
                                int firstClustre;
                                if (size > 0)
                                { firstClustre = FAT.GetAvailableBlock(); }
                                else
                                { firstClustre = 0; }

                                char[] n = name.ToCharArray();
                                int index = current_directory.searchDir(name);
                                if (index == -1)
                                {
                                    FileEntry f = new FileEntry(n, 0x0, firstClustre, size, current_directory, content);
                                    f.write_file();
                                    DirectoryEntry d = new DirectoryEntry(n, 0x0, firstClustre, size);
                                    current_directory.dirTable.Add(d);
                                    current_directory.writeDir();

                                }
                                else
                                { Console.WriteLine("The file is already exist"); }
                            }
                            else
                            { Console.WriteLine("The file is not exist to import"); }


                        }
                        else
                        { Console.WriteLine("Invalid Command!"); }
                    }

                    else if (data[0] == "dir")
                    {
                        Console.WriteLine("Directory of  " + current_path.Trim()+ ':' + '\\' + '\n');
                        int file_counter = 0;
                        int dir_counter = 0;
                        int total_filesize = 0;
                        for (int i = 0; i < current_directory.dirTable.Count; i++)
                        {
                            if (current_directory.dirTable[i].File_Att == 0x0)
                            {
                                Console.Write("\t" + current_directory.dirTable[i].File_Size);
                                Console.WriteLine(current_directory.dirTable[i].File_Name);
                                file_counter++;
                                total_filesize += current_directory.dirTable[i].File_Size;
                            }
                            else
                            {
                                string s = new string(current_directory.dirTable[i].File_Name);
                                Console.WriteLine("<DIR>" + "\t" + s);
                                dir_counter++;
                            }
                        }

                        Console.WriteLine();
                        Console.WriteLine(file_counter + "  File(s) " + '\t' + total_filesize + " bytes ");
                        Console.WriteLine(dir_counter + "  Dir(s) " + '\t' + FAT.getFreeSpace() + " bytes free");

                    }

                    else if (data[0] == "cd")
                    {
                        if (data.Length > 1)
                        {
                            string name = data[1];
                            char[] n = name.ToCharArray();
                            int index = current_directory.searchDir(name);
                            if (index != -1)
                            {
                                int first_clustre = current_directory.dirTable[index].First_Clustre;
                                Directory d = new Directory(n, 0x10, first_clustre, 0, current_directory);
                                current_directory = d;
                                current_path += "\\" + new string(current_directory.File_Name) + "\\";
                                d.readDir();
                            }
                            else
                            { Console.WriteLine("The directory is not exist"); }

                        }
                        else
                        { Console.WriteLine("Invalid Command!"); }
                    }

                    else if (data[0] == "rd")
                    {
                        if (data.Length > 1)
                        {
                            string name = data[1];
                            char[] n = name.ToCharArray();
                            int index = current_directory.searchDir(name);
                            if (index != -1)
                            {
                                int first_clustre = current_directory.dirTable[index].First_Clustre;
                                Directory d = new Directory(n, 0x10, first_clustre, 0, current_directory);
                                d.deleteDirectory();
                                //current_directory.writeDir();
                            }
                            else
                            { Console.WriteLine("The directory is not exist"); }
                        }
                        else
                        { Console.WriteLine("Invalid Command!"); }
                    }
                    else if (data[0] == "md")
                    {
                        if (data.Length > 1)
                        {
                            string name = data[1];
                            char[] n = name.ToCharArray();
                            if (current_directory.searchDir(name) == -1)
                            {
                                DirectoryEntry d = new DirectoryEntry(n, 0x10, 0, 0);
                                current_directory.dirTable.Add(d);
                                current_directory.writeDir();

                                if (current_directory.parent != null)
                                {
                                    current_directory.parent.update_content(current_directory.get_DirEntry());
                                    current_directory.parent.writeDir();
                                }
                            }
                            else
                            { Console.WriteLine("The directory is exist"); }

                        }
                        else
                        { Console.WriteLine("Invalid Command!"); }
                    }
                    else if (data[0] == "del")
                    {
                        if (data.Length > 1)
                        {
                            string name = data[1];
                            char[] n = name.ToCharArray();
                            int index = current_directory.searchDir(name);
                            if (index != -1)
                            {
                                if(Program.current_directory.dirTable[index].File_Att==0x0)
                                {
                                    int first_clustre = current_directory.dirTable[index].First_Clustre;
                                    int file_size = current_directory.dirTable[index].File_Size;
                                    FileEntry d = new FileEntry(n, 0x10, first_clustre, 0, current_directory,null);
                                    d.deleteFile();

                                }
                                else
                                { Console.WriteLine("The system cannot find the file specified"); }
                            }
                            else
                            { Console.WriteLine("The system cannot find the file specified"); }

                        }
                        else
                        { Console.WriteLine("Invalid Command!"); }
                    }

                    else if (data[0] == "help")
                    {
                        if (data.Length > 1)
                        {
                            if (data[1] == "cls")
                            { Console.WriteLine("CLS   \t Clears the screen."); }
                            else if (data[1] == "help")
                            { Console.WriteLine("HELP  \t Provides Help information for Windows commands."); }
                            else if (data[1] == "exite")
                            { Console.WriteLine("EXITE \t Exite the Program."); }
                            else if (data[1] == "dir")
                            { Console.WriteLine("DIR   \t List the contents of directory."); }
                            else if (data[1] == "copy")
                            { Console.WriteLine("COPY  \t Copies one or more files to another location."); }
                            else if (data[1] == "del")
                            { Console.WriteLine("DEL   \t Deletes one or more files."); }
                            else if (data[1] == "md")
                            { Console.WriteLine("MD    \t Creates a directory."); }
                            else if (data[1] == "rd")
                            { Console.WriteLine("RD    \t Removes a directory."); }
                            else if (data[1] == "rename")
                            { Console.WriteLine("RENAME\t Renames a file."); }
                            else if (data[1] == "type")
                            { Console.WriteLine("TYPE  \t Displays the contents of a text file."); }
                            else if (data[1] == "cd")
                            { Console.WriteLine("CD    \t Change the current default directory to."); }
                            else
                            { Console.WriteLine("Invalid command!."); }
                        }
                        else
                        {
                            Console.WriteLine("CLS   \t Clears the screen.");
                            Console.WriteLine("HELP  \t Provides Help information for Windows commands.");
                            Console.WriteLine("EXITE \t Exite the Program.");
                            Console.WriteLine("DIR   \t List the contents of directory.");
                            Console.WriteLine("COPY  \t Copies one or more files to another location.");
                            Console.WriteLine("DEL   \t Deletes one or more files.");
                            Console.WriteLine("MD    \t Creates a directory.");
                            Console.WriteLine("RD    \t Removes a directory.");
                            Console.WriteLine("RENAME\t Renames a file.");
                            Console.WriteLine("TYPE  \t Displays the contents of a text file.");
                            Console.WriteLine("CD    \t Change the current default directory to.");
                        }




                    }
                    else if (data[0] == "exite")
                    {
                        Environment.Exit(0);

                    }
                }
                else
                { Console.WriteLine("Invalid Command!"); }
            }



        }
    }
}