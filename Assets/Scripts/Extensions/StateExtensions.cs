using System;

namespace Assets.Scripts.Extensions
{
    public static class StateExtensions
    {
        public static string StateName(this AntManager.BehaviourState state)
        {
            switch (state)
            {
                case AntManager.BehaviourState.Assessing:
                    return Naming.Ants.BehavourState.Assessing;
                case AntManager.BehaviourState.Inactive:
                    return Naming.Ants.BehavourState.Inactive;
                case AntManager.BehaviourState.Recruiting:
                    return Naming.Ants.BehavourState.Recruiting;
                default:
                    throw new ArgumentOutOfRangeException("state");
            }
        }
    }
}
