using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sabio.Data;
using Sabio.Data.Providers;
using Sabio.Models;
using Sabio.Models.AppSettings;
using Sabio.Models.Domain;
using Sabio.Models.Domain.Files;
using Sabio.Models.Requests.Files;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using File = Sabio.Models.Domain.Files.File;

namespace Sabio.Services
{
    public class FilesService : IFilesService
    {
        IDataProvider _data = null;
        private AWSAppKeys _appKeys;

        public FilesService(IDataProvider data, IOptions<AWSAppKeys> appKeys)
        {
            _data = data;
            _appKeys = appKeys.Value;
        }
        public void Delete(int id)
        {
            string procName = "[dbo].[Files_Delete_ById]";
            _data.ExecuteNonQuery(procName,
            inputParamMapper: delegate (SqlParameterCollection col)
            {
                col.AddWithValue("@Id", id);
            },
            returnParameters: null);
        }
        public File Get(int id)
        {
            string procName = "[dbo].[Files_Select_ById]";

            File file = null;

            _data.ExecuteCmd(procName, delegate (SqlParameterCollection parameterCollection)
            {
                parameterCollection.AddWithValue("@Id", id);
            }
            , delegate (IDataReader reader, short set)
            {
                int startingIdex = 0;
                file = MapFile(reader, ref startingIdex);
            });
            return file;
        }
        public Paged<File> GetPageCreatedBy(int pageIndex, int pageSize, int createdBy)
        {
            Paged<File> pagedList = null;
            List<File> list = null;
            int totalCount = 0;

            string procName = "[dbo].[Files_Select_ByCreatedBy]";

            _data.ExecuteCmd(procName, delegate (SqlParameterCollection param)
            {
                param.AddWithValue("@pageIndex", pageIndex);
                param.AddWithValue("@pageSize", pageSize);
                param.AddWithValue("@CreatedBy", createdBy);
            }
           , delegate (IDataReader reader, short set)
           {
               int startingIdex = 0;
               File aFile = MapFile(reader, ref startingIdex);
               if (totalCount == 0)
               {
                   totalCount = reader.GetSafeInt32(startingIdex++);
               }
               if (list == null)
               {
                   list = new List<File>();
               }
               list.Add(aFile);
           }
           );
            if (list != null)
            {
                pagedList = new Paged<File>(list, pageIndex, pageSize, totalCount);
            }
            return pagedList;
        }
        public Paged<File> GetPage(int pageIndex, int pageSize)
        {
            Paged<File> pagedList = null;
            List<File> list = null;
            int totalCount = 0;

            string procName = "[dbo].[Files_SelectAll]";

            _data.ExecuteCmd(procName, delegate (SqlParameterCollection param)
            {
                param.AddWithValue("@pageIndex", pageIndex);
                param.AddWithValue("@pageSize", pageSize);
            }
           , delegate (IDataReader reader, short set)
           {
               int startingIdex = 0;
               File aFile = MapFile(reader, ref startingIdex);
               if (totalCount == 0)
               {
                   totalCount = reader.GetSafeInt32(startingIdex++);
               }
               if (list == null)
               {
                   list = new List<File>();
               }
               list.Add(aFile);
           }
           );
            if (list != null)
            {
                pagedList = new Paged<File>(list, pageIndex, pageSize, totalCount);
            }
            return pagedList;
        }
        public void Update(FileUpdateRequest model)
        {
            string procName = "[dbo].[Files_Update]";
            _data.ExecuteNonQuery(procName,
            inputParamMapper: delegate (SqlParameterCollection col)
            {
                col.AddWithValue("@Id", model.Id);
                col.AddWithValue("@Url", model.Url);
                col.AddWithValue("@FileTypeId", model.FileTypeId);
            },
            returnParameters: null);
        }
        public FileBase UploadAsync(IFormFile formFile, int userId)
        {
            FileBase response = null;
            string url = null;
            int fileTypeId = 0;

            RegionEndpoint bucketRegion = RegionEndpoint.USWest2;

            using (AmazonS3Client s3Client = new AmazonS3Client(_appKeys.AccessKey, _appKeys.Secret, bucketRegion))
            {

                var fileTransferUtility = new TransferUtility(s3Client);
                string fileKey = Guid.NewGuid().ToString();
                string keyName = "chamomile/" + fileKey + "/" + formFile.FileName;

                fileTypeId = GetFileTypeId(formFile);

                fileTransferUtility.UploadAsync(formFile.OpenReadStream(), _appKeys.BucketName, keyName).Wait();

                url = _appKeys.Domain + keyName;

                int fileId = Add(url, fileTypeId, userId);

                response = new FileBase();
                response.Id = fileId;
                response.Url = url;
            }
            return response;
        }
        private static int GetFileTypeId(IFormFile formFile)
        {
            int fileTypeId;
            switch (formFile.ContentType)
            {
                case "image/png":
                    fileTypeId = 1;
                    break;
                case "image/jpg":
                    fileTypeId = 2;
                    break;
                case "image/jpeg":
                    fileTypeId = 3;
                    break;
                case "application/pdf":
                    fileTypeId = 4;
                    break;
                case "text/plain":
                    fileTypeId = 5;
                    break;
                case "image/webp":
                    fileTypeId = 6;
                    break;
                case "image/bmp":
                    fileTypeId = 7;
                    break;
                case "image/gif":
                    fileTypeId = 8;
                    break;
                default:
                    throw new Exception("File Type not supported");
            }
            return fileTypeId;
        }
        public int Add(string url, int fileTypeId, int userId)
        {
            int id = 0;

            string procName = "[dbo].[Files_Insert]";

            _data.ExecuteNonQuery(procName,
            inputParamMapper: delegate (SqlParameterCollection col)
            {
                SqlParameter idOut = new SqlParameter("@Id", SqlDbType.Int);
                idOut.Direction = ParameterDirection.Output;

                col.Add(idOut);
                col.AddWithValue("@Url", url);
                col.AddWithValue("@FileTypeId", fileTypeId);
                col.AddWithValue("@CreatedBy", userId);
            },
            returnParameters: delegate (SqlParameterCollection returnCollection)
            {
                object oId = returnCollection["@Id"].Value;
                int.TryParse(oId.ToString(), out id);
            });
            return id;
        }
        private static File MapFile(IDataReader reader, ref int startingIdex)
        {
            File aFile = new File();

            aFile.Id = reader.GetSafeInt32(startingIdex++);
            aFile.Url = reader.GetSafeString(startingIdex++);
            aFile.FileTypeId = reader.GetSafeInt32(startingIdex++);
            aFile.FileTypeName = reader.GetSafeString(startingIdex++);
            aFile.CreatedBy = reader.GetSafeInt32(startingIdex++);
            aFile.DateCreated = reader.GetSafeDateTime(startingIdex++);

            return aFile;
        }
    }
}
