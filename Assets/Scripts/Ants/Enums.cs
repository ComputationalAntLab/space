using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Ants
{
    public enum BehaviourState
    {
        Inactive,
        Scouting,
        Assessing,
        Recruiting,
        Following,
        Reversing,
        ReversingLeading,
        Leading,
        Carrying,
    };

    public enum NestAssessmentStage
    {
        Assessing,
        ReturningToHomeNest,
        ReturningToPotentialNest
    }
}
