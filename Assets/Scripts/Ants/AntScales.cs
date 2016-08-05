using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Ants
{
    public static class AntScales
    {
        public static class Speeds
        {
            public const float Scouting = 5;                //speed of an active ant while not tandem running or carrying
            public const float TandemRunning = 5;               //speed of an ant while tandem running
            public const float Carrying = 37.5f;                //speed of an ant while carrying another ant
            public const float Inactive = .5f;             //speed of an ant in the inactive state
            public const float AssessingFirstVisit = 2.6595f;  //speed of an ant in the assessing state (first visit)

            // non-intersection 4.06 mm/s, intersection 2.72mm/s 
            public const float AssessingFirstVisitNonIntersecting = 3.2175f; //speed of an ant in the assessing state (second visit) when not intersecting with pheromones
            public const float AssessingFirstVisitSecondVisitIntersecting = 1.605f; //speed of an ant in the assessing state (second visit) when intersecting with pheromones


            public const float PheromoneFrequencyFTR = 0.2664f;   // 1.8mm/s => 12.5f ! FTR 3 lay per 10mm => 12.5mm/s (speed) lay pheromone every 0.2664 secs 
            public const float PheromoneFrequencyRTR = 0.8f;      // 1.8mm/s => 12.5f ! FTR 1 lay per 10mm => 12.5mm/s (speed) lay pheromone every 0.8 secs

            public const float PheromoneFrequencyBuffon = 0.0376f;        //the frequency that pheromones are laid for buffon needle
        }

        public static class Distances
        {
            public const float DoorSensing = 5f;

            public const float PheromoneSensing = 1; //maximum distance that pheromones can be sensed from

            // buffon needle
            // Eamonn B. Mallon and Nigel R. Franks - Ants estimate area using Buffon’s needle
            // The speeds at intersections were noted when an ant was within one antenna’s length of its first visit path.
            // one antenna lenght = 1mm. As pheromone range a radius around ant assessmentPheromoneRange = 0.5
            public const float AssessmentPheromoneSensing = .5f;


            public const float AverageAntenna = 2.0f;        // antenna reach value (1mm) => radius of ant is 0.5mm therefore to get 1mm gap between ants need to include 2*0.5mm 
            public const float LeaderStopping = 2.0f;         // leader stops when gap between leader and follower is 2mm. => must be 3.0f as radius of ant is 0.5mm

            public const float TandemFollowerLagging = 0.95f; // The distance from the leader causing follower to change direction 

            public const float AssessingDoor = 1;
            public const float AssessingNestMiddle = 2;

            public const float DoorEntry = 1.2f;
        }
    }
}
