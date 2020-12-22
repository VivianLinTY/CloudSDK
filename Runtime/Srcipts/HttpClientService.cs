using System;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace Cloud.Client
{
    internal class HttpClientService
    {
        private readonly static string TAG = "[HttpClientService]";

        private static HttpClient _httpClient;
        private static DateTime _ttl;

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void CreateClient()
        {
            if (null != _httpClient)
            {
                _httpClient.Dispose();
            }
            _httpClient = new HttpClient();

            _ttl = DateTime.UtcNow.AddHours(1);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void RefreshClient()
        {
            if (DateTime.UtcNow > _ttl)
            {
                Log.Instance.D(TAG, "Refresh _httpClient");
                CreateClient();
            }
        }

        internal static HttpClient ZHttpClient
        {
            get
            {
                if (null == _httpClient)
                {
                    Log.Instance.D(TAG, "null == _httpClient");
                    CreateClient();
                }
                else
                {
                    RefreshClient();
                }
                return _httpClient;
            }

        }
    }
}