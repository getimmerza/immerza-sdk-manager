using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace ImmerzaSDK.Manager.Editor
{
    internal class User
    {
        public string Id { get; set; }
        public string ResourceType { get; set; }
        public string Name { get; set; }
        public string Mail { get; set; }
    }
    internal class AuthData
    {
        public User User { get; set; } = new User();
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public long ExpiresIn { get; set; } = 0;
        public bool IsExpired => ExpiresIn == 0 || ExpiresIn <= DateTimeOffset.Now.ToUnixTimeSeconds();
    }

    internal static class Auth
    {
        private const string KEY_ACCESS_TOKEN  = "ImmerzaAccessToken";
        private const string KEY_REFRESH_TOKEN = "ImmerzaRefreshToken";
        private const string KEY_TOKEN_EXPIRE  = "ImmerzaTokenExpiration";

        internal static readonly AuthData InvalidAuthData = new();

        internal async static Task<(string, bool)> GetRequest(string route, string token)
        {
            using UnityWebRequest request = UnityWebRequest.Get(route);
            request.SetRequestHeader("Authorization", token);
            request.SetRequestHeader("Accept", "application/json");
            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return (request.downloadHandler.text, true);
            }

            return (request.error, false);
        }

        internal async static Awaitable<(AuthData, string)> SignIn(string email, string password)
        {
            WWWForm formData = new();
            formData.AddField("email", email);
            formData.AddField("password", password);
            formData.AddField("clientId", Constants.CLIENT_ID);

            using UnityWebRequest req = UnityWebRequest.Post(Constants.API_ROUTE_LOGIN, formData);
            await req.SendWebRequest();

            JObject resObj = JObject.Parse(req.downloadHandler.text);

            if (req.result != UnityWebRequest.Result.Success)
            {
                return (null, (string)resObj["message"]);
            }

            AuthData newAuthData = createAuthDataFromLoginResponse(resObj);
            if (!await LoadUserData(newAuthData))
            {
                return (null, "Failed requesting user data from backend");
            }

            storeAuthData(newAuthData);

            return (newAuthData, "Sign in successful!");
        }

        internal async static Awaitable<bool> CheckAuthData(AuthData authData)
        {
            if (authData.IsExpired)
            {
                Debug.Log(authData.RefreshToken);
                using UnityWebRequest req = UnityWebRequest.Post(Constants.API_ROUTE_REFRESH_TOKEN, $"{{\"refresh_token\": \"{authData.RefreshToken}\"}}", "application/json");
                req.SetRequestHeader("Content-Type", "application/json");
                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Log.LogError($"Request failed with '{req.result}': {req.downloadHandler.text}", LogChannelType.SDKManager);
                    return false;
                }

                JObject resObj = JObject.Parse(req.downloadHandler.text);
                authData = createAuthDataFromLoginResponse(resObj);
                storeAuthData(authData);
            }

            return true;
        }

        internal async static Awaitable<bool> LoadUserData(AuthData authData)
        {
            if (!await CheckAuthData(authData))
            {
                Log.LogError("Refreshing access token failed...", LogChannelType.SDKManager);
                return false;
            }

            (string data, bool success) = await GetRequest(Constants.API_ROUTE_USER_INFO, authData.AccessToken);
            if (!success)
            {
                Log.LogError($"Request failed with: {data}", LogChannelType.SDKManager);
                return false;
            }

            try
            {
                JObject result = JObject.Parse(data);
                authData.User = new User
                {
                    Id = (string)result["profile"]["id"],
                    ResourceType = (string)result["profile"]["resourceType"],
                    Name = (string)result["profile"]["name"][0]["given"][0] + " " + (string)result["profile"]["name"][0]["family"],
                    Mail = (string)result["user"]["email"]
                };
            }
            catch (ArgumentException e)
            {
                Log.LogError($"Malformed response: {e.Message}", LogChannelType.SDKManager);
                return false;
            }

            return true;
        }

        internal async static Awaitable<AuthData> Setup()
        {
            AuthData authData = loadAuthData();
            if (await LoadUserData(authData))
            {
                return authData;
            }
            return null;
        }

        internal static void ClearLogoutData()
        {
            storeAuthData(InvalidAuthData);
        }

        private static void storeAuthData(AuthData authData)
        {
            EditorPrefs.SetString(KEY_ACCESS_TOKEN, authData.AccessToken);
            EditorPrefs.SetString(KEY_REFRESH_TOKEN, authData.RefreshToken);
            EditorPrefs.SetString(KEY_TOKEN_EXPIRE, authData.ExpiresIn.ToString());
        }
        private static AuthData loadAuthData()
        {
            AuthData authData = new();

            if (EditorPrefs.HasKey(KEY_ACCESS_TOKEN) && EditorPrefs.HasKey(KEY_REFRESH_TOKEN) && EditorPrefs.HasKey(KEY_TOKEN_EXPIRE))
            {
                authData.AccessToken = EditorPrefs.GetString(KEY_ACCESS_TOKEN);
                authData.RefreshToken = EditorPrefs.GetString(KEY_REFRESH_TOKEN);
                if (long.TryParse(EditorPrefs.GetString(KEY_TOKEN_EXPIRE), out long tokenExpiresIn))
                {
                    authData.ExpiresIn = tokenExpiresIn;
                }

                return authData;
            }

            return InvalidAuthData;
        }
        private static AuthData createAuthDataFromLoginResponse(JObject response)
        {
            AuthData newAuthData = new();

            try
            {
                JToken refreshToken = response["refreshToken"] ?? response["refresh_token"];
                if (refreshToken != null)
                    newAuthData.RefreshToken = refreshToken.Value<string>();

                JToken accessToken = response["accessToken"] ?? response["access_token"];
                if (accessToken != null)
                    newAuthData.AccessToken = string.Format("Bearer {0}", accessToken.Value<string>());

                JToken expiresIn = response["expiresIn"] ?? response["expires_in"];
                if (expiresIn != null)
                    newAuthData.ExpiresIn = DateTimeOffset.Now.ToUnixTimeSeconds() + expiresIn.Value<long>() - 20;
            }
            catch (ArgumentException e)
            {
                Log.LogError($"Malformed response: {e.Message}", LogChannelType.SDKManager);
                newAuthData = InvalidAuthData;
            }

            return newAuthData;
        }
    }
}
