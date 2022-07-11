using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.WebRTC;
using Newtonsoft.Json;

namespace orca.orcavoip
{

    namespace Base
    {
        using Snowflake = System.String;

        public class Handlers : MonoBehaviour
        {

            #region fields

            public Channel channel;
            public Snowflake userId;

            //private Listeners eventListener;

            [SerializeField]
            public AudioSource OutputAudioGameObj = null;

            public Connection connection = null;

            public Dictionary<Snowflake, RTCPeerConnection> connections = new Dictionary<Snowflake, RTCPeerConnection>(); // UserId -> Connection

            public AudioSource inputAudioSource = null;
            public MediaStream inputStream, outputStream;
            public string m_deviceName = null;

            public AudioStreamTrack m_audioTrack;
            public AudioClip audioclip;

            internal List<RTCRtpCodecCapability> availableCodes = new List<RTCRtpCodecCapability>();


            public string channelId;

            #endregion

            #region Callbacks

            internal virtual void Awake()
            {
                //eventListener = gameObject.GetComponent<Listeners>();
                inputAudioSource = inputAudioSource == null ? FindObjectOfType<AudioSource>() : this.inputAudioSource;
            }

            internal virtual void Start()
            {
                //create connection object and assign its webSocket
                if (connection == null)
                {
                    connection = gameObject.GetComponent<Connection>();
                }

                //WebRTC.Initialize();
                StartCoroutine(WebRTC.Update());

                var codecs = RTCRtpSender.GetCapabilities(TrackKind.Audio).codecs;
                var excludeCodecTypes = new[] { "audio/CN", "audio/telephone-event" };

                foreach (var codec in codecs)
                {
                    foreach (var excluded in excludeCodecTypes)
                    {
                        if (codec.mimeType.Contains(excluded) == true)
                        {
                            continue;
                        }

                        availableCodes.Add(codec);
                    }
                }

                string deviceName = Microphone.devices[0];
                Debug.Log($"Using input source {deviceName}");
                audioclip = Microphone.Start(deviceName, true, 1, 48000);

                while (!(Microphone.GetPosition(deviceName) > 0)) { }
                Debug.Log("start playing... position is " + Microphone.GetPosition(deviceName));

                inputAudioSource.loop = true;
                inputAudioSource.clip = audioclip;
                inputAudioSource.Play();

                Debug.Log($"Input audio source clip is null? {inputAudioSource.clip == null}");


                outputStream = new MediaStream();
                outputStream.OnAddTrack += (e) =>
                {
                    var track = e.Track as AudioStreamTrack;
                    Debug.Log($"Playing from output stream");

                    var outputAudioSource = Instantiate(OutputAudioGameObj, transform);

                    outputAudioSource.SetTrack(track);
                    outputAudioSource.loop = true;
                    outputAudioSource.Play();
                };

                inputStream = new MediaStream();


                foreach (var item in Microphone.devices)
                {
                    Debug.Log($"Microphone: {item.ToString()}");
                }
            }
            #endregion

            #region Methods
            internal virtual void attachConnectionListeners(RTCPeerConnection connection, Snowflake pairId)
            {
                connection.OnIceGatheringStateChange = (state) =>
                {
                    Debug.Log($"ICE connection state: {state}");
                };

                connection.OnConnectionStateChange = (RTCPeerConnectionState state) =>
                {
                    Debug.Log($"Peer connection state is {state}");
                    if (state == RTCPeerConnectionState.Disconnected)
                    {
                        connections.Remove(pairId);
                        connection.Close();
                        connection.Dispose();
                    }
                    else if (state == RTCPeerConnectionState.Failed)
                    {
                        Debug.Log("Retrying connection");
                        Task.Delay(1000);
                        connection.RestartIce();
                    }
                };

                connection.OnIceCandidate = (RTCIceCandidate candidate) =>
                {
                  
                };

                //FIXME: runs only on 1 partner
                connection.OnTrack = (ev) =>
                {
                    Debug.Log($"Track received {ev.Track.Id}");
                    outputStream.AddTrack(ev.Track);
                };
            }

