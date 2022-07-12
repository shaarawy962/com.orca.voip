
using UnityEngine;

namespace orca.orcavoip
{


    public class AppSettings : ScriptableObject
    {
        [SerializeField]
        private string authkey = "";

        [SerializeField]
        public VoipType type;

        [SerializeField]
        public string url = "167.172.100.251:34197";

        public string AuthKey
        {

            get
            {
                return authkey;
            }
            set { authkey = value; }
        }

    }


}