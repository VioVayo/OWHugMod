using UnityEngine;
using GhostEnums;
using static HugMod.HuggableFrights.HuggableFrightsMain;

namespace HugMod.HuggableFrights
{
    public class ReturnAction : GhostAction //uses the GhostNavigation waypoints list to lead the Owl back to its node map if it was lured out of bounds
    {
        private GhostBrain ghostBrain;
        private GhostNavigation ghostNavigation;

        private bool returnFromOutOfAreaBounds = false;

        private static float questioningDecisionsTime = 5;


        public override void Initialize(GhostData data, GhostController controller, GhostSensors sensors, GhostEffects effects)
        {
            base.Initialize(data, controller, sensors, effects);
            ghostBrain = _controller.gameObject.GetComponent<GhostBrain>();
            ghostNavigation = _controller.gameObject.GetComponent<GhostNavigation>();
        }

        public override Name GetName() => ReturnActionName;

        public override float CalculateUtility() 
        {
            if (ghostNavigation.IsWaypointsListEmpty()) return -200;
            if (!_running && _data.previousAction != ReturnActionName && _data.currentAction != Name.Chase) return 200; //guaranteed to start if waypoints list isn't empty, unless-
            if (_controller.GetNodeMap().CheckLocalPointInBounds(_transform.localPosition)) return 91; //allows chase if seen while in bounds
            return 99; //allows grab if in contact
        }

        public override bool IsInterruptible() => GetActionTimeElapsed() > questioningDecisionsTime;

        public override void OnEnterAction()
        {
            returnFromOutOfAreaBounds = !_controller.GetNodeMap().CheckLocalPointInBounds(_transform.localPosition);
            ghostNavigation.AddLocalWaypoint(_transform.localPosition); //add a new waypoint even beyond capacity for corner blunting purposes, remove immediately
            ghostNavigation.RemoveLastLocalWaypoint();
            _controller.SetLanternConcealed(ghostBrain._startWithLanternConcealed);
            _controller.FaceVelocity();
            _effects.SetMovementStyle(GhostEffects.MovementStyle.Normal);
            if (Random.Range(0, 4) == 0) _effects.PlayVoiceAudioNear(AudioType.Ghost_HuntFail);
        }

        public override bool Update_Action()
        {
            if (returnFromOutOfAreaBounds && _controller.GetNodeMap().CheckLocalPointInBounds(_transform.localPosition)) ghostNavigation.ClearWaypoints(); //stop action if Owl found its way back from out of bounds
            if (!ghostNavigation.IsWaypointsListEmpty()) ghostNavigation.UpdateNavigationToTarget(ghostNavigation.GetLastLocalWaypoint(), MoveType.PATROL);
            return true;
        }

        public override void OnArriveAtPosition() //this might also be called when arriving near the projection of a waypoint onto an obstacle plane rather than at the waypoint itself
        {
            if (ghostNavigation.IsWaypointsListEmpty() || Vector3.Distance(_transform.localPosition, ghostNavigation.GetLastLocalWaypoint()) > 1) return;

            if (returnFromOutOfAreaBounds) ghostNavigation.RemoveLastLocalWaypoint();
            else ghostNavigation.ClearWaypoints(); //walk back exactly one waypoint and then stop action if started in bounds
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
