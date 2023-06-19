using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AvatarLightingTuner
{
    public class MeshProbeAnchorOverider : EditorWindow
    {
        private GameObject RootObject;
        private Transform Anchor;
        public Dictionary<SkinnedMeshRenderer, bool> SMRList = new Dictionary<SkinnedMeshRenderer, bool>();
        public Dictionary<MeshRenderer, bool> MRList = new Dictionary<MeshRenderer, bool>();
        private Vector2 ScrollPosition = Vector2.zero;

        [MenuItem("Tools/AvatarLightingTuner/MeshProbeAnchorOverider")]
        public static void ShowWindow()
        {
            var window = EditorWindow.GetWindow(typeof(MeshProbeAnchorOverider), false, "MeshProbeAnchorOverider v2.0");
        }

        void OnGUI()
        {
            //親オブジェクト
            RootObject = EditorGUILayout.ObjectField("オブジェクト/Object", RootObject, typeof(GameObject), true) as GameObject;

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

            //子要素を取得
            if (GUILayout.Button("リスト化/ListUp") && RootObject != null)
            {
                SMRList = RootObject.GetComponentsInChildren<SkinnedMeshRenderer>().ToDictionary(SMR => SMR, SMR => false);
                MRList = RootObject.GetComponentsInChildren<MeshRenderer>().ToDictionary(MR => MR, MR => false);
            }

            //描画
            if (SMRList.Any() || SMRList.Any())
            {
                EditorGUILayout.Space(20);
                var AnchorBackup = Anchor;
                Anchor = EditorGUILayout.ObjectField("アンカー/Anchor", Anchor, typeof(Transform), true) as Transform;
                if (AnchorBackup != Anchor && Anchor != null && !TransformInAvatar(RootObject, Anchor))
                    Anchor = AnchorBackup;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("全選択/Select All"))
                    {
                        SMRList = SMRList.ToDictionary(KV => KV.Key, KV => true);
                        MRList = MRList.ToDictionary(KV => KV.Key, KV => true);
                    }
                    if (GUILayout.Button("全選択解除/UnSelect All"))
                    {
                        SMRList = SMRList.ToDictionary(KV => KV.Key, KV => false);
                        MRList = MRList.ToDictionary(KV => KV.Key, KV => false);
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("メッシュレンダラー/MeshRenderer");
                    EditorGUILayout.LabelField("アンカー/Anchor");
                }
            }

            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);

            if (SMRList.Any())
            {
                EditorGUILayout.LabelField("SkinnedMeshRenderer", EditorStyles.boldLabel);
                Draw(SMRList);
                if (MRList.Any())
                    EditorGUILayout.Space(10);
            }

            if (MRList.Any())
            {
                EditorGUILayout.LabelField("MeshRenderer", EditorStyles.boldLabel);
                Draw(MRList);
            }

            EditorGUILayout.EndScrollView();

            if (SMRList.Any() || MRList.Any())
            {
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
                if (GUILayout.Button("適用/Apply"))
                {
                    Apply(RootObject, SMRList, Anchor);
                    Apply(RootObject, MRList, Anchor);
                }
            }
        }

        private bool Apply<T>(GameObject RootObject, Dictionary<T, bool> Dic, Transform AnchorTransform) where T : Renderer
        {
            if (AnchorTransform == null || TransformInAvatar(RootObject, AnchorTransform))
            {
                Dic.Where(KV => KV.Value).ToList().ForEach(KV => KV.Key.probeAnchor = AnchorTransform);
                return true;
            }
            return false;
        }

        private bool TransformInAvatar(GameObject RootObject, Transform transform)
        {
            return RootObject.GetComponentsInChildren<Transform>().Any(TRS => TRS == transform);
        }

        private void Draw<T>(Dictionary<T, bool> Dic) where T : Renderer
        {
            Dic.Keys.ToList().ForEach(Key =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Dic[Key] = EditorGUILayout.ToggleLeft("", Dic[Key], GUILayout.MaxWidth(25f));
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(Key, typeof(SkinnedMeshRenderer), true);
                    var backup = Key.probeAnchor;
                    Key.probeAnchor = EditorGUILayout.ObjectField(Key.probeAnchor, typeof(Transform), true) as Transform;
                    if (Key.probeAnchor != backup && Key.probeAnchor != null && !TransformInAvatar(RootObject, Key.probeAnchor))
                        Key.probeAnchor = backup;
                }
            });
        }
    }
}