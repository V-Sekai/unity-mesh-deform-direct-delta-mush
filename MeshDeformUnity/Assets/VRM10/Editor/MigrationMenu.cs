using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniVRM10
{
    public static class MigrationMenu
    {
        static VRMShaders.PathObject s_lastPath = VRMShaders.PathObject.UnityAssets;

        const string CONTEXT_MENU = "Assets/Migration: Vrm1";

        [MenuItem(CONTEXT_MENU, true)]
        static bool Enable()
        {
            if (!VRMShaders.PathObject.TryGetFromAsset(Selection.activeObject, out VRMShaders.PathObject path))
            {
                return false;
            }
            var isVrm = path.Extension.ToLower() == ".vrm";
            return isVrm;
        }

        [MenuItem(CONTEXT_MENU, false)]
        static void Exec()
        {
            if (!VRMShaders.PathObject.TryGetFromAsset(Selection.activeObject, out VRMShaders.PathObject path))
            {
                return;
            }

            // migrate
            var vrm1Bytes = MigrationVrm.Migrate(path.ReadAllBytes());

            if (!s_lastPath.TrySaveDialog("Save vrm1 file", $"{path.Stem}_vrm1", out VRMShaders.PathObject dst))
            {
                return;
            }
            s_lastPath = dst.Parent;

            // write result
            dst.WriteAllBytes(vrm1Bytes);

            if (dst.IsUnderAsset)
            {
                // immediately import for GUI update
                Debug.Log($"import: {dst}");
                dst.ImportAsset();
            }
            else
            {
                Debug.Log($"write: {dst}");
            }
        }
    }
}
