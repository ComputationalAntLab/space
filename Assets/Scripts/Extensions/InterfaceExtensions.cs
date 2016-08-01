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

        public static Text TextByName(this GameObject gameObject, string name)
        {
            return gameObject.transform.ComponentFromChild<Text>(name);
        }

        public static Button ButtonByName(this GameObject gameObject, string name)
        {
            return gameObject.transform.ComponentFromChild<Button>(name);
        }

        public static Button ButtonByName(this MonoBehaviour uiScript, string name)
        {
            return uiScript.transform.ComponentFromChild<Button>(name);
        }

        public static void SetText(this Button button, string text)
        {
            button.GetComponentInChildren<Text>().text = text;
        }
    }
}
