using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shadow2D
{
    public class Collider2DTracker
    {
        private int _updateOnFrame;
        public Collider2D[] colliders;
        public Vector3[] positions;
        public Quaternion[] rotations;
        public bool collidersChangedOnLastUpdate = true;

        public bool UpdateColliders(Vector2 topLeft, Vector2 bottomRight, LayerMask layerMask)
        {
            if (_updateOnFrame == Time.frameCount)
            {
                return collidersChangedOnLastUpdate;
            }
            collidersChangedOnLastUpdate = InnerUpdateColliders(topLeft, bottomRight, layerMask);
            return collidersChangedOnLastUpdate;
        }

        private bool InnerUpdateColliders(Vector2 topLeft, Vector2 bottomRight, LayerMask layerMask)
        {
            _updateOnFrame = Time.frameCount;

            Collider2D[] oldColliders = colliders;
            colliders = Physics2D.OverlapAreaAll(topLeft, bottomRight, layerMask);
            // Sort them so we get them in the same order as before. This is required for the comparison we do later on.
            Array.Sort(colliders, (c1, c2) => c1.GetInstanceID() - c2.GetInstanceID());

            Vector3[] oldPositions = positions;
            positions = colliders.Select(c => c.transform.position).ToArray();
            Quaternion[] oldRotations = rotations;
            rotations = colliders.Select(c => c.transform.rotation).ToArray();

            // Ease fast checks first
            if (oldColliders == null || oldColliders.Length != colliders.Length)
            {
                return true;
            }

            for (int index = 0; index < colliders.Length; index++)
            {
                Collider2D newCollider = colliders[index];
                Collider2D oldCollider = oldColliders[index];

                if (newCollider != oldCollider)
                {
                    return true;
                }

                if (!ShadowMathUtils.Approximately(oldPositions[index], newCollider.transform.position))
                {
                    return true;
                }

                if (!ShadowMathUtils.Approximately(oldRotations[index], newCollider.transform.rotation))
                {
                    return true;
                }
            }

            return false;
        }
    }
}