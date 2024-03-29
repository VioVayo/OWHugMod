﻿using System.Collections.Generic;
using UnityEngine;
using GhostEnums;

namespace HugMod.HuggableFrights
{
    public class GhostNavigation : MonoBehaviour
    {
        private GhostController ghostController;

        private List<Vector3> localWaypoints = new();
        private int maxCapacity = 5;

        private bool isInBounds = false, wasInBounds = false;


        public void Start() { ghostController = gameObject.GetComponent<GhostController>(); }


        public void AddLocalWaypoint(Vector3 localPosition) { localWaypoints.Add(localPosition); }
        public void RemoveLastLocalWaypoint() { localWaypoints.Remove(localWaypoints[localWaypoints.Count - 1]); }
        public void ClearWaypoints() { localWaypoints.Clear(); }

        public Vector3 GetLastLocalWaypoint() { return localWaypoints[localWaypoints.Count - 1]; }
        public bool IsWaypointsListEmpty() { return localWaypoints.Count <= 0; }
        public bool IsWaypointsListFull() { return localWaypoints.Count >= maxCapacity; }


        public void UpdateNavigationToTarget(Vector3 localTarget, MoveType moveType)
        {
            //keep the feetsies on the floor where they belong
            if (Physics.Raycast(gameObject.transform.position + gameObject.transform.up.normalized, -gameObject.transform.up, out RaycastHit hit, 1.25f, OWLayerMask.physicalMask)) 
                gameObject.transform.position = hit.point;

            //use existing navigation code while within bounds
            wasInBounds = isInBounds;
            isInBounds = ghostController.GetNodeMap().CheckLocalPointInBounds(gameObject.transform.localPosition);
            if (isInBounds)
            {
                if (ghostController.GetLocalTargetPosition() != localTarget) ghostController.PathfindToLocalPosition(localTarget, moveType);
                return;
            }
            else if (wasInBounds) AddLocalWaypoint(gameObject.transform.localPosition);

            //continuously projects target onto plane going through _transform.position
            var globalTarget = ghostController.LocalToWorldPosition(localTarget);
            var projection = Vector3.ProjectOnPlane(globalTarget - gameObject.transform.position, gameObject.transform.up) + gameObject.transform.position;

            //check for cliff edges
            var floorCheckOffsetMagnitude = ghostController._acceleration == 0 ? ghostController._velocity.magnitude : Mathf.Pow(ghostController._velocity.magnitude, 2) / ghostController._acceleration;
            var floorCheckOrigin = gameObject.transform.position + gameObject.transform.up.normalized + ghostController.LocalToWorldDirection(ghostController._velocity).normalized * floorCheckOffsetMagnitude;
            var floorCheck = Physics.Raycast(floorCheckOrigin, -gameObject.transform.up, out RaycastHit floorHit, 1.5f, OWLayerMask.physicalMask);
            if (!floorCheck)
            {
                ghostController.StopMoving();
                return;
            }

            //account for elevation differences, I drew triangles to math this and everything
            var distanceFromTarget = Vector3.Distance(projection, gameObject.transform.position);
            var floorCheckHeight = gameObject.transform.InverseTransformPoint(floorHit.point).y;
            var floorCheckXZDist = (floorHit.point - gameObject.transform.position - floorCheckHeight * gameObject.transform.up).magnitude;
            if (floorCheckXZDist != 0) projection += (distanceFromTarget * floorCheckHeight / floorCheckXZDist) * gameObject.transform.up.normalized;

            //obstacle avoidance
            var point1 = gameObject.transform.position + gameObject.transform.up.normalized * 0.9f;
            var point2 = gameObject.transform.position + gameObject.transform.up.normalized * 3.7f;
            var wallCheck = Physics.CapsuleCast(point1, point2, 0.8f, projection - gameObject.transform.position, out RaycastHit wallHit, 1, OWLayerMask.physicalMask);
            if (wallCheck) projection = Vector3.ProjectOnPlane(projection - wallHit.point, wallHit.normal) + wallHit.point + wallHit.normal;

            ghostController.MoveToLocalPosition(gameObject.transform.parent.InverseTransformPoint(projection), moveType);
        }
    }
}
