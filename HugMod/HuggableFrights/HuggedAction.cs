using UnityEngine;
using GhostEnums;
using static HugMod.PlayerHugController;
using static HugMod.HuggableFrights.HuggableFrightsMain;

namespace HugMod.HuggableFrights
{
    public class HuggedAction : GhostAction
    {
        private HugComponent hugComponent;

        //working variables
        private Vector3 hugLocation;
        private Vector3 currentTarget;
        private bool focussingLight;
        private bool shouldFocus;
        private float focusDelayTimer;

        //numbery things
        private float hugStunTime = 30;
        private float focusDelay = 0.4f;
        private float followDistance = 5;
        private float followLimit = 5;
        private float maxPositionOffset = 3;

        private int chanceToStopWhileMoving = 1;
        private int chanceToNotMoveWhileStopped = 98;


        public override void Initialize(GhostData data, GhostController controller, GhostSensors sensors, GhostEffects effects)
        {
            base.Initialize(data, controller, sensors, effects);
            hugComponent = _controller.gameObject.GetComponent<HugComponent>();
        }

        public override Name GetName() { return HuggedActionName; }

        public override float CalculateUtility() { return -200; }

        public override bool IsInterruptible() { return false; }

        public override void OnEnterAction()
        {
            if (_data.previousAction != GetName())
            {
                hugLocation = _transform.localPosition;
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
            if (GetActionTimeElapsed() > hugStunTime) 
            {
                hugComponent.ForceLookAtPlayer(false);
                return false; 
            }

            var playerPosition = _transform.parent.InverseTransformPoint(GetPlayerTransform().position);
            UpdateLantern(playerPosition);
            UpdateFacing(playerPosition);
            UpdateMovement(playerPosition);

            return true; 
        }


        private void UpdateLantern(Vector3 playerPosition)
        {
            var isClose = (playerPosition - _transform.localPosition).magnitude < _controller.GetUnfocusedLanternRange();
            var playerConcealed = Locator.GetDreamWorldController().GetPlayerLantern().GetLanternController().IsConcealed();
            var playerDroppedLantern = !Locator.GetDreamWorldController().GetPlayerLantern().GetLanternController().IsHeldByPlayer();
            var toggleCondition = false;
            if (shouldFocus) toggleCondition = _data.sensor.isPlayerHeldLanternVisible;
            else toggleCondition = (playerConcealed || playerDroppedLantern) && _data.wasPlayerLocationKnown;

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
                var refocusCondition = isClose ? _controller.GetDreamLanternController().GetFocus() != 0 : _controller.GetDreamLanternController().GetFocus() != 1;
                if (refocusCondition) _controller.ChangeLanternFocus(isClose ? 0 : 1);
            }
        }

        private void UpdateFacing(Vector3 playerPosition)
        {
            var relativePlayerPosition = _transform.InverseTransformPoint(GetPlayerTransform().position);
            if (_controller._facingState != GhostController.FacingState.FacePosition)
            {
                var hasBackToPlayer = !_controller.IsMoving() && relativePlayerPosition.z < 0;
                if (hasBackToPlayer || focussingLight || shouldFocus) _controller.FaceLocalPosition(playerPosition, shouldFocus ? TurnSpeed.FAST : TurnSpeed.MEDIUM);
            }
            else if (_controller._facingState != GhostController.FacingState.FaceVelocity)
            {
                var isFacingPlayer = relativePlayerPosition.z > 0.8f * relativePlayerPosition.magnitude;
                if (_controller.IsMoving() || isFacingPlayer) _controller.FaceVelocity();
            }
        }

        private void UpdateMovement(Vector3 playerPosition)
        {
            //keep the feetsies on the floor where they belong
            if (Physics.Raycast(_transform.position + _transform.up.normalized, -_transform.up, out RaycastHit hit, 1.25f, OWLayerMask.physicalMask)) _transform.position = hit.point;

            //check several conditions to see if movement should take place at all
            var playerDistance = (playerPosition - _transform.localPosition).magnitude;
            var movedDistance = (_transform.localPosition - hugLocation).magnitude;
            var playerMovedDistance = (playerPosition - hugLocation).magnitude;

            var floorCheckOrigin = _transform.position + _controller._velocity * 0.25f + _transform.up.normalized;
            var floorCheck = Physics.Raycast(floorCheckOrigin, -_transform.up, out RaycastHit floorHit, 1.5f, OWLayerMask.physicalMask);

            var random = Random.Range(0, 100);
            var stopChance = _controller.IsMoving() ? random < chanceToStopWhileMoving : random < chanceToNotMoveWhileStopped;

            if (playerDistance < followDistance || (movedDistance > followLimit && playerMovedDistance > followLimit) || !floorCheck || stopChance || focussingLight || shouldFocus)
            {
                _controller.StopMoving();
                return;
            }

            //continuously projects target onto plane going through _transform.position
            if (!_controller.IsMoving()) currentTarget = GetPlayerTransform().position + Random.onUnitSphere * maxPositionOffset;
            var projection = Vector3.ProjectOnPlane(currentTarget - _transform.position, _transform.up) + _transform.position;

            //account for elevation differences, I drew triangles to math this and everything
            if (floorCheck && _controller.IsMoving())
            {
                var dist = (projection - _transform.position).magnitude;
                var checkHeight = _transform.InverseTransformPoint(floorHit.point).y;
                var checkDist = Vector3.ProjectOnPlane(_controller._velocity * 0.25f - _transform.position, _transform.up).magnitude;
                if (checkDist != 0) projection += (dist * checkHeight / checkDist) * _transform.up.normalized;
            }

            _controller.MoveToLocalPosition(_transform.parent.InverseTransformPoint(projection), MoveType.INVESTIGATE);
        }
    }
}
