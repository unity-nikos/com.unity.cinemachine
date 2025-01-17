using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineOrbitalFollow))]
    [CanEditMultipleObjects]
    class CinemachineOrbitalFollowEditor : UnityEditor.Editor
    {
        CinemachineOrbitalFollow Target => target as CinemachineOrbitalFollow;

        void OnEnable()
        {
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(OrbitalFollowOrbitSelection));
        }
        
        void OnDisable()
        {
            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(OrbitalFollowOrbitSelection));
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux, CmPipelineComponentInspectorUtility.RequiredTargets.Tracking);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TrackerSettings)));
            ux.AddSpace();

            var orbitModeProp = serializedObject.FindProperty(() => Target.OrbitStyle);
            ux.Add(new PropertyField(orbitModeProp));
            var m_Radius = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.Radius)));
            var m_Orbits = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.Orbits)));

            ux.AddSpace();
            this.AddInputControllerHelp(ux, "Orbital Follow has no input axis controller behaviour.");
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.HorizontalAxis)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.VerticalAxis)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.RadialAxis)));

            ux.TrackPropertyWithInitialCallback(orbitModeProp, (p) =>
            {
                var mode = (CinemachineOrbitalFollow.OrbitStyles)p.intValue;
                m_Radius.SetVisible(mode == CinemachineOrbitalFollow.OrbitStyles.Sphere);
                m_Orbits.SetVisible(mode == CinemachineOrbitalFollow.OrbitStyles.ThreeRing);
            });
            return ux;
        }

        static GUIContent[] s_OrbitNames = 
        {
            new GUIContent("Top"), 
            new GUIContent("Center"), 
            new GUIContent("Bottom")
        };
        internal static GUIContent[] orbitNames => s_OrbitNames;

        bool m_UpdateCache = true;
        float m_VerticalAxisCache;

        void OnSceneGUI()
        {
            var orbitalFollow = Target;
            if (orbitalFollow == null || !orbitalFollow.IsValid)
                return;
            
            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool))) 
            {
                switch (orbitalFollow.OrbitStyle)
                {
                    case CinemachineOrbitalFollow.OrbitStyles.Sphere:
                    {
                        EditorGUI.BeginChangeCheck();
                        var camPos = orbitalFollow.VcamState.RawPosition;
                        var camTransform = orbitalFollow.VirtualCamera.transform;
                        var camRight = camTransform.right;
                        var followPos = orbitalFollow.FollowTargetPosition;
                        var handlePos = followPos + camRight * orbitalFollow.Radius;
                        var rHandleId = GUIUtility.GetControlID(FocusType.Passive);
                        var newHandlePosition = Handles.Slider(rHandleId, handlePos, -camRight,
                            CinemachineSceneToolHelpers.CubeHandleCapSize(camPos), Handles.CubeHandleCap, 0.5f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // Modify via SerializedProperty for OnValidate to get called automatically, and scene repainting too
                            var so = new SerializedObject(orbitalFollow);
                            var prop = so.FindProperty(() => orbitalFollow.Radius);
                            prop.floatValue -= CinemachineSceneToolHelpers.SliderHandleDelta(
                                newHandlePosition, handlePos, -camRight);
                            so.ApplyModifiedProperties();
                        }

                        var orbitRadiusHandleIsDragged = GUIUtility.hotControl == rHandleId;
                        var orbitRadiusHandleIsUsedOrHovered = orbitRadiusHandleIsDragged ||
                            HandleUtility.nearestControl == rHandleId;
                        if (orbitRadiusHandleIsUsedOrHovered)
                            CinemachineSceneToolHelpers.DrawLabel(camPos,
                                "Radius (" + orbitalFollow.Radius.ToString("F1") + ")");
                            
                        Handles.color = orbitRadiusHandleIsUsedOrHovered ? 
                            Handles.selectedColor : CinemachineSceneToolHelpers.HelperLineDefaultColor;
                        Handles.DrawLine(camPos, followPos);
                        Handles.DrawWireDisc(followPos, camTransform.up, orbitalFollow.Radius);
                            
                        CinemachineSceneToolHelpers.SoloOnDrag(
                            orbitRadiusHandleIsDragged, orbitalFollow.VirtualCamera, rHandleId);

                        Handles.color = originalColor;
                        break;
                    }
                    case CinemachineOrbitalFollow.OrbitStyles.ThreeRing:
                    {
                        if (m_UpdateCache)
                            m_VerticalAxisCache = orbitalFollow.VerticalAxis.Value;
                        
                        var draggedRig = CinemachineSceneToolHelpers.ThreeOrbitRigHandle(
                            orbitalFollow.VirtualCamera, orbitalFollow.GetReferenceOrientation(),
                            new SerializedObject(orbitalFollow).FindProperty(() => orbitalFollow.Orbits));
                        m_UpdateCache = draggedRig < 0 || draggedRig > 2;
                        orbitalFollow.VerticalAxis.Value = draggedRig switch
                        {
                            0 => orbitalFollow.VerticalAxis.Range.y,
                            1 => orbitalFollow.VerticalAxis.Center,
                            2 => orbitalFollow.VerticalAxis.Range.x,
                            _ => m_VerticalAxisCache
                        };
                        break;
                    }
                    default:
                    {
                        Debug.LogError("OrbitStyle has no associated handle");
                        throw new System.ArgumentOutOfRangeException();
                    }
                }
                
            }
            Handles.color = originalColor;
        }

        // TODO: ask swap's opinion on this. Do we want to always draw this or only when follow offset handle is not selected
        // TODO: what color? when follow offset handle is selected, do we want to draw CameraPath.
        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineOrbitalFollow))]
        static void DrawOrbitalGizmos(CinemachineOrbitalFollow orbital, GizmoType selectionType)
        {
            var vcam = orbital.VirtualCamera;
            if (vcam != null && vcam.Follow != null)
            {
                var color = CinemachineCore.Instance.IsLive(vcam)
                    ? CinemachineCorePrefs.BoundaryObjectGizmoColour.Value
                    : CinemachineCorePrefs.InactiveGizmoColour.Value;
                var targetPos = orbital.FollowTargetPosition;
                var orient = orbital.GetReferenceOrientation();
                var up = orient * Vector3.up;
                var rotation = orbital.HorizontalAxis.Value;
                orient = Quaternion.AngleAxis(rotation, up) * orient;

                switch (orbital.OrbitStyle)
                {
                    case CinemachineOrbitalFollow.OrbitStyles.ThreeRing:
                    {
                        var scale = orbital.RadialAxis.Value;
                        var prevColor = Handles.color;
                        Handles.color = color;
                        Handles.DrawWireDisc(
                            targetPos + up * orbital.Orbits.Top.Height * scale,
                            up, orbital.Orbits.Top.Radius * scale);
                        Handles.DrawWireDisc(
                            targetPos + up * orbital.Orbits.Center.Height * scale, 
                            up, orbital.Orbits.Center.Radius * scale);
                        Handles.DrawWireDisc(
                            targetPos + up * orbital.Orbits.Bottom.Height * scale,
                            up, orbital.Orbits.Bottom.Radius * scale);
                        Handles.color = prevColor;

                        DrawCameraPath(targetPos, orient, scale, color, orbital);

                        break;
                    }
                    case CinemachineOrbitalFollow.OrbitStyles.Sphere:
                    {
                        var fwd = targetPos - vcam.State.RawPosition;
                        var right = orient * Vector3.right;
                        up = Vector3.Cross(fwd, right);

                        var prevColor = Handles.color;
                        Handles.color = color;
                        Handles.DrawWireDisc(targetPos, up, orbital.Radius);
                        Handles.DrawWireDisc(targetPos, right, orbital.Radius);
                        Handles.color = prevColor;
                        break;
                    }
                }
            }
        }
        
        static void DrawCameraPath(
            Vector3 pos, Quaternion orient, float scale, Color color, CinemachineOrbitalFollow freelook)
        {
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(pos, orient, scale * Vector3.one);
            var prevColor = Gizmos.color;
            Gizmos.color = color;

            const float stepSize = 0.1f;
            var lastPos = freelook.GetCameraOffsetForNormalizedAxisValue(-1);
            var max = 1 + stepSize/2;
            for (float t = -1 + stepSize; t < max; t += stepSize)
            {
                var p = freelook.GetCameraOffsetForNormalizedAxisValue(t);
                Gizmos.DrawLine(lastPos, p);
                lastPos = p;
            }
            Gizmos.matrix = prevMatrix;
            Gizmos.color = prevColor;
        }
    }
}
