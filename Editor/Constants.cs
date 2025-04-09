using UnityEngine;

namespace ImmerzaSDK.Manager.Editor
{
    internal static class Constants
    {
        internal const string API_BASE_URL = "https://api.ovok.com/";
        internal const string API_ROUTE_LOGIN = API_BASE_URL + "v2/auth/login";
        internal const string API_ROUTE_REFRESH_TOKEN = API_BASE_URL + "auth/refresh-token";
        internal const string API_ROUTE_RELEASES = API_BASE_URL + "fhir/Basic?code=sdk-release&_sort=-_lastUpdated&_count=1";
        internal const string API_ROUTE_FILES = API_BASE_URL + "document/";
        internal const string API_ROUTE_USER_INFO = API_BASE_URL + "auth/me";
        internal const string CLIENT_ID = "fc86ca24-e854-4c7f-bde1-fd5bb04d9a6d";

        internal const string SDK_BASE_PATH = "Assets\\Immerza\\";
    }
}
