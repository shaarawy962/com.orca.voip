using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.WebRTC;
using System.IO;



namespace orca.orcavoip
{

    public enum VoipType : int
    {
        P2P = 1,
        Broadcast = 2
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public class OrcaVOIP
    {

        private static OrcaVOIP instance = null;
        public static OrcaVOIP GetInstance
        {
            get
            {
                if (instance == null)
                    instance = new OrcaVOIP();
                return instance;
            }
        }

        private OrcaVOIP()
        {

        }

        private static AppSettings appSettings;

        public static AppSettings AppSettings
        {
            get
            {
                if (appSettings == null)
                {
                    LoadOrCreateSettings();
                }
                return appSettings;
            }
            private set
            {
                appSettings = value;
            }
        }

        public const string appSettingsFileName = "OrcaSetting";
        private const string IP = "";
        private const string PORT = "";
        string url = "156.208.154.74:8080";
        static internal string DummyChannelID;
        internal Base.Connection connection;
        internal Base.Handlers handler;

        public GameObject OrcaGameObject;

        internal AudioSource InputAudioSource, OutputAudioSource;


        //public static void InitMethod()
        //{
        //    EditorApplication.quitting += IsAppQuitting;
        //}

        public static void IsAppQuitting()
        {
#if UNITY_EDITOR
            AssetDatabase.Refresh();
            
            AssetDatabase.SaveAssets();
#endif
        }

        public void Initialize(AudioSource input = null)
        {
            WebRTC.Initialize();
            switch (OrcaVOIP.AppSettings.type)
            {
                case VoipType.P2P:
                    InitializeParams(AppSettings.type);
                    break;

                case VoipType.Broadcast:
                    InitializeParams(AppSettings.type);
                    break;
            }

            if (input == null)
            {
                var inputAudio = GameObject.FindObjectOfType<AudioSource>();
                GetInstance.InputAudioSource = inputAudio != GetInstance.OutputAudioSource ? inputAudio : null;
                if (GetInstance.InputAudioSource == null)
                {
                    Debug.LogError("Input audio source not found, Add an Audio Source component");
                    return;
                }
                else { Debug.Log($"Input audio source found {GetInstance.InputAudioSource}"); }
            }
        }

        internal void InitializeParams(VoipType type)
        {
            //OrcaGameObject = new GameObject("Orca Main Obj");

            if (type == VoipType.Broadcast)
            {
                var conn = GameObject.FindObjectOfType<Broadcast.BroadcastConnection>(true) as Broadcast.BroadcastConnection;
                GetInstance.connection = conn ?
                 conn : null;
                //OrcaGameObject.AddComponent(typeof(Broadcast.BroadcastConnection)) as Broadcast.BroadcastConnection;

                var currhandler = GameObject.FindObjectOfType<Broadcast.BroadcastHandlers>(true) as Broadcast.BroadcastHandlers;
                GetInstance.handler = currhandler ?
                 GetInstance.handler : null;
                //OrcaGameObject.AddComponent(typeof(Broadcast.BroadcastHandlers)) as Broadcast.BroadcastHandlers;
            }
            else if (type == VoipType.P2P)
            {
                var conn = GameObject.FindObjectOfType<P2P.P2PConnection>(true) as P2P.P2PConnection;
                instance.connection = conn ?
                 conn : null;
                //OrcaGameObject.AddComponent(typeof(P2P.P2PConnection)) as P2P.P2PConnection;

                var currhandler = GameObject.FindObjectOfType<P2P.P2PHandlers>(true) as P2P.P2PHandlers;
                instance.handler = currhandler ?
                 currhandler : null;
                //OrcaGameObject.AddComponent(typeof(P2P.P2PHandlers)) as P2P.P2PHandlers;
            }
            else throw new ArgumentException($"Invalid enum type: {type}");
        }

        async public Task Connect()
        {
            Type connType, handlerType;

            connType = instance.connection.GetType();
            handlerType = instance.handler.GetType();
            P2P.P2PConnection p2PConnection = null;
            Broadcast.BroadcastConnection broadcastConnection = null;

            if (connType == typeof(P2P.P2PConnection))
            {

                var conn = (P2P.P2PConnection)instance.connection;
                //conn.Connect(url);


                if (handlerType != typeof(P2P.P2PHandlers))
                {
                    throw new InvalidOperationException("Invalid Handler type compatibility");
                }
                else
                {
                    var currhandler = (P2P.P2PHandlers)instance.handler;
                    currhandler.connection = conn;
                    conn.handler = currhandler;

                    conn.SetParameters(AppSettings.url, "PEER_TO_PEER", AppSettings.AuthKey);
                }
                p2PConnection = conn;
            }

            else if (connType == typeof(Broadcast.BroadcastConnection))
            {

                var conn = (Broadcast.BroadcastConnection)instance.connection;

                broadcastConnection = conn;

                if (handlerType != typeof(Broadcast.BroadcastHandlers))
                {
                    throw new InvalidOperationException("Invalid Handler type compatibility");
                }

                else
                {
                    var currhandler = (Broadcast.BroadcastHandlers)instance.handler;
                    currhandler.connection = conn;
                    conn.handler = currhandler;

                    //conn.SetParameters(AppSettings.url, "Broadcast", AppSettings.AuthKey);
                }
            }
            await p2PConnection.ConnectAsync();
        }

        public void CreateChannel()
        {
            Type connType = instance.connection.GetType();
            if (connType == typeof(P2P.P2PConnection))
            {
                var conn = instance.connection as P2P.P2PConnection;
                conn.CreateChannel();
            }
            else if (connType == typeof(Broadcast.BroadcastConnection))
            {
                var conn = instance.connection as Broadcast.BroadcastConnection;
                conn.CreateChannel();
            }

        }

        public void JoinChannel(string channelID)
        {
            Debug.Log(instance.connection.GetType());
            if (instance.connection.GetType() == typeof(P2P.P2PConnection))
            {
                var conn = (P2P.P2PConnection)instance.connection;
                Debug.Log("Attempting to join channel");
                conn.JoinChannel(channelID);
            }
            else if (instance.connection.GetType() == typeof(Broadcast.BroadcastConnection))
            {
                var conn = (Broadcast.BroadcastConnection)instance.connection;
                Debug.Log("Attempting to join channel");
                conn.JoinChannel(channelID);
            }
        }

        public static void LoadOrCreateSettings()
        {
            if (appSettings != null)
            {
                return;
            }

            appSettings = (AppSettings)Resources.Load("OrcaSetting", typeof(AppSettings));
            if (appSettings != null) return;

            if (appSettings == null)
            {
                appSettings = (AppSettings)ScriptableObject.CreateInstance(typeof(AppSettings));
                if (appSettings == null)
                {
                    return;
                }
            }

#if UNITY_EDITOR
            if (!Directory.Exists("Assets/Resources/"))
            {
                try
                {
                    Directory.CreateDirectory("Assets/Resources/");
                }catch (IOException e)
                {
                    Debug.LogError($"Error while creating directory: {e}");
                }
            }
            AssetDatabase.CreateAsset(appSettings, "Assets/Resources/OrcaSetting.asset");
            AssetDatabase.SaveAssets();
#endif
        }
    }
}
