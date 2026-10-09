using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace TitanGlenelg.Core.Playbook
{
    public enum PlayType : byte
    {
        Pass = 0,
        Run = 1,
        PlayAction = 2,
        Screen = 3
    }

    public enum FormationType : byte
    {
        PistolTripsRight = 0,
        PistolGladiator = 1,
        PistolSpread2x2 = 2,
        DefenseBase43 = 3,
        DefenseNickel335 = 4
    }

    [System.Serializable]
    public struct RoutePointDefinition
    {
        public Vector3 Point0;
        public Vector3 Point1;
        public Vector3 Point2;
        public Vector3 Point3;
        public float StemDuration;
        public float CutSharpness;
    }

    [System.Serializable]
    public struct PlayerAssignment
    {
        public AI.PlayerPosition Position;
        public Vector3 PreSnapAlignment;
        public RoutePointDefinition Route;
        public bool IsPrimaryReceiver;
        public bool IsPullingLineman;
        public Vector3 PullWaypoint;
        public Vector3 ZoneLandmark;
        public AI.PlayerPosition ManAssignment;
        public float CushionDistance;
        public float LeverageAngle;
    }

    public class PlayDefinition
    {
        public string PlayId;
        public string PlayName;
        public PlayType Type;
        public FormationType Formation;
        public string PlayDescription;
        public List<PlayerAssignment> Assignments = new List<PlayerAssignment>();
    }
}
