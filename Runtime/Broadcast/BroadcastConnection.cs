using UnityEngine;
using System.Threading.Tasks;
using NativeWebSocket;
using System.Text;
using Unity.WebRTC;
using Newtonsoft.Json;
using orca.orcavoip.Base;
using System;


namespace orca.orcavoip
{
    namespace Broadcast
    {

        using Snowflake = System.String;

        #region Delegates
        public delegate void CreateChannelEventHandler();
        public delegate void JoinChannelEventHandler(Snowflake channelId);

        public delegate void DisconnectFromChannelEventHandler();

        public delegate void OnDisconnect(Snowflake userID);

        public delegate void OnChannelUpdate(Snowflake userID, Channel channel, Event ev);

        #endregion


        [AddComponentMenu("OrcaSDK/Broadcast/Connection")]
        public class BroadcastConnection : Connection
        {
            #region fields

            // public string channelId;

            // public Channel channel;

            // public string userID;
            protected internal event CreateChannelEventHandler createChannel;
            protected internal event JoinChannelEventHandler joinChannel;

            // protected internal event DisconnectFromChannelEventHandler disconnectFromChannel;

            // protected BroadcastHandlers handler;

            // //protected Listeners eventListener = null;

            // [SerializeField]
            // string IP_ADDRESS;
            // [SerializeField]
            // string PORT_NUMBER;

            // [SerializeField]
            // private TMP_InputField InputField;

            // [SerializeField] private Button LeaveBtn;

            // [SerializeField] private Button CreateBtn;

            // [SerializeField] private Button JoinBtn;

            // public WebSocket websocket = null;

            // [SerializeField]
            // TMP_InputField IpAddress;

            // [SerializeField]
            // private Button connect;

            String url, mode, key;

            #endregion

            #region callbacks


            public override void SendMessage(object param)
            {
                websocket.SendText(JsonConvert.SerializeObject(param));
            }

            public override void Connect(String url, String mode, String key)
            {
                this.mode = mode;
                this.url = url;
                this.key = key;

                if (websocket != null && (websocket.State == WebSocketState.Open || websocket.State == WebSocketState.Connecting))
                {
                    Debug.LogError($"{websocket} socket is already connected");
                    return;
                }
                else if (websocket != null)
                {
                    websocket.Close();
                    websocket = null;
                }

                // if (url == null)
                // {
                //     IP_ADDRESS = "41.40.162.135";
                //     websocket = new WebSocket($"ws://{IP_ADDRESS}:{PORT_NUMBER}");
                // }
                // else
                // {
                //     IP_ADDRESS = url;
                //     websocket = new WebSocket($"ws://{IP_ADDRESS}");
                // }
                IP_ADDRESS = IpAddress.text.ToString();
                websocket = new WebSocket($"ws://{url}?mode={mode}?key={key}");


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
                    Connect(url, mode, key);
                };

                websocket.OnClose += (e) =>
                {
                    Debug.Log("Connection closed!");
                };

                disconnectFromChannel += () =>
                    {
                        //var connection = connections[this.userId];

                        //var connection = connections[handler.userId];

                        var message = new WebSocketMessage<LeaveChannelRequest>();
                        message.type = webSocketEvent.DISCONNECT;
                        message.data = new LeaveChannelRequest();
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

                websocket.Connect();
                // waiting for messages

            }


            new void Update()
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

            new private void Awake()
            {
                //eventListener = FindObjectOfType<Listeners>();

                JoinBtn.onClick.AddListener(joinChannelMethod);
                LeaveBtn.onClick.AddListener(leaveChannelMethod);
                //connect.onClick.AddListener(Connect);



                handler = gameObject.GetComponent(typeof(Base.Handlers)) as BroadcastHandlers;// like cast 
            }

            protected override void CreateChannelMethod()
            {
                base.CreateChannelMethod();
            }


            public void CreateChannel()
            {
                createChannel?.Invoke();
            }

            protected override void joinChannelMethod()
            {
                // channelId = InputField.text;
                // Debug.Log($"Channel ID: {channelId}");
                // joinChannel?.Invoke(channelId);
                base.joinChannelMethod();
            }


            public void JoinChannel(String channelID)
            {
                var listener = new Listeners();
                joinChannel += listener.JoinChannelEmitter;
                joinChannel?.Invoke(channelID);
            }
            protected override void leaveChannelMethod()
            {
                //disconnectFromChannel += eventListener.disconnectEmitter;
                base.leaveChannelMethod();
            }

            #endregion

            public override async Task HandleMessage(webSocketEvent eventType, string message)
            {
                Debug.Log($"Handling message: {message} {eventType}");

                switch (eventType)
                {
                    case webSocketEvent.INITIALIZE:
                        handler.handleInit(message);
                        break;

                    case webSocketEvent.CREATED_CHANNEL:
                        Debug.Log("CREATED_CHANNEL msg received!");
                        handler.handleCreatedChannel(message);
                        break;

                    case webSocketEvent.JOINED_CHANNEL:
                        StartCoroutine(handler.handleJoinedChannel(message));
                        break;

                    case webSocketEvent.OFFER:
                        StartCoroutine(handler.handleProvideOffer(message));
                        break;

                    case webSocketEvent.ANSWER:
                        StartCoroutine(handler.handleAnswer(message));
                        break;

                    case webSocketEvent.CHANNEL_UPDATE:
                        ChannelUpdateMethod(handler.userId, handler.channel, eventType);
                        handler.handleUpdateChannel(message);
                        break;

                    case webSocketEvent.ICE_FORWARD:
                        // await Task.Run(() =>
                        // {
                        //     Debug.Log($"Received ice candidate {message}");
                        //     var request = JsonConvert.DeserializeObject<WebSocketMessage<IceCandidateExchangeEventData>>(message);

                        //     var connection = handler.connections[request.data.source];
                        //     var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                        //     {
                        //         candidate = request.data.candidate.Candidate,
                        //         sdpMid = request.data.candidate.SdpMid,
                        //         sdpMLineIndex = request.data.candidate.SdpMLineIndex,
                        //     });

                        //     Debug.Log($"Setting ice candidate on connection: {request.data.candidate}");
                        //     connection.AddIceCandidate(candidate);
                        // });
                        handler.handleIceForward(message);
                        break;

                    default:
                        throw new ArgumentException($"Invalid enum value: {eventType} isn't a valid event type from the server");
                }
                //await base.HandleMessage(eventType, message);
            }

            internal override void ChannelUpdateMethod(string userID, Base.Channel channel, webSocketEvent ev)
            {
                base.ChannelUpdateMethod(userID, channel, ev);
            }
        }
    }


}
