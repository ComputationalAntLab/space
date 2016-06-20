﻿using UnityEngine;

namespace Assets.Scripts.Nests
{
    public class NestInfo
    {
        public bool IsStartingNest { get; private set; }
        public int NestId { get; private set; }

        public GameObject AntsAssessing { get; private set; }
        public GameObject AntsInactive { get; private set; }
        public GameObject AntsRecruiting { get; private set; }
        public GameObject AntsReversing { get; private set; }

        public NestInfo(int nestId, bool isStartingNest, GameObject assessing, GameObject recruiting, GameObject inactive, GameObject reversing)
        {
            NestId = nestId;
            IsStartingNest = isStartingNest;

            AntsAssessing = assessing;
            AntsRecruiting = recruiting;
            AntsInactive = inactive;
            AntsReversing = reversing;
        }
    }
}