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

    namespace P2P
    {
        using Snowflake = System.String;

        [AddComponentMenu("OrcaSDK/P2P/P2PHandlers")]
        public class P2PHandlers : Handlers
        {

            #region fields

            public P2P.Channel channel;
            // public Snowflake userId;

            // //private Listeners eventListener;

            // [SerializeField]
            // public AudioSource OutputAudioGameObj = null;

            // public Connection connection = null;

            // public Dictionary<Snowflake, RTCPeerConnection> connections = new Dictionary<Snowflake, RTCPeerConnection>(); // UserId -> Connection

            // public AudioSource inputAudioSource = null;
            // public MediaStream inputStream, outputStream;
            // public string m_deviceName = null;

            // public AudioStreamTrack m_audioTrack;
            // public AudioClip audioclip;

            // private List<RTCRtpCodecCapability> availableCodes = new List<RTCRtpCodecCapability>();


            // public string channelId;

            #endregion

            #region Callbacks

            internal override void Awake()
            {
                //eventListener = gameObject.GetComponent<Listeners>();
                inputAudioSource = inputAudioSource == null ? FindObjectOfType<AudioSource>() : this.inputAudioSource;
            }

            internal override void Start()
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
            internal override void attachConnectionListeners(RTCPeerConnection connection, Snowflake pairId)
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
                    if (candidate == null)
                    {
                        Debug.Log("Found null candidate");
                        return;
                    }

                    var candidateData = new P2P.IceCandidateData()
                    {
                        Candidate = candidate.Candidate,

                        SdpMid = candidate.SdpMid,

                        SdpMLineIndex = (int)candidate.SdpMLineIndex,
                    };
                    var iceData = new P2P.IceCandidateExchangeEventRequest { target = pairId, candidate = candidateData };
                    var iceForwardEvent = new P2P.WebSocketMessage<P2P.IceCandidateExchangeEventRequest> { type = webSocketEvent.ICE_FORWARD, data = iceData };

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
                var request = JsonConvert.DeserializeObject<P2P.WebSocketMessage<P2P.ChannelUpdateEventData>>(message);
                Debug.Log($"Channel update message received");

                if (this.channel == null)
                {
                    return;
                }

                this.channel = request.data.channel;
            }

            #endregion

            #region Coroutines

            public override IEnumerator<AsyncOperationBase> handleAnswer(string message)
            {
                var request = JsonConvert.DeserializeObject<P2P.WebSocketMessage<P2P.AnswerEventResponse>>(message);
                Debug.Log($"Answer received from {request.data.source}: {request.data.answer}");

                var connection = connections[request.data.source];

                if (connection == null)
                {
                    throw new Exception("Connection not found for source {request.data.source}");
                }

                Debug.Log($"Setting remote description {request.data.answer}");
                var setRemoteOp = connection.SetRemoteDescription(ref request.data.answer);
                yield return setRemoteOp;


                if (setRemoteOp.IsError)
                {
                    throw new Exception($"setRemoteOp error {setRemoteOp.Error.message}");
                }
            }


            public override IEnumerator<AsyncOperationBase> onNegotiationNeeded(RTCPeerConnection connection, Snowflake target)
            {
                Debug.Log("Creating Offer");
                var offer = connection.CreateOffer();
                yield return offer;

                if (offer.IsError)
                {
                    throw new Exception($"offer error {offer.Error.message}");
                }

                if (connection.SignalingState != RTCSignalingState.Stable)
                {
                    throw new Exception($"signaling state is not stable");
                }

                var desc = offer.Desc;
                Debug.Log("Setting local description");

                var setLocalOp = connection.SetLocalDescription(ref desc);
                yield return setLocalOp;

                if (setLocalOp.IsError)
                {
                    throw new Exception($"setLocalOp error {setLocalOp.Error.message}");
                }

                var offerEventData = new P2P.OfferEventRequest { target = target, offer = offer.Desc };
                var offerEvent = new P2P.WebSocketMessage<P2P.OfferEventRequest> { type = webSocketEvent.OFFER, data = offerEventData };

                Debug.Log($"Sending offer {desc.sdp.ToString()}");
                this.connection.SendMessage(offerEvent);
            }


            public override IEnumerator<AsyncOperationBase> handleProvideOffer(string message)
            {
                var request = JsonConvert.DeserializeObject<P2P.WebSocketMessage<P2P.OfferEventResponse>>(message);
                Debug.Log($"Offer received from {request.data.source}: {request.data.offer}");

                if (!connections.ContainsKey(request.data.source)) throw new Exception("Offer source ID not associated with a connection");

                var connection = connections[request.data.source];

                Debug.Log("Setting remote description");
                var setRemoteOp = connection.SetRemoteDescription(ref request.data.offer);
                yield return setRemoteOp;

                if (setRemoteOp.IsError)
                {
                    throw new Exception($"setRemoteOp error {setRemoteOp.Error.message}");
                }

                var transceiver = connection.AddTransceiver(TrackKind.Audio);
                transceiver.Direction = RTCRtpTransceiverDirection.SendRecv;
                transceiver.SetCodecPreferences(availableCodes.ToArray());

                var audioTrack = new AudioStreamTrack(inputAudioSource);
                connection.AddTrack(audioTrack, inputStream);

                Debug.Log("Creating Answer");
                var answer = connection.CreateAnswer();
                yield return answer;

                if (answer.IsError)
                {
                    throw new Exception($"answer error {answer.Error.message}");
                }

                var desc = answer.Desc;
                Debug.Log("Setting local description");
                var setLocalOp = connection.SetLocalDescription(ref desc);
                yield return setLocalOp;

                if (setLocalOp.IsError)
                {
                    throw new Exception($"setLocalOp error {setLocalOp.Error.message}");
                }

                var answerEventData = new P2P.AnswerEventRequest { target = request.data.source, answer = answer.Desc };
                var answerMessage = new P2P.WebSocketMessage<P2P.AnswerEventRequest> { type = webSocketEvent.ANSWER, data = answerEventData };

                Debug.Log("Sending answer");
                this.connection.SendMessage(answerMessage);
            }

            public override void handleInit(string message)
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

            public override void handleIceForward(string message)
            {
                Debug.Log($"Received ice candidate {message}");
                var request = JsonConvert.DeserializeObject<P2P.WebSocketMessage<P2P.IceCandidateExchangeEventResponse>>(message);

                var connection = connections[request.data.source];
                var candidate = new RTCIceCandidate(new RTCIceCandidateInit
                {
                    candidate = request.data.candidate.Candidate,
                    sdpMid = request.data.candidate.SdpMid,
                    sdpMLineIndex = request.data.candidate.SdpMLineIndex,
                });

                Debug.Log($"Setting ice candidate on connection: {request.data.candidate}");
                bool success = connection.AddIceCandidate(candidate);
                Debug.Log($"Ice candidate on connection status {success}");
            }

            public override IEnumerator handleJoinedChannel(string message)
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
            public override void handleCreatedChannel(string message)
            {
                Task.Run(() =>
                       {
                           var incomingMessage = JsonConvert.DeserializeObject<P2P.CreatedChannelEventData>(message);

                           if (channel == null)
                           {
                               P2P.Channel channel = new P2P.Channel(incomingMessage.channelId, new Snowflake[] { incomingMessage.userId });
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