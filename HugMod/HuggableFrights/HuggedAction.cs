using UnityEngine;
using GhostEnums;
using static HugMod.PlayerHugController;
using static HugMod.HuggableFrights.HuggableFrightsMain;

namespace HugMod.HuggableFrights
{
    public class HuggedAction : GhostAction
    {
        private HugComponent hugComponent;
        private GhostNavigation ghostNavigation;

        //working variables
        private Vector3 hugLocation;
        private Vector3 currentTarget;
        private bool focussingLight;
        private bool shouldFocus;
        private float focusDelayTimer;

        //numbery things
        private float hugStunTime = 30;
        private float focusDelay = 0.4f;
        private float followDistance = 7;
        private float followLimit = 15;
        private float maxPositionOffset = 3.5f;

        private int chanceToStopWhileMoving = 1;
        private int chanceToNotMoveWhileStopped = 98;


        public override void Initialize(GhostData data, GhostController controller, GhostSensors sensors, GhostEffects effects)
        {
            base.Initialize(data, controller, sensors, effects);
            hugComponent = _controller.gameObject.GetComponent<HugComponent>();
            ghostNavigation = _controller.gameObject.GetComponent<GhostNavigation>();
        }

        public override Name GetName() { return HuggedActionName; }

        public override float CalculateUtility() { return -200; }

        public override bool IsInterruptible() { return false; }

        public override void OnEnterAction()
        {
            CheckForWitnesses();
            if (_data.previousAction != GetName())
            {
                if (hugLocation == null || !ghostNavigation.CheckForFull()) 
                { 
                    hugLocation = _transform.localPosition;
                    ghostNavigation.AddLocalWaypoint(hugLocation);
                }
                currentTarget = hugLocation;
                focussingLight = false;
                shouldFocus = false;
            }
            hugComponent.ForceLookAtPlayer(true);
            _controller.SetLanternConcealed(!focussingLight);
            _controller.FaceVelocity();
            _effects.SetMovementStyle(GhostEffects.MovementStyle.Normal);
            _effects.PlayVoiceAudioNear(AudioType.Ghost_Identify_Curious);
        }

        public override bool Update_Action()
        {
            if (hugComponent.IsSequenceInProgress()) return true;
            if (GetActionTimeElapsed() > hugStunTime) return false; 

            var localPlayerPosition = _transform.parent.InverseTransformPoint(GetPlayerObjectTransform().position);
            UpdateLantern(localPlayerPosition);
            UpdateFacing(localPlayerPosition);
            UpdateMovement(localPlayerPosition);

            return true; 
        }

        public override void OnExitAction() { hugComponent.ForceLookAtPlayer(false); }


        private void UpdateLantern(Vector3 localPlayerPosition)
        {
            var playerConcealed = Locator.GetDreamWorldController().GetPlayerLantern().GetLanternController().IsConcealed();
            var playerDroppedLantern = !Locator.GetDreamWorldController().GetPlayerLantern().GetLanternController().IsHeldByPlayer();
            var toggleCondition = shouldFocus ? _data.sensor.isPlayerHeldLanternVisible : (playerConcealed || playerDroppedLantern) && _data.wasPlayerLocationKnown;

            if (toggleCondition)
            { 
                shouldFocus = !shouldFocus;
                focusDelayTimer = Time.time + focusDelay;
            }
            if (focussingLight != shouldFocus && Time.time > focusDelayTimer)
            {
                focussingLight = shouldFocus;
                _controller.SetLanternConcealed(!focussingLight);
            }
            if (focussingLight)
            {
                var isClose = Vector3.Distance(localPlayerPosition, _transform.localPosition) < _controller.GetUnfocusedLanternRange();
                var refocusCondition = isClose ? _controller.GetDreamLanternController().GetFocus() != 0 : _controller.GetDreamLanternController().GetFocus() != 1;
                if (refocusCondition) _controller.ChangeLanternFocus(isClose ? 0 : 1);
            }
        }

        private void UpdateFacing(Vector3 localPlayerPosition)
        {
            if (focussingLight || shouldFocus)
            {
                _controller.FaceLocalPosition(localPlayerPosition, TurnSpeed.FAST);
                return;
            }

            var relativePlayerPosition = _transform.InverseTransformPoint(GetPlayerObjectTransform().position);
            if (_controller._facingState != GhostController.FacingState.FacePosition)
            {
                var hasBackToPlayer = !_controller.IsMoving() && relativePlayerPosition.z < 0;
                if (hasBackToPlayer) _controller.FaceLocalPosition(localPlayerPosition, TurnSpeed.MEDIUM);
            }
            else if (_controller._facingState != GhostController.FacingState.FaceVelocity)
            {
                var isFacingPlayer = relativePlayerPosition.z > 0.8f * relativePlayerPosition.magnitude;
                if (_controller.IsMoving() || isFacingPlayer) _controller.FaceVelocity();
            }
        }

        private void UpdateMovement(Vector3 localPlayerPosition)
        {
            var playerDistance = Vector3.Distance(localPlayerPosition, _transform.localPosition);
            var movedDistance = Vector3.Distance(_transform.localPosition, hugLocation);
            var playerMovedDistance = Vector3.Distance(localPlayerPosition, hugLocation);

            var random = Random.Range(0, 100);
            var stopChance = _controller.IsMoving() ? random < chanceToStopWhileMoving : random < chanceToNotMoveWhileStopped;

            if (playerDistance < followDistance || (movedDistance > followLimit && playerMovedDistance > followLimit) || stopChance || focussingLight || shouldFocus)
            {
                _controller.StopMoving();
                return;
            }

            if (!_controller.IsMoving()) currentTarget = localPlayerPosition + Random.onUnitSphere * maxPositionOffset;
            ghostNavigation.UpdateNavigationToTarget(currentTarget, MoveType.INVESTIGATE);
        }

        private void CheckForWitnesses()
        {
            foreach (var brain in GhostBrains)
            {
                if (Vector3.Distance(brain.gameObject.transform.position, _transform.position) > 50) continue;
                if (brain.GetCurrentActionName() == HuggedActionName) continue;

                var lookPosition = _sensors._sightOrigin.position;
                if (brain._sensors.CheckPointInVisionCone(lookPosition) && !brain._sensors.CheckLineOccluded(brain._sensors._sightOrigin.position, lookPosition))
                    brain.ChangeAction(WitnessedHugActionName);
            }
        }
    }
}
