using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using Newtonsoft.Json;

namespace ImmerzaSDK.Manager.Editor
{
    internal sealed class Client
    {
        internal static async Task<string> Get(string uri, AuthData authData)
        {
            return await SendRequest(UnityWebRequest.Get(uri), authData);
        }

        internal static async Task<string> Put(string uri, string bodyData, AuthData authData)
        {
            return await SendRequest(UnityWebRequest.Put(uri, bodyData), authData);
        }

        internal static async Task<string> Post(string uri, Dictionary<string, string> data, AuthData authData)
        {
            string payload = JsonConvert.SerializeObject(data);
            return await SendRequest(UnityWebRequest.Post(uri, payload, "application/json"), authData);
        }

        private static async Task<string> SendRequest(UnityWebRequest request, AuthData authData)
        {
            request.SetRequestHeader("Authorization", authData.AccessToken);
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return request.downloadHandler.text;
            }

            return string.Empty;
        }
    }
}
