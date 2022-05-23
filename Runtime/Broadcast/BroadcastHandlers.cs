using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.WebRTC;
using Newtonsoft.Json;
using orca.orcavoip.Base;

namespace orca.orcavoip
{

    namespace Broadcast
    {
        using Snowflake = System.String;


        [AddComponentMenu("OrcaSDK/Broadcast/Handlers")]
        public class BroadcastHandlers : Base.Handlers
        {

            #region fields

            public Broadcast.Channel channel;
            // public Snowflake userId;

            // //private Listeners eventListener;

            // [SerializeField]
            // public AudioSource OutputAudioGameObj;

            // public BroadcastConnection connection = null;

            // public Dictionary<Snowflake, RTCPeerConnection> connections = new Dictionary<Snowflake, RTCPeerConnection>(); // UserId -> Connection

            // public AudioSource inputAudioSource;
            // public MediaStream inputStream, outputStream;
            // public string m_deviceName = null;

            // public AudioStreamTrack m_audioTrack;
            // public AudioClip audioclip;

            // private List<RTCRtpCodecCapability> availableCodes = new List<RTCRtpCodecCapability>();


            // public string channelId;

            private RTCPeerConnection serverConnection;

            #endregion

            #region Callbacks

            new private void Awake()
            {
                //eventListener = gameObject.GetComponent<Listeners>();
            }

            new private void Start()
            {
                //create connection object and assign its webSocket
                if (connection == null)
                {
                    connection = gameObject.GetComponent<BroadcastConnection>();
                }

                WebRTC.Initialize();
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

                // serverConnection = new RTCPeerConnection();


                foreach (var item in Microphone.devices)
                {
                    Debug.Log($"Microphone: {item.ToString()}");
                }
            }
            #endregion

            #region Methods
            internal void attachConnectionListeners(RTCPeerConnection connection)
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
                    if (candidate == null)
                    {
                        Debug.Log("Found null candidate");
                        return;
                    }

                    var candidateData = new IceCandidateData()
                    {
                        Candidate = candidate.Candidate,

                        SdpMid = candidate.SdpMid,

                        SdpMLineIndex = (int)candidate.SdpMLineIndex,
                    };
                    var iceData = new IceCandidateExchangeEventData { candidate = candidateData };
                    var iceForwardEvent = new WebSocketMessage<IceCandidateExchangeEventData> { type = webSocketEvent.ICE_FORWARD, data = iceData };

                    Debug.Log($"Sending ice candidate {iceForwardEvent}");
                    this.connection.SendMessage(iceForwardEvent);
                };

                //FIXME: runs only on 1 partner
                connection.OnTrack = (ev) =>
                {
                    Debug.Log($"Track received {ev.Track.Id}");
                    outputStream.AddTrack(ev.Track);
                };
            }

            public override void handleUpdateChannel(string message)
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

            public override IEnumerator<AsyncOperationBase> handleProvideOffer(string message)
            {
                var request = JsonConvert.DeserializeObject<WebSocketMessage<OfferEventData>>(message);
                Debug.Log($"Offer received from server: {request.data.offer}");


                Debug.Log("Setting remote description");
                var orcaOffer = new RTCSessionDescription () {
                    sdp = request.data.offer.sdp,
                    type = RTCSdpType.Offer
                };

                var setRemoteOp = serverConnection.SetRemoteDescription(ref orcaOffer);
                yield return setRemoteOp;

                if (setRemoteOp.IsError)
                {
                    throw new Exception($"setRemoteOp error {setRemoteOp.Error.message}");
                }

                var transceiver = serverConnection.AddTransceiver(TrackKind.Audio);
                transceiver.Direction = RTCRtpTransceiverDirection.SendRecv;
                transceiver.SetCodecPreferences(availableCodes.ToArray());

                var audioTrack = new AudioStreamTrack(inputAudioSource);
                serverConnection.AddTrack(audioTrack, inputStream);

                Debug.Log("Creating Answer");
                var answer = serverConnection.CreateAnswer();
                yield return answer;

                if (answer.IsError)
                {
                    throw new Exception($"answer error {answer.Error.message}");
                }

                var desc = answer.Desc;
                Debug.Log("Setting local description");
                var setLocalOp = serverConnection.SetLocalDescription(ref desc);
                yield return setLocalOp;

                if (setLocalOp.IsError)
                {
                    throw new Exception($"setLocalOp error {setLocalOp.Error.message}");
                }

                var answerDescription = new OrcaRTCSessionDescription()
                {
                    type = "answer",
                    sdp = desc.sdp
                };

                var answerEventData = new AnswerEventData { answer = answerDescription };
                var answerMessage = new WebSocketMessage<AnswerEventData> { type = webSocketEvent.ANSWER, data = answerEventData };

                Debug.Log("Sending answer");
                this.connection.SendMessage(answerMessage);
            }

