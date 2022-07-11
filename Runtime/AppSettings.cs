
using UnityEngine;

namespace orca.orcavoip
{

    
    public class AppSettings : ScriptableObject
    {
        [SerializeField]
        private string authkey = "";

        [SerializeField]
        public VoipType type;
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