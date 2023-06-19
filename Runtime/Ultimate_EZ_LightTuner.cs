using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AvatarLightingTuner
{
    public class Ultimate_EZ_LightTuner : EditorWindow
    {
        [MenuItem("Tools/AvatarLightingTuner/Ultimate_EZ_LightTuner")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(Ultimate_EZ_LightTuner), false, "Ultimate_EZ_LightTuner v1.1");
        }

        private List<Animator> _avatars = new List<Animator>();
        private Vector2 _scroll = Vector2.zero;

        void OnFocus()
        {
            _avatars = FindObjectsOfType<Animator>().Where(a => a.avatar.isHuman).ToList();
        }

        void OnGUI()
        {
            Animator AvatarAnimator = EditorGUILayout.ObjectField("Avatar →", null, typeof(Animator), true) as Animator;

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            EditorGUILayout.LabelField("Avatars List", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh"))
                OnFocus();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            _avatars.ForEach(a =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(a.name))
                        AvatarAnimator = a;
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(a, typeof(Animator), true);
                }
            });

            EditorGUILayout.EndScrollView();

            if (AvatarAnimator == null) return;
            if (!AvatarAnimator.avatar.isHuman)
            {
                AvatarAnimator = null;
                EditorUtility.DisplayDialog("Ultimate_EZ_LightTuner", "Humanoidアバターじゃないですね", "OK");
                return;
            };
            Transform Anchor = AvatarAnimator.GetBoneTransform(HumanBodyBones.Chest);
            if (Anchor == null) AvatarAnimator.GetBoneTransform(HumanBodyBones.Spine);
            if (Anchor == null) AvatarAnimator.GetBoneTransform(HumanBodyBones.Hips);
            if (Anchor == null)
            {
                AvatarAnimator = null;
                EditorUtility.DisplayDialog("Ultimate_EZ_LightTuner", "Chest, Spine, Hips全部無いんで無理ですね", "OK");
                return;
            }
            GameObject RootObject = AvatarAnimator.gameObject;

            //アンカー設定
            RootObject.GetComponentsInChildren<SkinnedMeshRenderer>().ToList().ForEach(SMR => SMR.probeAnchor = Anchor);
            RootObject.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(MR => MR.probeAnchor = Anchor);

            //明るさ設定(処理の重複あるけどええやろ別)
            RootObject.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(MR => MR.sharedMaterials).Where(M => M != null)
                .ToList().ForEach(M => SetLighting(M));
            RootObject.GetComponentsInChildren<MeshRenderer>().SelectMany(MR => MR.sharedMaterials).Where(M => M != null)
                .ToList().ForEach(M => SetLighting(M));

            EditorUtility.DisplayDialog("Ultimate_EZ_LightTuner", "完了しました\n(設定したアンカー:" + Anchor.gameObject.name + ")", "OK");
            AvatarAnimator = null;
        }

        private void SetLighting(Material M)
        {
            //For LilToon
            if (M.HasProperty("_LightMinLimit")) M.SetFloat("_LightMinLimit", 0.05f);
            if (M.HasProperty("_LightMaxLimit")) M.SetFloat("_LightMaxLimit", 1f);
            if (M.HasProperty("_AsUnlit")) M.SetFloat("_AsUnlit", 0f);
            if (M.HasProperty("_VertexLightStrength")) M.SetFloat("_VertexLightStrength", 0f);
            if (M.HasProperty("_BeforeExposureLimit")) M.SetFloat("_BeforeExposureLimit", 10000f);
            if (M.HasProperty("_MonochromeLighting")) M.SetFloat("_MonochromeLighting", 0f);
            if (M.HasProperty("_lilDirectionalLightStrength")) M.SetFloat("_lilDirectionalLightStrength", 1f);

            //For UTS
            if (M.HasProperty("_Unlit_Intensity")) M.SetFloat("_Unlit_Intensity", 1f);
        }
    }
}