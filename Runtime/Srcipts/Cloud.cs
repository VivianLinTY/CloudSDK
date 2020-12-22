using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cloud.Client
{
    public class Cloud
    {
        private readonly static string TAG = "[Cloud]";

        private readonly static int DELAY_RETRY = 500;

        private readonly static string TYPE_DOWNLOAD = "download";
        private readonly static string TYPE_UPLOAD = "upload";

        private readonly static string ERROR_GENERAL_NULL = "Ooh...Something is null";

        private static string _environment;

        public static Cloud Instance { get; private set; }

        public static void CreateInstance(string environment, bool isDebugMode)
        {
            _environment = environment;
            Log.CreateInstance(isDebugMode);
            if (Instance == null)
            {
                Instance = new Cloud();
            }
        }

        private string GetHttpUrl(string httpType,
            string folder, int category, string fileName)
        {
            string domain = CloudUtils.GetCloudDomain(_environment);
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentException("Folder is empty.");
            }
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("FileName is empty.");
            }
            Log.Instance.D(TAG, "GetUrl accountEnv=" + _environment +
                ", category=" + category + ", fileName=" + fileName);
            UriBuilder builder = new UriBuilder(domain)
            {
                Path = "api/v1/urls/" + httpType,
                Query = "?category=" + category +
                "&folder=" + folder + "&filename=" + fileName
            };
            return builder.Uri.AbsoluteUri;
        }

        private async Task GetHttpResponse(string fileName, string url,
            string token, IProgress<ResponseInfo> callback, int retryCount)
        {
            try
            {
                var getRequest = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get
                };
                getRequest.Headers.Add("Authorization", "Bearer " + token);
                var getResponse = await
                    HttpClientService.ZHttpClient.SendAsync(getRequest);
                Log.Instance.D(TAG, "GetHttpResponse getResponse=" + getResponse);
                if (getResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ResponseInfo args = new ResponseInfo(fileName,
                        await getResponse.Content.ReadAsStringAsync());
                    if (null != callback)
                    {
                        callback.Report(args);
                    }
                }

            }
            catch (Exception e)
            {
                if (retryCount < 5)
                {
                    if(retryCount < 1)
                    {
                        Log.Instance.W(TAG, "GetHttpResponse msg=" + e.Message +
                        ", stackTrace:" + e.StackTrace + ", retryCount=" +
                        retryCount + ", fileName = " + fileName);
                    }
                    else
                    {
                        Log.Instance.D(TAG, "GetHttpResponse msg=" + e.Message +
                        ", stackTrace:" + e.StackTrace + ", retryCount=" +
                        retryCount + ", fileName = " + fileName);
                    }
                    await Task.Delay(DELAY_RETRY);
                    await GetHttpResponse(
                        fileName, url, token, callback, retryCount + 1);
                }
                else
                {
                    Log.Instance.E(TAG, "GetHttpResponse error fileName=" + fileName);
                    ResponseInfo args = new ResponseInfo(fileName, "");
                    if (null != callback)
                    {
                        callback.Report(args);
                    }
                }
            }
        }

        private async Task PutHttpResponse(string fileName, string url,
        Stream stream, string token,
        IProgress<ProgressInfo> callback, int retryCount, string error)
        {
            Log.Instance.D(TAG, "PutHttpResponse url=" + url);
            ProgressInfo args = null;
            try
            {
                MultipartFormDataContent content = new MultipartFormDataContent();
                StreamContent streamConent = new StreamContent(stream);
                content.Add(new StreamContent(stream));
                ZHttpContent progressContent = null;
                if (null != callback)
                {
                    progressContent = new ZHttpContent(streamConent, (sent, total) =>
                    {
                        args = new ProgressInfo(
                            fileName, (int)sent, (int)total);
                        callback.Report(args);
                    });
                }
                var putResponse = await HttpClientService.ZHttpClient
                    .PutAsync(url, progressContent ?? streamConent);
                Log.Instance.D(TAG, "PutHttpResponse putResponse=" + putResponse);
                if (null != callback && null != args)
                {
                    if (putResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        if (args.IsFinished)
                        {
                            args.Success = true;

                        }
                        else
                        {
                            string message = "Uploaded " + fileName + " incompletely.";
                            Log.Instance.W(TAG, message);
                            args.Error = message;
                        }
                    }
                    else
                    {
                        args.Error = await putResponse.Content.ReadAsStringAsync();
                    }
                    callback.Report(args);
                }
            }
            catch (Exception e)
            {
                if (retryCount < 5)
                {
                    if (retryCount < 1)
                    {
                        Log.Instance.W(TAG, "PutHttpResponse msg=" + e.Message +
                        ", stackTrace:" + e.StackTrace + ", retryCount=" +
                        retryCount + ", fileName = " + fileName);
                    }
                    else
                    {
                        Log.Instance.D(TAG, "PutHttpResponse msg=" + e.Message +
                        ", stackTrace:" + e.StackTrace + ", retryCount=" +
                        retryCount + ", fileName = " + fileName);
                    }
                    await Task.Delay(DELAY_RETRY);
                    await PutHttpResponse(fileName, url, stream, token, callback,
                        retryCount + 1, retryCount < 1 ? e.Message : error);
                }
                else
                {
                    Log.Instance.E(TAG, "PutHttpResponse error fileName=" + fileName);
                    if (null != callback)
                    {
                        if (null == args)
                        {
                            args = new ProgressInfo(fileName, 0, 1);
                        }
                        args.Error = error;
                        callback.Report(args);
                    }
                }
            }
        }

        //Download
        public void GetTempDownloadUrl(
            string fileName, string token, IProgress<ResponseInfo> callback)
        {
            GetDownloadUrl(CloudUtils.FOLDER_CACHE,
                fileName, CloudUtils.CATEGORY_CACHE, token, callback);
        }

        public void GetDownloadUrl(string folder, string fileName,
            int category, string token, IProgress<ResponseInfo> callback)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token is empty.");
            }
            Task.Run(async () => await GetHttpResponse(fileName, GetHttpUrl(
                TYPE_DOWNLOAD, folder, category, fileName), token, callback, 0));
        }

        //Upload
        public void UploadTempFile(string fileName,
            string filePath, string token, IProgress<ProgressInfo> callback)
        {
            UploadFile(CloudUtils.FOLDER_CACHE,
               fileName, CloudUtils.CATEGORY_CACHE, filePath, token, callback);
        }

        public void UploadTempFile(string fileName,
            Stream stream, string token, IProgress<ProgressInfo> callback)
        {
            UploadFile(CloudUtils.FOLDER_CACHE,
               fileName, CloudUtils.CATEGORY_CACHE, stream, token, callback);
        }

        public void UploadTempFile(string fileName,
            byte[] bytes, string token, IProgress<ProgressInfo> callback)
        {
            UploadFile(CloudUtils.FOLDER_CACHE, fileName,
                CloudUtils.CATEGORY_CACHE, bytes, token, callback);
        }

        public void UploadFile(string folder,
            string fileName, int category, string filePath, string token,
            IProgress<ProgressInfo> callback)
        {
            Stream stream;
            try
            {
                stream = File.OpenRead(filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
            UploadFile(
                folder, fileName, category, stream, token, callback);
        }

        public void UploadFile(string folder,
            string fileName, int category, Stream stream, string token,
            IProgress<ProgressInfo> callback)
        {
            byte[] bytes;
            try
            {
                bytes = CloudUtils.StreamToBytes(stream);
            }
            catch (Exception e)
            {
                throw e;
            }
            UploadFile(folder, fileName, category, bytes, token, callback);
        }

        public void UploadFile(string folder,
            string fileName, int category, byte[] bytes, string token,
            IProgress<ProgressInfo> callback)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token is empty.");
            }

            if (null == bytes || bytes.Length < 1)
            {
                throw new FormatException("Size of " + fileName + " is zero.");
            }

            if (bytes.Length > CloudUtils.SIZE_LIMIT && category != CloudUtils.CATEGORY_MOBILE_RESOURCES)
            {
                throw new FormatException("Size of " + fileName +
                    " is over 30MB. Size:" + bytes.Length);
            }

            Stream stream = null;
            try
            {
                stream = CloudUtils.BytesToStream(bytes);
            }
            catch (Exception e)
            {
                throw e;
            }

            if (null == stream || 0 == stream.Length)
            {
                throw new ArgumentException("Data stream is null");
            }

            var progress = new Progress<ResponseInfo>();
            progress.ProgressChanged += (o, info) =>
           {
               if (info.Filename == fileName &&
               !string.IsNullOrEmpty(info.ResponseContent))
               {
                   Task.Run(async () => await PutHttpResponse(fileName,
                       info.ResponseContent, stream,
                       token, callback, 0, null));
                   return;
               }
               Log.Instance.E(TAG, "Filename : " + info.Filename +
                   ", ResponseContent=" + info.ResponseContent);
               var args = new ProgressInfo(fileName, 0, 1)
               {
                   Error = ERROR_GENERAL_NULL
               };
               callback.Report(args);
           };
            Task.Run(async () =>
                await GetHttpResponse(fileName, GetHttpUrl(TYPE_UPLOAD,
                folder, category, fileName), token, progress, 0));
        }
    }
}