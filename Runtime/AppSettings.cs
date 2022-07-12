
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
        public string url;

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