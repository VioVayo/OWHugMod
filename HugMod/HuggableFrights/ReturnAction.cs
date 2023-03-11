﻿using UnityEngine;
using GhostEnums;
using static HugMod.HuggableFrights.HuggableFrightsMain;

namespace HugMod.HuggableFrights
{
    public class ReturnAction : GhostAction
    {
        private GhostBrain ghostBrain;
        private GhostNavigation ghostNavigation;

        private bool returnFromOutOfAreaBounds = false;

        private float questioningDecisionsTime = 5;


        public override void Initialize(GhostData data, GhostController controller, GhostSensors sensors, GhostEffects effects)
        {
            base.Initialize(data, controller, sensors, effects);
            ghostBrain = _controller.gameObject.GetComponent<GhostBrain>();
            ghostNavigation = _controller.gameObject.GetComponent<GhostNavigation>();
        }

        public override Name GetName() { return ReturnActionName; }

        public override float CalculateUtility() 
        {
            if (ghostNavigation.CheckForEmpty()) return -200;
            if (!_running || GetActionTimeElapsed() <= questioningDecisionsTime) return 200;
            if (_controller.GetNodeMap().CheckLocalPointInBounds(_transform.localPosition)) return 91; //allows chase if seen
            return 99; //allows grab if in contact
        }

        public override void OnEnterAction()
        {
            returnFromOutOfAreaBounds = !_controller.GetNodeMap().CheckLocalPointInBounds(_transform.localPosition);
            _controller.SetLanternConcealed(ghostBrain._startWithLanternConcealed);
            _controller.FaceVelocity();
            _effects.SetMovementStyle(GhostEffects.MovementStyle.Normal);
            if (Random.Range(0, 4) == 0) _effects.PlayVoiceAudioNear(AudioType.Ghost_HuntFail);
        }

        public override bool Update_Action()
        {
            if (returnFromOutOfAreaBounds && _controller.GetNodeMap().CheckLocalPointInBounds(_transform.localPosition)) ghostNavigation.ClearWaypoints();

            if (!ghostNavigation.CheckForEmpty()) 
            {
                var distanceFromWaypoint = Vector3.Distance(ghostNavigation.GetLastLocalWaypoint(), _transform.localPosition);
                if (distanceFromWaypoint <= 0.5f) 
                {
                    if (returnFromOutOfAreaBounds) ghostNavigation.RemoveLastLocalWaypoint();
                    else ghostNavigation.ClearWaypoints(); //walk back exactly one waypoint if started in bounds
                }
            }
            if (!ghostNavigation.CheckForEmpty()) ghostNavigation.UpdateNavigationToTarget(ghostNavigation.GetLastLocalWaypoint(), MoveType.PATROL);

            return true;
        }

        public override void OnExitAction()
        {
            if (!_controller.GetNodeMap().CheckLocalPointInBounds(_transform.localPosition))
            {
                _controller.GetNodeMap().FindClosestEdge(_transform.localPosition, out var closestPoint);
                _controller.PathfindToLocalPosition(closestPoint, MoveType.PATROL);
            }
        }
    }
}