            // public override void handleInit(string message)
            // {
            //     var request = JsonConvert.DeserializeObject<WebSocketMessage<InitResponse>>(message);
            //     Debug.Log($"INITIALIZE received from server");

            //     Debug.Log($"Creating connection with server");
            //     var configuration = GetSelectedSdpSemantics();
            //     var connection = new RTCPeerConnection(ref configuration);
            //     attachConnectionListeners(connection);

            //     var transceiver = connection.AddTransceiver(TrackKind.Audio);
            //     transceiver.Direction = RTCRtpTransceiverDirection.SendRecv;
            //     transceiver.SetCodecPreferences(availableCodes.ToArray());

            //     var audioTrack = new AudioStreamTrack(inputAudioSource);
            //     connection.AddTrack(audioTrack, inputStream);

            //     connection.OnNegotiationNeeded = () =>
            //     {
            //         Debug.Log("Negotiation needed");
            //         StartCoroutine(onNegotiationNeeded(connection));
            //     };

            //     return;
            // }

            public override void handleIceForward(string message)
            {
                Debug.Log($"Received ice candidate {message}");
                var request = JsonConvert.DeserializeObject<WebSocketMessage<IceCandidateExchangeEventData>>(message);

                var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                {
                    candidate = request.data.candidate.Candidate,
                    sdpMid = request.data.candidate.SdpMid,
                    sdpMLineIndex = request.data.candidate.SdpMLineIndex,
                });

                Debug.Log($"Setting ice candidate on connection: {request.data.candidate}");
                bool success = serverConnection.AddIceCandidate(candidate);
                Debug.Log($"Ice candidate on connection status {success}");
            }

            public override IEnumerator handleJoinedChannel(string message)
            {
                var request = JsonConvert.DeserializeObject<WebSocketMessage<JoinChannelResponseEventData>>(message);
                userId = request.data.userId;

                Debug.Log($"Assigned user id: {userId}");
                Debug.Log($"Users in channel: {request.data.users}");

                Debug.Log($"Creating connections for users in {channelId}");

                var configuration = GetSelectedSdpSemantics();
                serverConnection = new RTCPeerConnection(ref configuration);

                attachConnectionListeners(serverConnection);

                var initData = new InitRequest { };
                var init = new WebSocketMessage<InitRequest> { type = webSocketEvent.INITIALIZE, data = initData };
                Debug.Log($"Sending INITALIZE");
                this.connection.SendMessage(init);

                yield return null;

                // List<Snowflake> otherUsers = new List<Snowflake>();

                // foreach (Snowflake id in request.data.users)
                // {
                //     if (id != userId) otherUsers.Add(id);
                // }

                // foreach (Snowflake id in otherUsers)
                // {
                //     Debug.Log($"Creating connection with user {id}");
                //     var configuration = GetSelectedSdpSemantics();
                //     var connection = new RTCPeerConnection(ref configuration);
                //     attachConnectionListeners(connection, id);

                //     connections.Add(id, connection);

                //     var initData = new InitRequest { target = id };
                //     var init = new WebSocketMessage<InitRequest> { type = webSocketEvent.INITIALIZE, data = initData };

                //     Debug.Log($"Sending INITIALIZE to {id}");
                //     this.connection.SendMessage(init);

                //     yield return null;
                // };
            }


            #endregion

            #region static methods
            public override void handleCreatedChannel(string message)
            {
                Task.Run(() =>
                       {
                           var incomingMessage = JsonConvert.DeserializeObject<CreatedChannelEventData>(message);

                           if (channel == null)
                           {
                               Channel channel = new Channel(incomingMessage.channelId, new Snowflake[] { incomingMessage.userId });
                           }
                       });

                return;
            }
            new private static RTCConfiguration
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