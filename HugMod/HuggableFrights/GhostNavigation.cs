using System.Collections.Generic;
using UnityEngine;
using GhostEnums;

namespace HugMod.HuggableFrights
{
    public class GhostNavigation : MonoBehaviour //this class supplies movement code that doesn't use the node map system, for use by the multiple custom GhostActions
    {
        private GhostController ghostController;
        private List<Vector3> localWaypoints = new();

        private static int maxCapacity = 5;


        public void Start() 
        { 
            ghostController = gameObject.GetComponent<GhostController>();
            ghostController.OnNodeMapChanged += ClearWaypoints;
        }


        //the following methods allow us to keep track of the path an owl is taking off the node map, so they can retread it to find their way back
        public void AddLocalWaypoint(Vector3 localPosition) 
        { 
            localWaypoints.Add(localPosition);

            var n = localWaypoints.Count;
            if (n >= 3) //blunt corners if we didn't actually lure them around a corner
            {
                var angle = Vector3.Angle(localWaypoints[n - 1] - localWaypoints[n - 2], localWaypoints[n - 3] - localWaypoints[n - 2]);
                if (angle > 90) return;

                var pt = localWaypoints[n - 1] + 0.5f * (localWaypoints[n - 3] - localWaypoints[n - 1]);
                var ptGlobal = ghostController.LocalToWorldPosition(pt);
                var wpGlobal = ghostController.LocalToWorldPosition(localWaypoints[n - 2]);
                pt += (localWaypoints[n - 2] - pt) * (0.5f * angle / 90); //sharper corners get blunted more

                //check if obstacles were avoided with that corner
                var rc = Physics.Raycast(ghostController.LocalToWorldPosition(pt) + gameObject.transform.up * 0.25f, -gameObject.transform.up, out RaycastHit _, 0.5f, OWLayerMask.physicalMask);
                rc = rc && !Physics.Raycast(wpGlobal + gameObject.transform.up, (ptGlobal - wpGlobal).normalized, out RaycastHit _, (ptGlobal - wpGlobal).magnitude, OWLayerMask.physicalMask);
                if (rc) localWaypoints[n - 2] = pt;
            }
        }
        public void RemoveLastLocalWaypoint() { localWaypoints.Remove(localWaypoints[localWaypoints.Count - 1]); }
        public void ClearWaypoints() { localWaypoints.Clear(); }

        public Vector3 GetLastLocalWaypoint() => localWaypoints[localWaypoints.Count - 1];
        public bool IsWaypointsListEmpty() => localWaypoints.Count <= 0;
        public bool IsWaypointsListFull() => localWaypoints.Count >= maxCapacity;


        //actual movement is done here
        public void UpdateNavigationToTarget(Vector3 localTarget, MoveType moveType)
        {
            //keep the feetsies on the floor where they belong
            if (Physics.Raycast(gameObject.transform.position + gameObject.transform.up, -gameObject.transform.up, out RaycastHit hit, 1.25f, OWLayerMask.physicalMask)) 
                gameObject.transform.position = hit.point;

            //use existing navigation code while within bounds
            if (ghostController.GetNodeMap().CheckLocalPointInBounds(gameObject.transform.localPosition))
            {
                if (ghostController.GetLocalTargetPosition() != localTarget) ghostController.PathfindToLocalPosition(localTarget, moveType);
                return;
            }

            //continuously project target onto plane going through _transform.position
            var projection = Vector3.ProjectOnPlane(ghostController.LocalToWorldPosition(localTarget) - gameObject.transform.position, gameObject.transform.up) + gameObject.transform.position;

            //check for cliff edges
            var floorCheckOrigin = gameObject.transform.position + gameObject.transform.up + (projection - gameObject.transform.position).normalized;
            var floorCheck = Physics.Raycast(floorCheckOrigin, -gameObject.transform.up, out RaycastHit terrainHit, 1.5f, OWLayerMask.physicalMask); //50cm slope allowance
            if (!floorCheck)
            {
                //project backwards from the void to find the cliff edge so we can use it to get the angle between it and the owl
                var cliffCheck = Physics.Raycast(floorCheckOrigin - gameObject.transform.up * 1.25f, (gameObject.transform.position - projection).normalized, out terrainHit, 1, OWLayerMask.physicalMask);
                if (!cliffCheck) //failing that, there's nothing we can do for avoidance other than stop until there's a better target position
                {
                    ghostController.StopMoving();
                    return;
                }
                floorCheck = Vector3.Angle(terrainHit.normal, gameObject.transform.up) <= 45; //false alarm, it's just a steeper slope
            }

            if (floorCheck)
            {
                //account for elevation differences, I drew triangles to math this and everything
                var distanceFromTarget = Vector3.Distance(projection, gameObject.transform.position);
                var floorCheckHeight = gameObject.transform.InverseTransformPoint(terrainHit.point).y;
                var floorCheckXZDist = (terrainHit.point - gameObject.transform.position - floorCheckHeight * gameObject.transform.up).magnitude;
                if (floorCheckXZDist != 0) projection += (distanceFromTarget * floorCheckHeight / floorCheckXZDist) * gameObject.transform.up;
            }

            //obstacle avoidance
            var point1 = gameObject.transform.position + gameObject.transform.up * 0.9f;
            var point2 = gameObject.transform.position + gameObject.transform.up * 3.7f;
            var wallCheck = Physics.CapsuleCast(point1, point2, 0.8f, projection - gameObject.transform.position, out RaycastHit wallHit, 1, OWLayerMask.physicalMask);
            if (wallCheck) projection = Vector3.ProjectOnPlane(projection - wallHit.point, wallHit.normal) + wallHit.point + wallHit.normal;
            else if (!floorCheck) //walls have priority, in case the lack of floor is behind a wall
            {
                var cliffHitNormal = Vector3.ProjectOnPlane(-terrainHit.normal, gameObject.transform.up);
                projection = Vector3.ProjectOnPlane(projection - terrainHit.point, cliffHitNormal) + terrainHit.point + cliffHitNormal * 1.8f; //80cm of extra distance to match capsule radius above
            }

            ghostController.MoveToLocalPosition(gameObject.transform.parent.InverseTransformPoint(projection), moveType);
        }
    }
}
