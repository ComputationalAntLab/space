using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Extensions
{
    public static class InterfaceExtensions
    {
        public static Text TextByName(this MonoBehaviour uiScript, string name)
        {
            return uiScript.transform.ComponentFromChild<Text>(name);
        }
    }
}
