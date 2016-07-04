using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.Ants
{
    public static class AntColours
    {
        public static class States
        {
            public static Color Inactive { get { return Color.black; } }

            public static Color Recruiting { get { return Color.blue; } }

            public static Color Scouting { get { return Color.white; } }

            public static Color Assessing { get { return Color.red; } }

            public static Color Reversing { get { return Color.yellow; } }
        }

        public static class NestAssessment
        {
            public static Color SecondVisit { get { return Color.gray; } }

            public static Color ReturningToHomeNest { get { return Color.cyan; } }

            public static Color ReturningToPotentialNest { get { return Color.magenta; } }
        }
    }
}
