﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace HoloToolkit.UI.Keyboard
{
    public class AxisSlider : MonoBehaviour
    {

        public enum EAxis
        {
            X,
            Y,
            Z
        }

        public EAxis Axis = EAxis.X;

        private float currentPos;
        private float slideVel;

        public float slideAccel = 5.25f;
        public float slideFriction = 6f;
        public float deadZone = 0.55f;
        public float clampDistance = 300.0f;
        public float bounce = 0.5f;

        [HideInInspector]
        public Vector3 TargetPoint;

        // Use this for initialization
        void Start()
        {

        }

        float GetAxis(Vector3 v)
        {
            switch (Axis)
            {
                case EAxis.X: return v.x;
                case EAxis.Y: return v.y;
                case EAxis.Z: return v.z;
            }
            return 0;
        }

        Vector3 SetAxis(Vector3 v, float f)
        {
            switch (Axis)
            {
                case EAxis.X: v.x = f; break;
                case EAxis.Y: v.y = f; break;
                case EAxis.Z: v.z = f; break;
            }
            return v;
        }

        // Update is called once per frame
        void Update()
        {

            float targetP = GetAxis(TargetPoint);

            float dt = Time.deltaTime;
            float delta = targetP - currentPos;

            // Accelerate left or right if outside of deadzone
            if (Mathf.Abs(delta) > deadZone * deadZone)
            {
                slideVel += slideAccel * Mathf.Sign(delta) * dt;
            }

            // Apply friction
            slideVel -= slideVel * slideFriction * dt;

            // Apply velocity to position
            currentPos += slideVel * dt;

            // Clamp to sides (bounce)
            if (Mathf.Abs(currentPos) >= clampDistance)
            {
                slideVel *= -bounce;
                currentPos = clampDistance * Mathf.Sign(currentPos);
            }

            // Set position
            transform.localPosition = SetAxis(transform.localPosition, currentPos);
        }
    }
}
