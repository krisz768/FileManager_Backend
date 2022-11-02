using FileManagerBackend.Controllers;
using FileManagerBackend.Service.FileManager;
using System.Security.Cryptography;

namespace FileManagerBackend.Model.FileManager
{
    public class Fm_Share
    {
        public long Id { get; set; }
        public string Link { get; set; }
        public int Owner { get; set; }
        public string RelPath { get; set; }
        public bool IsFile { get; set; }
        public int UsageCount { get; set; }
        public bool FileExist { get; set; }

        public Fm_Share (long Id, string Link, int Owner, string RelPath, bool IsFile, int UsageCount, bool FileExist)
        {
            this.Id = Id;
            this.Link = Link;
            this.Owner = Owner;
            this.RelPath = RelPath;
            this.IsFile = IsFile;
            this.UsageCount = UsageCount;
            this.FileExist = FileExist;

        }

        public async Task<bool> Delete()
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();

            return await fm_DbCommands.DeleteShareById(this.Id);
        }

        public static async Task<Fm_Share> CreateNewShare(int Owner, string RelPath, bool IsFile)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();

            string Link = MD5Hash(Owner + RelPath + DateTime.Now.ToString());
            long Id = await fm_DbCommands.CreateShare(Link,Owner, RelPath, IsFile);

            return new Fm_Share(Id, Link,Owner, RelPath, IsFile,0,true);
        }

        public static async Task<Fm_Share> GetShareByRelPathAndOwner(int Owner, string RelPath)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();
            return await fm_DbCommands.GetShareByOwnerAndRelPath( Owner, RelPath);
        }

        public static async Task<Fm_Share[]> GetSharesByUserId(int UserId)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();
            return await fm_DbCommands.GetSharesByUserId(UserId);
        }

        public static async Task<Fm_Share> GetShareById(long Id)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();
            return await fm_DbCommands.GetShareById(Id);
        }

        public static async Task<bool> DeleteAllInvalid(int UserId)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();
            return await fm_DbCommands.DeleteAllInvalidShare(UserId);
        }

        public static async Task<Fm_Share> GetShareByLink(string Link)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();
            return await fm_DbCommands.GetShareByLink(Link);
        }

        private static string MD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes).ToLower();
            }
        }

    }
}
