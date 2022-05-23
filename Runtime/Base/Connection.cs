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

            public string channelId;

            public Channel channel;

            public string userID;
            protected internal event CreateChannelEventHandler createChannel;
            protected internal event JoinChannelEventHandler joinChannel;

            protected internal event DisconnectFromChannelEventHandler disconnectFromChannel;
            protected internal event OnChannelUpdate channelUpdate;

            internal Handlers handler;

            //protected Listeners eventListener = null;

            [SerializeField]
            internal string IP_ADDRESS;
            [SerializeField]
            internal string PORT_NUMBER;

            [SerializeField]
            internal TMP_InputField InputField;

            [SerializeField] internal Button LeaveBtn;

            [SerializeField] internal Button CreateBtn;

            [SerializeField] internal Button JoinBtn;

            public WebSocket websocket = null;

            [SerializeField]
            internal TMP_InputField IpAddress;

            [SerializeField]
            internal Button connect;

            #endregion

            #region callbacks


            public virtual void SendMessage(object param)
            {
                websocket.SendText(JsonConvert.SerializeObject(param));
            }

            async public virtual void Connect()
            {
                if (websocket != null && (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.Connecting))
                {
                    Debug.LogError($"{websocket} socket is already connected");
                    return;
                }
                else if (websocket != null)
                {
                    await websocket.Close();
                    websocket = null;
                }



                // if (url == null)
                // {
                //     IP_ADDRESS = "167.172.100.251";
                //     websocket = new WebSocket($"ws://{IP_ADDRESS}:{PORT_NUMBER}");
                // }
                // else IP_ADDRESS = url;
                // websocket = new WebSocket($"ws://{IP_ADDRESS}");
                IP_ADDRESS = IpAddress.text.ToString();
                websocket = new WebSocket($"ws://{IP_ADDRESS}:{PORT_NUMBER}");

                websocket.OnOpen += async () =>
                {
                    //WebRTC.Initialize();
                    Debug.Log("Connection open!");

                    createChannel += async () =>
                    {
                        Debug.Log("Channel Creating");
                        WebSocketMessage<CreateChannelEventData> message = new WebSocketMessage<CreateChannelEventData>();
                        message.type = webSocketEvent.CREATE_CHANNEL;
                        await websocket.SendText(JsonConvert.SerializeObject(message));
                        Debug.Log("Channel Created");
                    };

                    joinChannel += async (channelID) =>
                    {
                        Debug.Log($"Joining channel: {channelID}...");

                        WebSocketMessage<JoinChannelEventData> message = new WebSocketMessage<JoinChannelEventData>();

                        message.data = new JoinChannelEventData { channelId = channelID };
                        message.type = webSocketEvent.JOIN_CHANNEL;

                        Debug.Log($"Sending JOIN_CHANNEL {JsonConvert.SerializeObject(message)}");
                        await websocket.SendText(JsonConvert.SerializeObject(message));
                    };

                    /// <summary>
                    /// Either create or join channel based on choice
                    /// </summary>
                    /// <returns> event of created or joined channel respectively</returns>

                    await websocket.SendText(JsonConvert.SerializeObject("hey"));
                };

                websocket.OnError += async (e) =>
                {
                    Debug.Log("Error! " + e);
                    Debug.Log("Couldn't connect to server");
                    await Task.Delay(2500);
                    Connect();
                };

                websocket.OnClose += (e) =>
                {
                    Debug.Log("Connection closed!");
                };

                disconnectFromChannel += () =>
                    {
                        //var connection = connections[this.userId];

                        //var connection = connections[handler.userId];

                        var message = new P2P.WebSocketMessage<P2P.LeaveChannelRequest>();
                        message.type = webSocketEvent.DISCONNECT;
                        message.data = new P2P.LeaveChannelRequest();
                        Debug.Log($"Requesting to leave channel");
                        websocket.SendText(JsonConvert.SerializeObject(message));

                        foreach (var connection in handler.connections)
                        {
                            connection.Value.Close();
                            Debug.Log("Closing Connection");
                            handler.connections.Remove(connection.Key);
                            Debug.Log("Removing Connection");
                        }


                        /// Send web socket message to server with leaving channel event
                        /// Receive confirmation message
                        /// remove connection key from map
                        /// close rtc peer connection, dispose of WebRTC
                        /// connections.Remove(this.userId);
                        //connection.Close();
                    };


                websocket.OnMessage += async (bytes) =>
                {

                    /// <summary>
                    /// Cases
                    /// 1- Created Channel = NOTIFIES HOST OF CHANNEL ID {channelId, userId}
                    /// 2- JOINED_CHANNEL = {userId, users[]}
                    /// 3- REQUEST_OFFER = PINGS ALL CLIENTS TO PREPARE OFFER {USER_ID, CHANNEL_ID}
                    /// 4- REQUEST_ANSWER = PINGS JOINING CLIENT TO PREPARE ANSWER {USER_ID, CHANNEL_ID, OFFERS}
                    /// 5- RELAY_ANSWER = SENDS ANSWER TO ALL CLIENTS {USER_ID, CHANNEL_ID, OFFER, ANSWER}
                    /// </summary>
                    /// <returns>
                    /// doesn't return anything and sends confirmation from received messages
                    /// </returns>
                    string str = Encoding.UTF8.GetString(bytes);
                    Debug.Log($"Message received: {str}");

                    EventData message = JsonConvert.DeserializeObject<EventData>(str);

                    await HandleMessage(message.type, str);
                };

                // Keep sending messages at every 0.3s
                //InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);

                await websocket.Connect();
                // waiting for messages

            }

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
