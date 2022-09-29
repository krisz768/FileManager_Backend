using FileManagerBackend.Model.FileManager;
using MySql.Data.MySqlClient;
using System.Data;
using System.Reflection;

namespace FileManagerBackend.Service.FileManager
{
    public class Fm_DbCommands
    {
        private readonly MySqlConnection MySqlConn;

        public Fm_DbCommands ()
        {
            MySqlConn = DbConnection.GetDatabaseConnection("FileManager");
            MySqlConn.Open ();
        }

        private async void OpenConnection()
        {
            if (!MySqlConn.State.Equals(ConnectionState.Open))
            {
                await MySqlConn.OpenAsync();
            }
        }

        public async Task<Fm_User> GetUserByUsername(string Username)
        {  
            try
            {
                OpenConnection();

                Fm_User User = null;

                string CommandString = "SELECT * FROM `Users` WHERE `Username` = BINARY ?;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("username", Username);

                using (var reader = await Command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        User = new Fm_User(Convert.ToInt32(reader["Id"]), reader["Username"].ToString(), reader["Password"].ToString(), reader["RootPath"].ToString());
                    }
                }

                return User;
            } catch
            {
                throw new HttpRequestException("<DatabaseError>");
            }
        }

        public async Task<long> CreateShare(string Link, int Owner, string RelPath, bool IsFile)
        {
            try
            {
                OpenConnection();

                string CommandString = "INSERT INTO `Shares`(`Link`, `Owner`, `RelPath`, `IsFile`) VALUES (@Link,@Owner,@RelPath,@IsFile);";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("@Link", Link);
                Command.Parameters.AddWithValue("@Owner", Owner);
                Command.Parameters.AddWithValue("@RelPath", RelPath);
                Command.Parameters.AddWithValue("@IsFile", IsFile);

                await Command.ExecuteNonQueryAsync();
                long Id = Command.LastInsertedId;
                return Id;
            }
            catch
            {
                throw new HttpRequestException("<DatabaseError>");
            }
        }

        public async Task<Fm_Share> GetShareByOwnerAndRelPath(int Owner, string RelPath)
        {
            try
            {
                OpenConnection();

                Fm_Share Share = null;

                string CommandString = "SELECT * FROM `Shares` WHERE `Owner` = ? AND `RelPath` = BINARY ?;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("owner", Owner);
                Command.Parameters.AddWithValue("relpath", RelPath);

                using (var reader = await Command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        Share = new Fm_Share(Convert.ToInt32(reader["Id"]), reader["Link"].ToString(), Convert.ToInt32(reader["Owner"]), reader["RelPath"].ToString(), Convert.ToBoolean(reader["IsFile"]), Convert.ToInt32(reader["UsageCount"]), Convert.ToBoolean(reader["FileExist"]));
                    }
                }

                return Share;
            }
            catch
            {
                throw new HttpRequestException("<DatabaseError>");
            }
        }
    }
}
