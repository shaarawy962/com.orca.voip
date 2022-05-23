using System;
using UnityEngine;

using Unity.WebRTC;

// public class OrcaVOIP : MonoBehaviour
// {
//     // Start is called before the first frame update
//     void Start()
//     {

//     }

//     // Update is called once per frame
//     void Update()
//     {

//     }
// }


namespace orca.orcavoip
{


    public enum VoipType : int
    {
        P2P = 1,
        Broadcast = 2
    }


    public static class OrcaVOIP
    {

        private static string AuthKey = "";
        private const string IP = "167.172.100.251";
        private const string PORT = "34197";
        static internal string url = "167.172.100.251:34197";
        static internal string DummyChannelID;
        static internal Base.Connection connection;
        static internal Base.Handlers handler;

        public static GameObject OrcaGameObject;

        static internal AudioSource InputAudioSource, OutputAudioSource;

        public static void Initialize(VoipType type, AudioSource input = null)
        {
            WebRTC.Initialize();
            switch (type)
            {
                case VoipType.P2P:
                    // connection = GameObject.FindObjectOfType<OrcaSdk.P2P.Connection>() as OrcaSdk.P2P.Connection;
                    // handler = GameObject.FindObjectOfType<OrcaSdk.P2P.Handlers>() as OrcaSdk.P2P.Handlers;
                    InitializeParams(VoipType.P2P);
                    break;

                case VoipType.Broadcast:
                    // connection = GameObject.FindObjectOfType<OrcaSdk.Broadcast.Connection>() as OrcaSdk.Broadcast.Connection;
                    // handler = GameObject.FindObjectOfType<OrcaSdk.Broadcast.Handlers>() as OrcaSdk.Broadcast.Handlers;
                    InitializeParams(VoipType.Broadcast);
                    break;
            }

            if (input == null)
            {
                var inputAudio = GameObject.FindObjectOfType<AudioSource>();
                OrcaVOIP.InputAudioSource = inputAudio != OutputAudioSource ? inputAudio : null;
                if (OrcaVOIP.InputAudioSource == null)
                {
                    Debug.LogError("Input audio source not found, Add an Audio Source component");
                    return;
                }
                else { Debug.Log($"Input audio source found {OrcaVOIP.InputAudioSource}"); }
            }
        }

        static internal void InitializeParams(VoipType type)
        {
            OrcaGameObject = new GameObject("Orca Main Obj");

            if (type == VoipType.Broadcast)
            {
                var conn = GameObject.FindObjectOfType<Broadcast.BroadcastConnection>(true) as Broadcast.BroadcastConnection;
                OrcaVOIP.connection = conn ?
                 conn :
                 OrcaGameObject.AddComponent(typeof(Broadcast.BroadcastConnection)) as Broadcast.BroadcastConnection;

                var handler = GameObject.FindObjectOfType<Broadcast.BroadcastHandlers>(true) as Broadcast.BroadcastHandlers;
                OrcaVOIP.handler = handler ?
                 (Base.Handlers)handler :
                 OrcaGameObject.AddComponent(typeof(Broadcast.BroadcastHandlers)) as Broadcast.BroadcastHandlers;
            }
            else if (type == VoipType.P2P)
            {
                var conn = GameObject.FindObjectOfType<P2P.P2PConnection>(true) as P2P.P2PConnection;
                OrcaVOIP.connection = conn ?
                 conn :
                 OrcaGameObject.AddComponent(typeof(P2P.P2PConnection)) as P2P.P2PConnection;

                var handler = GameObject.FindObjectOfType<P2P.P2PHandlers>(true) as P2P.P2PHandlers;
                OrcaVOIP.handler = handler ?
                 handler :
                 OrcaGameObject.AddComponent(typeof(P2P.P2PHandlers)) as P2P.P2PHandlers;
            }
            else throw new ArgumentException($"Invalid enum type: {type}");
        }

        public static void SetAuthKey(string key)
        {
            AuthKey = key;
            Debug.Log($"Auth Key set to {AuthKey}");
        }
        public static string GetAuthKey(){
            return AuthKey;
        }

        public static void Connect()
        {
            Type connType, handlerType;

            connType = OrcaVOIP.connection.GetType();
            handlerType = OrcaVOIP.handler.GetType();

            if (connType == typeof(P2P.P2PConnection))
            {
                var conn = (P2P.P2PConnection)OrcaVOIP.connection;
                //conn.Connect(url);

                if (handlerType != typeof(P2P.P2PHandlers))
                {
                    throw new InvalidOperationException("Invalid Handler type compatibility");
                }
                else
                {
                    var handler = (P2P.P2PHandlers)OrcaVOIP.handler;
                    handler.connection = conn;
                    conn.handler = handler;
                }
            }
        }

        public static void CreateChannel()
        {

        }

        public static void JoinChannel(string channelID)
        {
            Debug.Log(OrcaVOIP.connection.GetType());
            if (OrcaVOIP.connection.GetType() == typeof(P2P.P2PConnection))
            {
                var conn = (P2P.P2PConnection)OrcaVOIP.connection;
                Debug.Log("Attempting to join channel");
                //conn.JoinChannel(channelID);
            }
            else if (OrcaVOIP.connection.GetType() == typeof(Broadcast.BroadcastConnection))
            {
                var conn = (Broadcast.BroadcastConnection)OrcaVOIP.connection;
                Debug.Log("Attempting to join channel");
            }
        }
    }
}
