using Newtonsoft.Json;
using Unity.WebRTC;

namespace orca.orcavoip
{

    namespace Base
    {

        using Snowflake = System.String;

        #region Enums 
        public enum webSocketEvent
        {
            INITIALIZE,
            CREATE_CHANNEL,
            CREATED_CHANNEL,
            JOIN_CHANNEL,
            JOINED_CHANNEL,
            OFFER,
            ANSWER,
            ICE_FORWARD,
            DISCONNECT,
            CHANNEL_UPDATE
        }

        #endregion

        #region Classes

        public class RTCData
        {
            // public RTCSessionDescription description { get; set; }
            // public string id { get; set; }
        }

        public class WebSocketMessage<T>
        {
            public webSocketEvent type;
            public T data;
        }

        [JsonObject]
        public class EventData
        {
            [JsonProperty("type")]
            public webSocketEvent type;
        }

        public class WebSocketRTCMessage : WebSocketMessage<RTCData>
        {
            public WebSocketRTCMessage(webSocketEvent type, RTCData data = null)
            {
                this.type = type;
                this.data = data;
            }
        }

        public class AuthenticatedEventData
        {
            // [JsonProperty("token")]
            // public Snowflake token;
        }

        // public class RegisterUserRequestEventData
        // {

        // }

        // public class RegisterUserResponseEventData
        // {
        //     // [JsonProperty("token")]
        //     // public Snowflake token;

        //     [JsonProperty("userId")]
        //     public Snowflake userId;
        // }

        public class CreateChannelEventData : AuthenticatedEventData
        {
            [JsonProperty("userId")]
            public Snowflake userId;
        }

        public class CreatedChannelEventData : AuthenticatedEventData
        {
            [JsonProperty("channelId")]
            public Snowflake channelId;

            [JsonProperty("userId")]
            public Snowflake userId;
        }

        public class JoinChannelEventData
        {
            [JsonProperty("channelId")]
            public string channelId;
        }

        public class JoinChannelResponseEventData
        {
            [JsonProperty("userId")]
            public Snowflake userId;

            [JsonProperty("users")]
            public Snowflake[] users;
        }

        public class AnswerEventData
        {

            [JsonProperty("answer")]
            public RTCSessionDescription answer;
        }

        public class InitRequest
        {
        }

        public class InitResponse
        {

        }

        public class OfferEventData
        {

            [JsonProperty("offer")]
            public RTCSessionDescription offer;
        }

        public class RelayAnswerEventData
        {

            [JsonProperty("answer")]
            public RTCSessionDescription answer;
        }

        public class Channel
        {
            [JsonProperty("users")]
            public Snowflake[] users;

            [JsonProperty("channelId")]
            public Snowflake channelId;

            public Channel(Snowflake channelID, Snowflake[] users)
            {
                this.channelId = channelID;
                this.users = users;
            }
        }

        public class IceCandidateData
        {
            [JsonProperty("candidate")]
            public string Candidate;

            [JsonProperty("SdpMid")]
            public string SdpMid;

            [JsonProperty("SdpMLineIndex")]
            public int SdpMLineIndex;
        }


        public class LeaveChannelRequest
        {

        }

        public class ChannelUpdateEventData
        {
            [JsonProperty("channel")]
            public Channel channel;
        }

        #endregion
    }

}