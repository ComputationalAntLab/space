using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
  public static  class EntityExtensions
    {
        public static NestManager Nest(this GameObject gameObject)
        {
            return Component<NestManager>(gameObject);
        }

        public static AntManager AntManager(this GameObject gameObject)
        {
            return Component<AntManager>(gameObject);
        }

        public static AntMovement AntMovement(this GameObject gameObject)
        {
            return Component<AntMovement>(gameObject);
        }

        public static T Component<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null)
                return null;              

            return (T)gameObject.GetComponent(typeof(T));
        }
    }
}
