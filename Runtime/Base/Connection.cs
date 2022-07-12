using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using NativeWebSocket;
using System.Text;
using Unity.WebRTC;
using Newtonsoft.Json;
using System;


namespace orca.orcavoip
{
    namespace Base
    {

        using Snowflake = System.String;

        #region Delegates
        public delegate void CreateChannelEventHandler();
        public delegate void JoinChannelEventHandler(Snowflake channelId);

        public delegate void DisconnectFromChannelEventHandler();

        public delegate void OnDisconnect(Snowflake userID);

        public delegate void OnChannelUpdate(Snowflake userID, Channel channel, webSocketEvent ev);

        #endregion



        [AddComponentMenu("OrcaSDK/P2P/Connection")]
        public abstract class Connection : MonoBehaviour
        {
            #region fields

            [NonSerialized]
            public string channelId;

            public Channel channel;

            [NonSerialized]
            public string userID;
            protected internal event CreateChannelEventHandler createChannel;
            protected internal event JoinChannelEventHandler joinChannel;

            protected internal event DisconnectFromChannelEventHandler disconnectFromChannel;
            protected internal event OnChannelUpdate channelUpdate;

            internal Handlers handler;

            //protected Listeners eventListener = null;

            
            internal string IP_ADDRESS;
            
            internal string PORT_NUMBER;

            
            internal TMP_InputField InputField;

            internal Button LeaveBtn;

            internal Button CreateBtn;

            internal Button JoinBtn;

            public WebSocket websocket = null;

            [NonSerialized]
            internal TMP_InputField IpAddress;

            [NonSerialized]
            internal Button connect;

            #endregion

            #region callbacks


            public virtual void SendMessage(object param)
            {
                websocket.SendText(JsonConvert.SerializeObject(param));
            }

            public abstract void Connect(String url, String mode, String Key);

            internal virtual void ChannelUpdateMethod(Snowflake userID, Channel channel, webSocketEvent ev)
            {
                var listener = new Listeners();
                channelUpdate += listener.ChannelUpdateEmitter;
                channelUpdate?.Invoke(userID, channel, ev);
            }


            public virtual void Update()
            {
                if (websocket != null)
                {
                    websocket.DispatchMessageQueue();
                }
            }

            private async void OnApplicationQuit()
            {
                if (websocket != null)
                {
                    WebRTC.Dispose();
                    await websocket.Close();
                }
            }

            public virtual void Awake()
            {
                //eventListener = FindObjectOfType<Listeners>();

                JoinBtn.onClick.AddListener(joinChannelMethod);
                LeaveBtn.onClick.AddListener(leaveChannelMethod);
                //connect.onClick.AddListener(Connect);



                handler = gameObject.GetComponent(typeof(Handlers)) as Handlers;// like cast 

                if (handler == null)
                {
                    handler = gameObject.AddComponent(typeof(Handlers)).GetComponent<Handlers>();
                }

            }

            protected virtual void CreateChannelMethod()
            {
                createChannel?.Invoke();
            }

            protected virtual void joinChannelMethod()
            {
                var listener = new Listeners();
                channelId = InputField.text;
                Debug.Log($"Channel ID: {channelId}");
                joinChannel += listener.JoinChannelEmitter;
                joinChannel?.Invoke(channelId);
            }

            protected virtual void leaveChannelMethod()
            {
                var listener = new Listeners();
                disconnectFromChannel += listener.disconnectEmitter;
                disconnectFromChannel?.Invoke();
            }

            #endregion

            public async virtual Task HandleMessage(webSocketEvent eventType, string message)
            {
                // Debug.Log($"Handling message: {message} {eventType}");

                // switch (eventType)
                // {
                //     case webSocketEvent.INITIALIZE:
                //         handler.handleInit(message);
                //         break;

                //     case webSocketEvent.CREATED_CHANNEL:
                //         Debug.Log("CREATED_CHANNEL msg received!");
                //         handler.handleCreatedChannel(message);
                //         break;

                //     case webSocketEvent.JOINED_CHANNEL:
                //         StartCoroutine(handler.handleJoinedChannel(message));
                //         break;

                //     case webSocketEvent.OFFER:
                //         StartCoroutine(handler.handleProvideOffer(message));
                //         break;

                //     case webSocketEvent.ANSWER:
                //         StartCoroutine(handler.handleAnswer(message));
                //         break;

                //     case webSocketEvent.CHANNEL_UPDATE:
                //         handler.handleUpdateChannel(message);
                //         break;

                //     case webSocketEvent.ICE_FORWARD:
                //         // await Task.Run(() =>
                //         // {
                //         //     Debug.Log($"Received ice candidate {message}");
                //         //     var request = JsonConvert.DeserializeObject<WebSocketMessage<IceCandidateExchangeEventData>>(message);

                //         //     var connection = handler.connections[request.data.source];
                //         //     var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                //         //     {
                //         //         candidate = request.data.candidate.Candidate,
                //         //         sdpMid = request.data.candidate.SdpMid,
                //         //         sdpMLineIndex = request.data.candidate.SdpMLineIndex,
                //         //     });

                //         //     Debug.Log($"Setting ice candidate on connection: {request.data.candidate}");
                //         //     connection.AddIceCandidate(candidate);
                //         // });
                //         handler.handleIceForward(message);
                //         break;

                //     default:
                //         throw new ArgumentException($"Invalid enum value: {eventType} isn't a valid event type from the server");
                //}
            }
        }
    }


}