            public virtual void handleUpdateChannel(string message)
            {
                var request = JsonConvert.DeserializeObject<WebSocketMessage<ChannelUpdateEventData>>(message);
                Debug.Log($"Channel update message received");

                if (this.channel == null)
                {
                    return;
                }

                this.channel = request.data.channel;
            }

            #endregion

            #region Coroutines

            public virtual IEnumerator<AsyncOperationBase> handleAnswer(string message)
            {
                yield return null;
            }


            public virtual IEnumerator<AsyncOperationBase> onNegotiationNeeded(RTCPeerConnection connection, Snowflake target)
            {
                yield return null;
            }


            public virtual IEnumerator<AsyncOperationBase> handleProvideOffer(string message)
            {
                yield return null;
            }

            public virtual void handleInit(string message)
            {
                var request = JsonConvert.DeserializeObject<P2P.WebSocketMessage<P2P.InitResponse>>(message);
                Debug.Log($"INITIALIZE received from {request.data.source}");

                Debug.Log($"Creating connection with user {request.data.source}");
                var configuration = GetSelectedSdpSemantics();
                var connection = new RTCPeerConnection(ref configuration);
                attachConnectionListeners(connection, request.data.source);

                var transceiver = connection.AddTransceiver(TrackKind.Audio);
                transceiver.Direction = RTCRtpTransceiverDirection.SendRecv;
                transceiver.SetCodecPreferences(availableCodes.ToArray());

                var audioTrack = new AudioStreamTrack(inputAudioSource);
                connection.AddTrack(audioTrack, inputStream);

                connections.Add(request.data.source, connection);

                connection.OnNegotiationNeeded = () =>
                {
                    Debug.Log("Negotiation needed");
                    StartCoroutine(onNegotiationNeeded(connection, request.data.source));
                };

                return;
            }

            public virtual void handleIceForward(string message)
            {
                
            }

            public virtual IEnumerator handleJoinedChannel(string message)
            {
                var request = JsonConvert.DeserializeObject<P2P.WebSocketMessage<P2P.JoinChannelResponseEventData>>(message);
                userId = request.data.userId;

                Debug.Log($"Assigned user id: {userId}");
                Debug.Log($"Users in channel: {request.data.users}");

                Debug.Log($"Creating connections for users in {channelId}");

                List<Snowflake> otherUsers = new List<Snowflake>();

                foreach (Snowflake id in request.data.users)
                {
                    if (id != userId) otherUsers.Add(id);
                }

                foreach (Snowflake id in otherUsers)
                {
                    Debug.Log($"Creating connection with user {id}");
                    var configuration = GetSelectedSdpSemantics();
                    var connection = new RTCPeerConnection(ref configuration);
                    attachConnectionListeners(connection, id);

                    connections.Add(id, connection);

                    var initData = new P2P.InitRequest { target = id };
                    var init = new P2P.WebSocketMessage<P2P.InitRequest> { type = webSocketEvent.INITIALIZE, data = initData };

                    Debug.Log($"Sending INITIALIZE to {id}");
                    this.connection.SendMessage(init);

                    yield return null;
                };
            }


            #endregion

            #region static methods
            public virtual void handleCreatedChannel(string message)
            {
                Task.Run(() =>
                {
                    var incomingMessage = JsonConvert.DeserializeObject<P2P.CreatedChannelEventData>(message);

                    if (channel == null)
                    {
                        Channel incomingChannel = new Channel(incomingMessage.channelId, new Snowflake[] { incomingMessage.userId });
                        this.channel = incomingChannel;
                    }
                });

                return;
            }
            internal static RTCConfiguration
            GetSelectedSdpSemantics()
            {
                RTCConfiguration config = default;
                config.iceServers = new[] { new RTCIceServer {
                 urls = new[] {
                "stun:stun.l.google.com:19302",
                 "stun:stun1.l.google.com:19302",
                 "stun:stun2.l.google.com:19302",
                 "stun:stun3.l.google.com:19302",
                 "stun:stun4.l.google.com:19302" } } };

                return config;
            }

            #endregion

        }

    }


}