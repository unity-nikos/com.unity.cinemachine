﻿using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class ActivateOnKeypress : MonoBehaviour
    {
        public KeyCode ActivationKey = KeyCode.LeftControl;
        public int PriorityBoostAmount = 10;
        public GameObject Reticle;

        Cinemachine.CinemachineVirtualCameraBase vcam;
        bool boosted = false;

        void Start()
        {
            vcam = GetComponent<Cinemachine.CinemachineVirtualCameraBase>();
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (vcam != null)
            {
                if (Input.GetKey(ActivationKey))
                {
                    if (!boosted)
                    {
                        vcam.Priority.Value += PriorityBoostAmount;
                        boosted = true;
                    }
                }
                else if (boosted)
                {
                    vcam.Priority.Value -= PriorityBoostAmount;
                    boosted = false;
                }
            }

            if (Reticle != null)
                Reticle.SetActive(boosted);
#else
        InputSystemHelper.EnableBackendsWarningMessage();
#endif
        }
    }
}