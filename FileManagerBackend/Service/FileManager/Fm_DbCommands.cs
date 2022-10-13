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

        public async Task<Fm_User> GetUserById(int Id)
        {
            try
            {
                OpenConnection();

                Fm_User User = null;

                string CommandString = "SELECT * FROM `Users` WHERE `Id` = ?;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("id", Id);

                using (var reader = await Command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        User = new Fm_User(Convert.ToInt32(reader["Id"]), reader["Username"].ToString(), reader["Password"].ToString(), reader["RootPath"].ToString());
                    }
                }

                return User;
            }
            catch
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

        public async Task<bool> ChangePasswordById(int UserId, string NewPassMD5)
        {
            try
            {
                OpenConnection();

                string CommandString = "UPDATE `Users` SET `Password`=? WHERE `Id` = ?;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("password", NewPassMD5);
                Command.Parameters.AddWithValue("id", UserId);

                int Result = await Command.ExecuteNonQueryAsync();

                return Result == 1 ? true : false;
            }
            catch
            {
                throw new HttpRequestException("<DatabaseError>");
            }
        }

        public async Task<Fm_Share[]> GetSharesByUserId(int UserId)
        {
            try
            {
                OpenConnection();

                List<Fm_Share> Shares = new List<Fm_Share>();

                string CommandString = "SELECT * FROM `Shares` WHERE `Owner` = ?";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("owner", UserId);

                using (var reader = await Command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        Shares.Add(new Fm_Share(Convert.ToInt32(reader["Id"]), reader["Link"].ToString(), Convert.ToInt32(reader["Owner"]), reader["RelPath"].ToString(), Convert.ToBoolean(reader["IsFile"]), Convert.ToInt32(reader["UsageCount"]), Convert.ToBoolean(reader["FileExist"])));
                    }
                }

                return Shares.ToArray();
            }
            catch
            {
                throw new HttpRequestException("<DatabaseError>");
            }
        }

        public async Task<Fm_Share> GetShareById(int Id)
        {
            try
            {
                OpenConnection();

                Fm_Share Share = null;

                string CommandString = "SELECT * FROM `Shares` WHERE `Id` = ?;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("id", Id);

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

        public async Task<bool> DeleteShareById(long Id)
        {
            try
            {
                OpenConnection();

                string CommandString = "DELETE FROM `Shares` WHERE `Id` = ?;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("id", Id);

                int Result = await Command.ExecuteNonQueryAsync();

                return Result == 1 ? true : false;
            }
            catch
            {
                throw new HttpRequestException("<DatabaseError>");
            }
        }

        public async Task<bool> DeleteAllInvalidShare(int UserId)
        {
            try
            {
                OpenConnection();

                string CommandString = "DELETE FROM `Shares` WHERE `Owner` = ? AND `FileExist` = 0;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("owner", UserId);

                int Result = await Command.ExecuteNonQueryAsync();

                return true;
            }
            catch
            {
                throw new HttpRequestException("<DatabaseError>");
            }
        }

        public async Task<Fm_Share> GetShareByLink(string Link)
        {
            try
            {
                OpenConnection();

                Fm_Share Share = null;

                string CommandString = "SELECT * FROM `Shares` WHERE `Link` = BINARY ?;";
                var Command = new MySqlCommand(CommandString, MySqlConn);

                Command.Parameters.AddWithValue("link", Link);

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
