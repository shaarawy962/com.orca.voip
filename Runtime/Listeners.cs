using System;

using UnityEngine;
using orca.orcavoip.Base;

namespace orca.orcavoip
{

    
    public class Listeners
    {
        private Connection connection;

        private Handlers handlers;

        public Listeners()
        {
            connection = GameObject.FindObjectOfType<Connection>();
            handlers = GameObject.FindObjectOfType<Handlers>();
        }

        //public event OnDisconnect onUserDisconnect;

        // public event OnChannelUpdate channelUpdate;

        // private void OnEnable()
        // {
        //     onUserDisconnect = id => Debug.Log($"User disconnected with the id: {id} ");

        //     channelUpdate = OnchannelUpdate;
        // }

        private void OnchannelUpdate(string userid, Channel channel, webSocketEvent @event)
        {
            if (@event == webSocketEvent.JOIN_CHANNEL)
            {
                Debug.Log($"User {userid}, joined channel {channel.channelId}");
            }

            else if (@event == webSocketEvent.DISCONNECT)
            {
                Debug.Log($"User {userid} left channel {channel.channelId}");
            }

            else throw new ArgumentException($"Invalid enum value: {@event}");
        }

        public void disconnectEmitter()
        {
            Debug.Log($"{handlers.userId} disconnected from the channel");
        }

        public void ChannelUpdateEmitter(string userID, Channel channel, webSocketEvent ev)
        {
            Debug.Log($"channel {channel} has been updated by the {userID}");
        }

        public void JoinChannelEmitter(string channelID)
        {
            Debug.Log($"{handlers.userId} joined channel: {channelID}.");
        }
    }

}