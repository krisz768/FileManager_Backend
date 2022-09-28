using FileManagerBackend.Service.FileManager;

namespace FileManagerBackend.Model.FileManager
{
    public class Fm_Share
    {
        int Id { get; set; }
        string Link { get; set; }
        int Owner { get; set; }
        string RelPath { get; set; }
        bool IsFile { get; set; }
        int UsageCount { get; set; }
        bool FileExist { get; set; }

        public Fm_Share (int Id, string Link, int Owner, string RelPath, bool IsFile, int UsageCount, bool FileExist)
        {
            this.Id = Id;
            this.Link = Link;
            this.Owner = Owner;
            this.RelPath = RelPath;
            this.IsFile = IsFile;
            this.UsageCount = UsageCount;
            this.FileExist = FileExist;

        }

        private async Task<Fm_User> CreateNewShare(int Owner, string RelPath, bool IsFile)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();
            int Id = await fm_DbCommands.CreateShare();
            return User;
        }

    }
}
