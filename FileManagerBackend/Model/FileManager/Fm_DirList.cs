namespace FileManagerBackend.Model.FileManager
{
    public class Fm_DirList
    {
        public bool IsFile { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public DateTime LastMod { get; set; }

        public Fm_DirList(bool isFile, string name, string size, DateTime lastMod)
        {
            IsFile = isFile;
            Name = name;
            Size = size;
            LastMod = lastMod;
        }

        public static Fm_DirList[] GetListFromFolder(string BaseDir)
        {
            List<Fm_DirList> list = new List<Fm_DirList>();

            DirectoryInfo dirInfo = new DirectoryInfo(BaseDir);
            DirectoryInfo[] dsInfo = dirInfo.GetDirectories();
            dsInfo = dsInfo.OrderBy(item => item.Name).ToArray();
            foreach (DirectoryInfo dInfo in dsInfo)
            {
                list.Add(new Fm_DirList(false, dInfo.Name, "", dInfo.LastWriteTime));
            }
            FileInfo[] fsInfo = dirInfo.GetFiles();
            fsInfo = fsInfo.OrderBy(item => item.Name).ToArray();
            foreach (FileInfo fInfo in fsInfo)
            {
                list.Add(new Fm_DirList(true, fInfo.Name, ByteLenghtToHumanRString(fInfo.Length), fInfo.LastWriteTime));
            }
            
            return list.ToArray();
        }

        private static string ByteLenghtToHumanRString(long FileSize)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = FileSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }
    }
}
