using UnityEngine;
using System.Threading.Tasks;
using NativeWebSocket;
using System.Text;
using Unity.WebRTC;
using Newtonsoft.Json;
using orca.orcavoip.Base;
using System;
using UnityEditor;

namespace orca.orcavoip
{
    namespace P2P
    {

        using Snowflake = System.String;

        #region Delegates
        public delegate void CreateChannelEventHandler();
        public delegate void JoinChannelEventHandler(Snowflake channelId);

        public delegate void DisconnectFromChannelEventHandler();

        public delegate void OnDisconnect(Snowflake userID);

        public delegate void OnChannelUpdate(Snowflake userID, Channel channel, Event ev);

        #endregion



        [AddComponentMenu("OrcaSDK/P2P/P2PConnection")]
        [RequireComponent(typeof(P2P.P2PHandlers))]
        [RequireComponent(typeof(AudioSource))]
        public class P2PConnection : Connection
        {
            #region fields
            protected internal event CreateChannelEventHandler createChannel;
            protected internal event JoinChannelEventHandler joinChannel;

            string url, mode, key;

               

            #endregion

            #region callbacks


            public override void SendMessage(object param)
            {
                base.SendMessage(param);
            }

            public override void Connect(string url, string mode, string Key)
            {
                throw new NotImplementedException();
            }

            async public Task ConnectAsync()
            {


                
                //orcaApi = (OrcaVOIP) Resources.Load("OrcaSetting.asset") as OrcaVOIP;
                //if (orcaApi == null)
                //{
                //    Debug.LogError("Couldn't connect as config isn't initialized");
                //    return;
                //}

                //Debug.Log($"Key {OrcaVOIP.GetAuthKey()}");
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

                string encodedKey = Uri.EscapeDataString(key);


                // if (url == null)
                // {
                //     IP_ADDRESS = "167.172.100.251";
                //     websocket = new WebSocket($"ws://{IP_ADDRESS}:{PORT_NUMBER}");
                // }
                // else IP_ADDRESS = url;
                //IP_ADDRESS = IpAddress.text;
                websocket = new WebSocket($"ws://{url}?mode={mode}&key={encodedKey}");

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
                    int numberOfTrials = 0;
#if UNITY_EDITOR
                    if (!EditorApplication.isPlaying) return;
                    else if (EditorApplication.isPlaying)
                    {
                        while (numberOfTrials < 3)
                        {
                            numberOfTrials++;
                            Debug.Log("Error! " + e);
                            await Task.Delay(2500);
                            await ConnectAsync();
                        }
                        Debug.LogError("Unable to Connect to server");

                    }
#endif
                    if (Application.isPlaying)
                    {
                        while (numberOfTrials < 3)
                        {
                            numberOfTrials++;
                            Debug.Log("Error! " + e);
                            await Task.Delay(2500);
                            await ConnectAsync();
                            Debug.LogError("Unable to Connect to server");
                        }
                    }

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

                await websocket.Connect();
                // waiting for messages

            }

            public void SetParameters(Snowflake uri, Snowflake mode, Snowflake key)
            {
                this.url = uri;
                this.mode = mode;
                this.key = key;
            }


            public override void Update()
            {
                base.Update();
            }

            internal override void ChannelUpdateMethod(string userID, Base.Channel channel, webSocketEvent ev)
            {
                base.ChannelUpdateMethod(userID, channel, ev);
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
                

                Debug.Log($"AuthKey is set to {OrcaVOIP.AppSettings.AuthKey}");
                //JoinBtn.onClick.AddListener(joinChannelMethod);
                //LeaveBtn.onClick.AddListener(leaveChannelMethod);
                //connect.onClick.AddListener(Connect);



                handler = gameObject.GetComponent(typeof(Base.Handlers)) as Base.Handlers;// like cast 

                if (handler == null)
                {
                    handler = gameObject.AddComponent(typeof(Base.Handlers)).GetComponent<Base.Handlers>();
                }

            }

            protected override void CreateChannelMethod()
            {
                base.CreateChannelMethod();
            }

            protected override void joinChannelMethod()
            {
                // channelId = InputField.text;
                // Debug.Log($"Channel ID: {channelId}");
                // joinChannel?.Invoke(channelId);
                base.joinChannelMethod();
            }

            public void JoinChannel(string channelID)
            {
                var listener = new Listeners();
                joinChannel += listener.JoinChannelEmitter;
                joinChannel?.Invoke(channelID);
            }

            public void CreateChannel()
            {
                createChannel?.Invoke();
            }

            protected override void leaveChannelMethod()
            {
                // disconnectFromChannel += eventListener.disconnectEmitter;
                // disconnectFromChannel?.Invoke();
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
                        base.ChannelUpdateMethod(handler.userId, handler.channel, eventType);
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
        }
    }


}
