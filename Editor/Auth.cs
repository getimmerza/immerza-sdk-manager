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
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public long ExpiresIn { get; set; }
    }

    internal static class Auth
    {
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

            AuthData newAuthData = new()
            {
                AccessToken = (string)resObj["refreshToken"],
                RefreshToken = "Bearer " + (string)resObj["accessToken"],
                ExpiresIn = DateTimeOffset.Now.ToUnixTimeSeconds() + (long)resObj["expiresIn"] - 20
            };

            EditorPrefs.SetString("ImmerzaAccessToken", newAuthData.AccessToken);
            EditorPrefs.SetString("ImmerzaRefreshToken", newAuthData.RefreshToken);
            EditorPrefs.SetString("ImmerzaTokenExpiration", newAuthData.ExpiresIn.ToString());

            return (newAuthData, "Sign in successful!");
        }

        internal async static Awaitable<bool> CheckAuthData(AuthData authData)
        {
            if (authData.ExpiresIn <= DateTimeOffset.Now.ToUnixTimeSeconds())
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

                authData.AccessToken = (string)resObj["refreshToken"];
                authData.RefreshToken = "Bearer " + (string)resObj["accessToken"];
                authData.ExpiresIn = DateTimeOffset.Now.ToUnixTimeSeconds() + (long)resObj["expiresIn"] - 20;

                EditorPrefs.SetString("ImmerzaAccessToken", authData.AccessToken);
                EditorPrefs.SetString("ImmerzaRefreshToken", authData.RefreshToken);
                EditorPrefs.SetString("ImmerzaTokenExpiration", authData.ExpiresIn.ToString());

                return true;
            }

            return true;
        }

        internal async static Awaitable<AuthData> Setup()
        {
            AuthData authData = new()
            {
                AccessToken = EditorPrefs.HasKey("ImmerzaAccessToken") ? EditorPrefs.GetString("ImmerzaAccessToken") : "",
                RefreshToken = EditorPrefs.HasKey("ImmerzaAccessToken") ? EditorPrefs.GetString("ImmerzaAccessToken") : "",
                ExpiresIn = EditorPrefs.HasKey("ImmerzaAccessToken") ? long.Parse(EditorPrefs.GetString("ImmerzaTokenExpiration")) : 0
            };

            return await CheckAuthData(authData) ? authData : null;
        }
    }
}
