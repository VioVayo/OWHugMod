using UnityEngine;
using GhostEnums;
using static HugMod.PlayerHugController;
using static HugMod.HuggableFrights.HuggableFrightsMain;

namespace HugMod.HuggableFrights
{
    public class WitnessedHugAction : GhostAction
    {
        private HugComponent hugComponent;

        private float stunTime;
        private float minStunTime = 40;
        private float maxStunTime = 60;

        private bool focussingLight = false;


        public override void Initialize(GhostData data, GhostController controller, GhostSensors sensors, GhostEffects effects)
        {
            base.Initialize(data, controller, sensors, effects);
            hugComponent = _controller.gameObject.GetComponent<HugComponent>();
        }

        public override Name GetName() { return WitnessedHugActionName; }

        public override float CalculateUtility() { return -200; }

        public override bool IsInterruptible() { return false; }

        public override void OnEnterAction() 
        { 
            stunTime = Random.Range(minStunTime, maxStunTime);
            hugComponent.ForceLookAtPlayer(true);
            focussingLight = Random.Range(0, 3) == 0;
            _controller.StopMoving();
            _controller.SetLanternConcealed(!focussingLight);
            _controller.FaceLocalPosition(_transform.parent.InverseTransformPoint(GetPlayerObjectTransform().position), TurnSpeed.MEDIUM);
        }

        public override bool Update_Action()
        {
            if (GetActionTimeElapsed() > stunTime) return false;

            var localPlayerPosition = _transform.parent.InverseTransformPoint(GetPlayerObjectTransform().position);
            UpdateAnimationStyle(localPlayerPosition);
            UpdateLantern(localPlayerPosition);
            UpdateFacing(localPlayerPosition);

            return true;
        }

        public override void OnExitAction() { hugComponent.ForceLookAtPlayer(false); }

        private void UpdateAnimationStyle(Vector3 localPlayerPosition)
        {
            var distance = Vector3.Distance(localPlayerPosition, _transform.localPosition);
            if (distance <= 7 && _effects._movementStyle != GhostEffects.MovementStyle.Stalk) _effects.SetMovementStyle(GhostEffects.MovementStyle.Stalk);
            else if (distance > 7 && _effects._movementStyle != GhostEffects.MovementStyle.Normal) _effects.SetMovementStyle(GhostEffects.MovementStyle.Normal);
        }

        private void UpdateLantern(Vector3 localPlayerPosition)
        {
            if (!focussingLight) return;

            var isClose = Vector3.Distance(localPlayerPosition, _transform.localPosition) < _controller.GetUnfocusedLanternRange();
            var refocusCondition = isClose ? _controller.GetDreamLanternController().GetFocus() != 0 : _controller.GetDreamLanternController().GetFocus() != 1;
            if (refocusCondition) _controller.ChangeLanternFocus(isClose ? 0 : 1);
        }

        private void UpdateFacing(Vector3 localPlayerPosition)
        {
            if (focussingLight || _transform.InverseTransformPoint(GetPlayerObjectTransform().position).z < 1) _controller.FaceLocalPosition(localPlayerPosition, TurnSpeed.MEDIUM);
        }
    }
}
