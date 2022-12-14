using FileManagerBackend.Model;
using FileManagerBackend.Model.FileManager;
using FileManagerBackend.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileManagerBackend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FileManagerController : ControllerBase
    {
        private readonly ILogger<FileManagerController> _logger;
        private readonly IConfiguration _Configuration;

        //<LoginError>, <DatabaseError>, <LoggedOut>, <NotLoggedIn>, <ListError>, <DeleteError>,  <CopyError>, <CopySuccessful>, <DeleteSuccessful> || <FolderCreateSucessful>, <FolderCreateError>, FolderDeleteError,FolderDeleteSucessful || UploadSucessful, UploadError || "<DownloadError>", FileError || RenameSucessfull, "<RenameError>", "<ShareError>", || "FileSizeError", "ChangePasswordFail", "ChangePasswordSuccess", "ChangePasswordNotMatch", "ShareLoadError", ChangePasswordFail8Char

        public FileManagerController(ILogger<FileManagerController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _Configuration = configuration;
            DbConnection.Configuration = _Configuration;
        }

        [Route("Login")]
        [HttpPost]
        public async Task<ResponseModel> LoginPost(string Username, string Password)
        {
            try
            {
                Fm_User User = await Fm_User.GetByUsername(Username);
                await Task.Delay(1500);
                if (User.Password != MD5Hash(Password + "#" + User.Id))
                {
                    throw new HttpRequestException("<LoginError>");
                }

                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("UserObject", ToJSONString(User));

                return new ResponseModel(false, User.Username);
            } catch (Exception e)
            {
                return new ResponseModel(true, e.Message);
            }
        }

        [Route("IsLoggedIn")]
        [HttpPost]
        public ResponseModel IsLoggedInPost()
        {
            var IsLoggedIn = HttpContext.Session.GetString("IsLoggedIn");

            if (IsLoggedIn != null)
            {
                return new ResponseModel(false, bool.Parse(IsLoggedIn));
            }

            return new ResponseModel(false, false);
        }

        [Route("GetUsername")]
        [HttpPost]
        public ResponseModel GetUsernamePost()
        {
            if (IsLoggedIn())
            {
                return new ResponseModel(false, ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
            } else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("LogOut")]
        [HttpPost]
        public ResponseModel LogoutPost()
        {
            HttpContext.Session.SetString("IsLoggedIn", "false");

            return new ResponseModel(false, "<LoggedOut>");
        }

        [Route("ListFolder")]
        [HttpPost]
        public async Task<ResponseModel> ListFolderPost(string subFolder)
        {
            if (IsLoggedIn())
            {
                if (!subFolder.Contains(".."))
                {
                    try
                    {
                        subFolder = subFolder.TrimStart('/');
                        Fm_DirList[] DirList = Fm_DirList.GetListFromFolder(Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath, subFolder));
                        return new ResponseModel(false, DirList);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Cannot list: " + Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath, subFolder).ToString());
                        _logger.LogError("Deatils: " + e.ToString());
                        return new ResponseModel(true, "<ListError>");
                    }
                } else
                {
                    _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                    return new ResponseModel(true, "<ListError>");
                }
            } else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("DeleteFile")]
        [HttpPost]
        public async Task<ResponseModel> DeleteFilePost(string FilePath)
        {
            if (IsLoggedIn())
            {
                if (!FilePath.Contains(".."))
                {
                    try
                    {
                        FilePath = FilePath.TrimStart('/');
                        FilePath = Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath, FilePath);

                        FileInfo file = new FileInfo(FilePath);

                        if (file.Exists)
                        {
                            file.Delete();
                            return new ResponseModel(false, "<DeleteSuccessful>");
                        } else
                        {
                            return new ResponseModel(true, "<DeleteError>");
                        }

                        
                    }
                    catch (Exception e)
                    {
                        return new ResponseModel(true, "<DeleteError>");
                    }
                }
                else
                {
                    _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                    return new ResponseModel(true, "<DeleteError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("CopyFile")]
        [HttpPost]
        public async Task<ResponseModel> CopyFilePost(string FilePath, string DestinationPath)
        {
            if (IsLoggedIn())
            {
                if (!FilePath.Contains("..") || !DestinationPath.Contains(".."))
                {
                    try
                    {
                        FilePath = FilePath.TrimStart('/');
                        DestinationPath = DestinationPath.TrimStart('/');

                        FilePath = Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath, FilePath);
                        DestinationPath = Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath, DestinationPath);

                        FileInfo file = new FileInfo(FilePath);
                        FileInfo Newfile = new FileInfo(DestinationPath);

                        if (file.Exists)
                        {
                            using (Stream source = file.Open(FileMode.Open))
                            {
                                using (Stream destination = Newfile.Create())
                                {
                                    await source.CopyToAsync(destination);
                                }
                            }
                            return new ResponseModel(false, "<CopySuccessful>");
                        }
                        else
                        {
                            return new ResponseModel(true, "<CopyError>");
                        }


                    }
                    catch (Exception e)
                    {
                        return new ResponseModel(true, "<CopyError>");
                    }
                }
                else
                {
                    _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                    return new ResponseModel(true, "<CopyError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("MoveFile")]
        [HttpPost]
        public async Task<ResponseModel> MoveFilePost(string FilePath, string DestinationPath)
        {
            if (IsLoggedIn())
            {
                if (!FilePath.Contains("..") || !DestinationPath.Contains(".."))
                {
                    try
                    {
                        FilePath = FilePath.TrimStart('/');
                        DestinationPath = DestinationPath.TrimStart('/');

                        FilePath = Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath, FilePath);
                        DestinationPath = Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath, DestinationPath);

                        FileInfo file = new FileInfo(FilePath);

                        if (file.Exists)
                        {
                            file.MoveTo(DestinationPath);
                            return new ResponseModel(false, "<CopySuccessful>");
                        }
                        else
                        {
                            return new ResponseModel(true, "<CopyError>");
                        }


                    }
                    catch (Exception e)
                    {
                        return new ResponseModel(true, "<CopyError>");
                    }
                }
                else
                {
                    _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                    return new ResponseModel(true, "<CopyError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("CreateFolder")]
        [HttpPost]
        public async Task<ResponseModel> CreateFolderPost(string FolderPath)
        {
            if (IsLoggedIn())
            {
                FolderPath = FolderPath.TrimStart('/');
                if (!FolderPath.Contains("..") || FolderPath.Contains("/") || FolderPath.Contains(@"\"))
                {
                    try
                    {
                        FolderPath = Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath, FolderPath);

                        DirectoryInfo folder = new DirectoryInfo(FolderPath);

                        folder.Create();
                        return new ResponseModel(false, "<FolderCreateSucessful>");

                    }
                    catch (Exception e)
                    {
                        return new ResponseModel(true, "<FolderCreateError>");
                    }
                }
                else
                {
                    _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                    return new ResponseModel(true, "<FolderCreateError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("DeleteFolder")]
        [HttpPost]
        public async Task<ResponseModel> DeleteFolderPost(string FolderPath)
        {
            if (IsLoggedIn())
            {
                if (!FolderPath.Contains(".."))
                {
                    try
                    {
                        FolderPath = FolderPath.TrimStart('/');

                        FolderPath = Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath, FolderPath);

                        DirectoryInfo folder = new DirectoryInfo(FolderPath);

                        folder.Delete();
                        return new ResponseModel(false, "<FolderDeleteSucessful>");

                    }
                    catch (Exception e)
                    {
                        return new ResponseModel(true, "<FolderDeleteError>");
                    }
                }
                else
                {
                    _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                    return new ResponseModel(true, "<FolderDeleteError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("UploadFile")]
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<ResponseModel> UploadFilePost([FromForm] string FolderPath, [FromForm] List<IFormFile> Files)
        {
            if (IsLoggedIn())
            {
                bool FileNameCorrect = true;

                Files.ForEach((file) => { if (file.FileName.Contains("..") || file.FileName.Contains("/") || file.FileName.Contains(@"\")) { FileNameCorrect = false; }});

                if (!FolderPath.Contains("..") || !FileNameCorrect)
                {
                    try
                    {
                        var filePath = Path.Combine(ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath ,FolderPath.TrimStart('/'));
                        if (Files.Count != 0)
                        {
                            foreach (IFormFile file in Files)
                            {
                                if (file.Length > 0)
                                {
                                    using (var stream = new FileStream(Path.Combine(filePath, file.FileName), FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }
                                }
                            }
                            return new ResponseModel(false, "<UploadSucessful>");
                        }
                        else {
                            return new ResponseModel(true, "<UploadError>");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Upload error: " + e.ToString());
                        return new ResponseModel(true, "<UploadError>");
                    }
                }
                else
                {
                    _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                    return new ResponseModel(true, "<UploadError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("DownloadZip")]
        [HttpGet]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DownloadZipGet(string BasePath, [FromQuery] string[] FilePaths)
        {

            if (IsLoggedIn())
            {
                string tempPath = Path.GetTempFileName();
                try
                {
                 
                    string UserPath = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath;
                    var FullFilePaths = new List<string>();

                    foreach (string FilePath in FilePaths)
                    {
                        if (!FilePath.Contains(".."))
                        {
                            string FilePathFix = FilePath.TrimStart('/');
                            FullFilePaths.Add(FilePathFix);
                        }
                        else
                        {
                            _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                            return BadRequest();
                        }
                    }

                    

                    using (var zipFileStream = new FileStream(tempPath, FileMode.Create))
                    {
                        using (ZipArchive archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
                        {
                            foreach (var FullFilePath in FullFilePaths)
                            {
                                string RelPath = FullFilePath.TrimStart('/');
                                
                                if (BasePath.TrimStart('/') != "")
                                {
                                    RelPath = FullFilePath.Replace(BasePath.TrimStart('/'), "").TrimStart('/');
                                }
                                

                                var entry = archive.CreateEntry(RelPath);
                                using (var entryStream = entry.Open())
                                using (var fileStream = System.IO.File.OpenRead(Path.Combine(UserPath, FullFilePath)))
                                {
                                    await fileStream.CopyToAsync(entryStream);
                                }
                            }
                        }
                    }

                    var zipFileStreamR = new FileStream(tempPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                    return File(zipFileStreamR, "application/zip", DateTime.Now.ToString(new CultureInfo("hu-HU")) + ".zip");

                } catch (Exception e)
                {
                    _logger.LogError("Download error: " + e.ToString());
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                    
                    return Problem("<DownloadError>");
                }
            }
            else
            {
                return Problem("<NotLoggedIn>");
            }
        }

        [Route("GetFileType")]
        [HttpPost]
        public async Task<ResponseModel> GetFileType(string FilePath)
        {

            if (IsLoggedIn())
            {
                try
                {

                    string UserPath = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath;

                    if (!FilePath.Contains(".."))
                    {
                        FilePath = FilePath.TrimStart('/');
                        FilePath = FilePath.TrimEnd('/');
                    }
                    else
                    {
                        _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                        return new ResponseModel(true, "<FileError>");
                    }

                    var ContentTypeProvider = new FileExtensionContentTypeProvider();
                    string contentType;
                    if (!ContentTypeProvider.TryGetContentType(Path.Combine(UserPath, FilePath), out contentType))
                    {
                        contentType = "application/octet-stream";
                    }

                    return new ResponseModel(false, contentType);

                }
                catch (Exception e)
                {
                    _logger.LogError("Download error: " + e.ToString());

                    return new ResponseModel(true, "<FileError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("DownloadFile")]
        [HttpGet]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DownloadFileGet(string FilePath)
        {

            if (IsLoggedIn())
            {
                try
                {

                    string UserPath = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath;

                    if (!FilePath.Contains(".."))
                    {
                        FilePath = FilePath.TrimStart('/');
                        FilePath = FilePath.TrimEnd('/');
                    }
                    else
                    {
                        _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                        return BadRequest();
                    }

                    var ContentTypeProvider = new FileExtensionContentTypeProvider();
                    string contentType;
                    if (!ContentTypeProvider.TryGetContentType(Path.Combine(UserPath, FilePath), out contentType))
                    {
                        contentType = "application/octet-stream";
                    }

                    var DownloadFile = new FileStream(Path.Combine(UserPath, FilePath), FileMode.Open, FileAccess.Read);
                    var RetObject = File(DownloadFile, contentType, Path.GetFileName(Path.Combine(UserPath, FilePath)));
                    RetObject.EnableRangeProcessing = true;
                    return RetObject;

                }
                catch (Exception e)
                {
                    _logger.LogError("Download error: " + e.ToString());

                    return Problem("<DownloadError>");
                }
            }
            else
            {
                return Problem("<NotLoggedIn>");
            }
        }

        [Route("RenameFileOrFolder")]
        [HttpPost]
        public async Task<ResponseModel> RenameFileOrFolder(string Path, string OldName, string NewName)
        {

            if (IsLoggedIn())
            {
                try
                {

                    string UserPath = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath;

                    if (!Path.Contains("..") && !OldName.Contains("..") && !OldName.Contains("/") && !NewName.Contains("..") && !NewName.Contains("/"))
                    {
                        Path = Path.TrimStart('/');
                        Path = Path.TrimEnd('/');
                    }
                    else
                    {
                        _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                        return new ResponseModel(true, "<RenameError>");
                    }

                    string FullPath = System.IO.Path.Combine(UserPath, Path);

                    if (System.IO.File.Exists(FullPath))
                    {
                        System.IO.File.Move(FullPath + "/" + OldName, FullPath + "/" + NewName);
                    } else if (System.IO.Directory.Exists(FullPath))
                    {
                        Directory.Move(FullPath + "/" + OldName, FullPath + "/" + NewName);
                    } else
                    {
                        return new ResponseModel(true, "<RenameError>");
                    }

                    return new ResponseModel(false, "<RenameSucessfull>");

                }
                catch (Exception e)
                {
                    _logger.LogError("Download error: " + e.ToString());

                    return new ResponseModel(true, "<FileError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("CreateShare")]
        [HttpPost]
        public async Task<ResponseModel> CreateSharePost(string SharePath)
        {
            if (IsLoggedIn())
            {
                if (!SharePath.Contains(".."))
                {
                    try
                    {
                        SharePath = SharePath.TrimStart('/');
                        Fm_User User = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject"));


                        Fm_Share ExistingLink = await Fm_Share.GetShareByRelPathAndOwner(User.Id, SharePath);

                        if (ExistingLink != null)
                        {
                            return new ResponseModel(false, ExistingLink.Link);
                        } else
                        {
                            string SharePathFullPath = Path.Combine(User.RootPath, SharePath);

                            bool IsFile = false;

                            if (System.IO.File.Exists(SharePathFullPath))
                            {
                                IsFile = true;
                            }
                            else if (System.IO.Directory.Exists(SharePathFullPath))
                            {
                                IsFile = false;
                            }
                            else
                            {
                                return new ResponseModel(true, "<ShareError>");
                            }

                            int OwnerId = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Id;

                            Fm_Share NewShare = await Fm_Share.CreateNewShare(OwnerId, SharePath, IsFile);

                            return new ResponseModel(false, NewShare.Link);
                        }
                    }
                    catch (Exception e)
                    {
                        return new ResponseModel(true, "<ShareError>");
                    }
                }
                else
                {
                    _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                    return new ResponseModel(true, "<ShareError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("GetTextFile")]
        [HttpPost]
        public async Task<ResponseModel> GetTextFile(string FilePath)
        {

            if (IsLoggedIn())
            {
                try
                {

                    string UserPath = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).RootPath;

                    if (!FilePath.Contains(".."))
                    {
                        FilePath = FilePath.TrimStart('/');
                        FilePath = FilePath.TrimEnd('/');
                    }
                    else
                    {
                        _logger.LogWarning("Prevented access to subfolder, attempted by user: " + ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject")).Username);
                        return new ResponseModel(true, "<FileError>");
                    }

                    string FullPath = System.IO.Path.Combine(UserPath, FilePath);
                    FileInfo File = new FileInfo(FullPath);

                    if (File.Exists)
                    {
                        if (File.Length <= 1000000)
                        {
                            string FileContent = await File.OpenText().ReadToEndAsync();
                            return new ResponseModel(false, FileContent);
                        } else
                        {
                            return new ResponseModel(true, "<FileSizeError>");
                        }
                    } else
                    {
                        return new ResponseModel(true, "<FileError>");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Download error: " + e.ToString());

                    return new ResponseModel(true, "<FileError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("ChangePassword")]
        [HttpPost]
        public async Task<ResponseModel> ChangePassword(string OldPassword, string NewPassword)
        {

            if (IsLoggedIn())
            {
                try
                {
                    Fm_User UserO = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject"));
                    if (UserO.Password == MD5Hash(OldPassword + "#" + UserO.Id))
                    {
                        if (NewPassword.Length < 8)
                        {
                            return new ResponseModel(true, "<ChangePasswordFail8Char>");
                        }

                        bool result = await UserO.SetPassword(MD5Hash(NewPassword + "#" + UserO.Id));
                        if (result)
                        {
                            HttpContext.Session.SetString("UserObject", ToJSONString(UserO));
                            return new ResponseModel(false, "<ChangePasswordSuccess>");
                        } else
                        {
                            return new ResponseModel(true, "<ChangePasswordFail>");
                        }
                    } else
                    {
                        return new ResponseModel(true, "<ChangePasswordNotMatch>");
                    }
                    
                } catch
                {
                    return new ResponseModel(true, "<ChangePasswordFail>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("ListAllShares")]
        [HttpPost]
        public async Task<ResponseModel> ListAllShares()
        {

            if (IsLoggedIn())
            {
                try
                {
                    Fm_User UserO = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject"));

                    Fm_Share[] Shares = await Fm_Share.GetSharesByUserId(UserO.Id);

                    return new ResponseModel(false, Shares);
                }
                catch
                {
                    return new ResponseModel(true, "<ListError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("DeleteShareById")]
        [HttpPost]
        public async Task<ResponseModel> DeleteShareById(long Id)
        {
            if (IsLoggedIn())
            {
                try
                {
                    Fm_User UserO = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject"));

                    Fm_Share Share = await Fm_Share.GetShareById(Id);

                    if (Share.Owner == UserO.Id)
                    {
                        bool Result = await Share.Delete();
                        if (Result)
                        {
                            return new ResponseModel(false, "<DeleteSuccessful>");
                        }
                        else
                        {
                            return new ResponseModel(true, "<DeleteError>");
                        }
                    }
                    else
                    {
                        return new ResponseModel(true, "<DeleteError>");
                    }
                }
                catch (Exception e)
                {
                    return new ResponseModel(true, "<DeleteError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("DeleteAllInvalidShare")]
        [HttpPost]
        public async Task<ResponseModel> DeleteAllInvalidShare()
        {

            if (IsLoggedIn())
            {
                try
                {
                    Fm_User UserO = ToJSONObject<Fm_User>(HttpContext.Session.GetString("UserObject"));

                    Fm_Share.DeleteAllInvalid(UserO.Id);

                    return new ResponseModel(false, "<DeleteSuccessful>");
                }
                catch
                {
                    return new ResponseModel(true, "<DeleteError>");
                }
            }
            else
            {
                return new ResponseModel(true, "<NotLoggedIn>");
            }
        }

        [Route("GetShareFileType")]
        [HttpPost]
        public async Task<ResponseModel> GetShareFileType(string ShareLink)
        {
            try
            {
                Fm_Share share = await Fm_Share.GetShareByLink(ShareLink);

                if (share != null)
                {
                    string contentType;

                    if (share.IsFile)
                    {
                        string UserPath = (await Fm_User.GetById(share.Owner)).RootPath;

                        var ContentTypeProvider = new FileExtensionContentTypeProvider();
                        
                        if (!ContentTypeProvider.TryGetContentType(Path.Combine(UserPath, share.RelPath), out contentType))
                        {
                            contentType = "application/octet-stream";
                        }
                    } else
                    {
                        contentType = "FOLDER";
                    }

                    return new ResponseModel(false, contentType);

                } else
                {
                    return new ResponseModel(true, "<ShareLoadError>");
                } 

            }
            catch (Exception e)
            {
                _logger.LogError("Download error: " + e.ToString());

                return new ResponseModel(true, "<ShareLoadError>");
            }
        }

        [Route("DownloadSahreFile")]
        [HttpGet]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> DownloadShareFileGet(string ShareLink)
        {
            try
            {
                var share = await Fm_Share.GetShareByLink(ShareLink);

                if (share != null)
                {
                    string UserPath = (await Fm_User.GetById(share.Owner)).RootPath;


                    var ContentTypeProvider = new FileExtensionContentTypeProvider();
                    string contentType;
                    if (!ContentTypeProvider.TryGetContentType(Path.Combine(UserPath, share.RelPath), out contentType))
                    {
                        contentType = "application/octet-stream";
                    }

                    var DownloadFile = new FileStream(Path.Combine(UserPath, share.RelPath), FileMode.Open, FileAccess.Read);
                    var RetObject = File(DownloadFile, contentType, Path.GetFileName(Path.Combine(UserPath, share.RelPath)));
                    RetObject.EnableRangeProcessing = true;
                    return RetObject;
                } else
                {
                    return Problem("<ShareLoadError>");
                }

            }
            catch (Exception e)
            {
                _logger.LogError("Download error: " + e.ToString());

                return Problem("<ShareLoadError>");
            }
        }

        [Route("GetShareTextFile")]
        [HttpPost]
        public async Task<ResponseModel> GetShareTextFile(string ShareLink)
        {

            try
            {

                var share = await Fm_Share.GetShareByLink(ShareLink);

                if (share != null)
                {
                    string UserPath = (await Fm_User.GetById(share.Owner)).RootPath;

                    string FullPath = System.IO.Path.Combine(UserPath, share.RelPath);
                    FileInfo File = new FileInfo(FullPath);

                    if (File.Exists)
                    {
                        if (File.Length <= 1000000)
                        {
                            string FileContent = await File.OpenText().ReadToEndAsync();
                            return new ResponseModel(false, FileContent);
                        }
                        else
                        {
                            return new ResponseModel(true, "<FileSizeError>");
                        }
                    }
                    else
                    {
                        return new ResponseModel(true, "<FileError>");
                    }
                } else
                {
                    return new ResponseModel(true, "<ShareLoadError>");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Download error: " + e.ToString());

                return new ResponseModel(true, "<ShareLoadError>");
            }
        }












        private bool IsLoggedIn()
        {
            var IsLoggedIn = HttpContext.Session.GetString("IsLoggedIn");

            if (IsLoggedIn != null)
            {
                return bool.Parse(IsLoggedIn);
            }

            return false;
        }

        private static T ToJSONObject<T>(string Object)
        {
            return JsonSerializer.Deserialize<T>(Object);
        }

        private static string ToJSONString(object Object)
        {
            return JsonSerializer.Serialize(Object);
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
