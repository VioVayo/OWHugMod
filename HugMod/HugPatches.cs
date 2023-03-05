using HarmonyLib;
using OWML.Logging;
using UnityEngine;
using static HugMod.PlayerHugController;

namespace HugMod
{
    [HarmonyPatch]
    public class HugPatches
    {
        //-----Simulate player input for walking towards hug target-----
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateMovement))]
        public static bool PlayerCharacterController_UpdateMovement_Prefix(PlayerCharacterController __instance)
        {
            Vector2 vector = OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.Character | InputMode.NomaiRemoteCam);

            if (WalkingTowardsHugTarget) vector = new Vector2(0, WalkingTowardsHugSpeed);

            float magnitude = vector.magnitude;
            if (__instance._groundBody != null)
            {
                if (magnitude == 0f)
                {
                    __instance.PreventSliding();
                }
                Vector3 pointAcceleration = __instance._groundBody.GetPointAcceleration(__instance._groundContactPt);
                Vector3 forceAcceleration = __instance._forceDetector.GetForceAcceleration();
                __instance._normalAcceleration = Vector3.Project(pointAcceleration - forceAcceleration, __instance._groundNormal);
            }
            bool flag = !OWInput.IsPressed(InputLibrary.rollMode, InputMode.Character | InputMode.NomaiRemoteCam, 0f) || __instance._heldLanternItem != null;
            float num = (flag ? ((vector.y < 0f) ? __instance._strafeSpeed : __instance._runSpeed) : __instance._walkSpeed);
            float num2 = (flag ? __instance._strafeSpeed : __instance._walkSpeed);
            if (__instance._heldLanternItem != null)
            {
                __instance._heldLanternItem.OverrideMaxRunSpeed(ref num2, ref num);
            }
            if (Locator.GetAlarmSequenceController() != null && Locator.GetAlarmSequenceController().IsAlarmWakingPlayer())
            {
                num = Mathf.Min(num, __instance._walkSpeed);
                num2 = Mathf.Min(num2, __instance._walkSpeed);
            }
            if (__instance._jumpChargeTime > 0f && !__instance._useChargeCurve)
            {
                float num3 = Mathf.InverseLerp(1f, 2f, __instance._jumpChargeTime);
                num = Mathf.Min(num, Mathf.Lerp(num, 2f, num3));
                num2 = Mathf.Min(num2, Mathf.Lerp(num2, 2f, num3));
            }
            Vector3 vector2 = new Vector3(vector.x * num2, 0f, vector.y * num);
            if (__instance._isStaggered)
            {
                float num4 = Mathf.Clamp01((Time.time - __instance._initStaggerTime) / __instance._staggerLength);
                vector2 *= num4;
                if (num4 == 1f)
                {
                    __instance._isStaggered = false;
                }
            }
            if (PlayerState.IsCameraUnderwater())
            {
                vector2 *= 0.5f;
            }
            else if (!flag || vector2.magnitude <= __instance._walkSpeed)
            {
                RaycastHit raycastHit;
                if (Physics.Raycast(__instance._transform.position + __instance._transform.TransformDirection(new Vector3(vector.x, 0f, vector.y).normalized * 0.1f), -__instance._transform.up, out raycastHit, 20f, OWLayerMask.groundMask))
                {
                    float num5 = raycastHit.distance - 1f;
                    if (num5 > 0.2f && (Vector3.Angle(__instance._owRigidbody.GetLocalUpDirection(), raycastHit.normal) > (float)__instance._maxAngleToBeGrounded || num5 > 1.5f))
                    {
                        vector2 = Vector3.zero;
                        vector = Vector2.zero;
                    }
                }
                else
                {
                    vector2 = Vector3.zero;
                    vector = Vector2.zero;
                }
            }
            __instance.SetPhysicsMaterial((magnitude > 0.01f || __instance._movingPlatform != null) ? __instance._runningPhysicMaterial : __instance._standingPhysicMaterial);
            Vector3 vector3 = __instance._transform.InverseTransformDirection(__instance._owRigidbody.GetVelocity());
            Vector3 vector4 = vector2 + __instance.GetLocalGroundFrameVelocity() - vector3;
            vector4.y = 0f;
            if (vector4.magnitude > __instance._tumbleThreshold)
            {
                __instance.InitTumble();
                return false;
            }
            float num6 = Time.fixedDeltaTime * 60f;
            float num7 = __instance._acceleration * num6;
            vector4.x = Mathf.Clamp(vector4.x, -num7, num7);
            vector4.z = Mathf.Clamp(vector4.z, -num7, num7);
            Vector3 vector5 = __instance._transform.TransformDirection(vector4);
            vector5 -= Vector3.Project(vector5, __instance._groundNormal);
            __instance._owRigidbody.AddVelocityChange(vector5);
            return false;
        }

        //yoinked from NH, thank you JohnCorby!
        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnityLogger), "OnLogMessageReceived")]
        public static bool UnityLogger_OnLogMessageReceived(string message)
        {
            // Filter out goofy error that doesn't actually break anything
            return !message.EndsWith(") is out of bounds (size=0)");
        }
    }
}
