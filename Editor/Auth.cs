using Codice.Client.Common;
using Newtonsoft.Json.Linq;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace ImmerzaSDK.Manager.Editor
{
    internal class AuthData
    {
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
            storeAuthData(newAuthData);
            return (newAuthData, "Sign in successful!");
        }

        internal async static Awaitable<bool> CheckAuthData(AuthData authData)
        {
            if (authData.IsExpired)
            {
                WWWForm formData = new();
                formData.AddField("refresh_token", authData.RefreshToken);

                using UnityWebRequest req = UnityWebRequest.Post(Constants.API_ROUTE_REFRESH_TOKEN, formData);
                await req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    return false;
                }

                JObject resObj = JObject.Parse(req.downloadHandler.text);
                authData = createAuthDataFromLoginResponse(resObj);
                storeAuthData(authData);
            }

            return true;
        }

        internal async static Awaitable<AuthData> Setup()
        {
            AuthData authData = loadAuthData();
            return await CheckAuthData(authData) ? authData : null;
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
            }

            return authData.IsExpired ? InvalidAuthData : authData;
        }
        private static AuthData createAuthDataFromLoginResponse(JObject response)
        {
            AuthData newAuthData = new();

            try
            {
                JToken refreshToken = response["refreshToken"];
                if (refreshToken != null)
                    newAuthData.RefreshToken = refreshToken.Value<string>();

                JToken accessToken = response["accessToken"];
                if (accessToken != null)
                    newAuthData.AccessToken = string.Format("Bearer {0}", accessToken.Value<string>());

                JToken expiresIn = response["expiresIn"];
                if (expiresIn != null)
                    newAuthData.ExpiresIn = DateTimeOffset.Now.ToUnixTimeSeconds() + expiresIn.Value<long>() - 20;
            }
            catch (ArgumentException e)
            {
                newAuthData = InvalidAuthData;
            }

            return newAuthData;
        }
    }
}
