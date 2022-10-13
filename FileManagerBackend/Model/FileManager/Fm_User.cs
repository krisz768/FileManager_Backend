using FileManagerBackend.Service.FileManager;

namespace FileManagerBackend.Model.FileManager
{
    public class Fm_User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string RootPath { get; set; }

        public Fm_User (int id, string userName, string password, string rootPath)
        {
            Id = id;
            Username = userName;
            Password = password;
            RootPath = rootPath;
        }

        public static async Task<Fm_User> GetByUsername(string Username)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();
            Fm_User User = await fm_DbCommands.GetUserByUsername(Username);
            if (User == null)
            {
                throw new HttpRequestException("<LoginError>");
            }
            return User;
        }

        public static async Task<Fm_User> GetById(int Id)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();
            Fm_User User = await fm_DbCommands.GetUserById(Id);
            return User;
        }

        public async Task<bool> SetPassword(string NewPassword)
        {
            Fm_DbCommands fm_DbCommands = new Fm_DbCommands();
            bool Result = await fm_DbCommands.ChangePasswordById(this.Id, NewPassword);
            if (Result)
                this.Password = NewPassword;
            return Result;
        }
    }
}
