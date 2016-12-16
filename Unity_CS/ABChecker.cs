using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BuildSystem
{
    public class ABChecker
    {
        public class DepInfo
        {
            public HashSet<string> containingABs = new HashSet<string>();
            
            public int memsize;
        }

        private static void collectDep(string asset, string bundle, Dictionary<string, DepInfo> depasset2bundles,
            Dictionary<string, string> asset2bundle)
        {
            foreach (var depasset in AssetDatabase.GetDependencies(asset, false))
            {
                if (!depasset.EndsWith(".cs") && !depasset.Equals(asset) && !asset2bundle.ContainsKey(depasset))
                {
                    DepInfo res;
                    if (depasset2bundles.TryGetValue(depasset, out res))
                    {
                        res.containingABs.Add(bundle);
                    }
                    else
                    {
                        res = new DepInfo();
                        res.containingABs.Add(bundle);
                        depasset2bundles.Add(depasset, res);
                    }
                    collectDep(depasset, bundle, depasset2bundles, asset2bundle);
                }
                
            }
        }

        public static void CheckAB()
        {
            ABMarker.DoMark();
            Dictionary<string, string> asset2bundle = new Dictionary<string, string>();
            foreach (var bundle in AssetDatabase.GetAllAssetBundleNames())
            {
                foreach (var asset in AssetDatabase.GetAssetPathsFromAssetBundle(bundle))
                {
                    asset2bundle.Add(asset, bundle);
                }
            }

            Dictionary<string, DepInfo> depasset2bundles = new Dictionary<string, DepInfo>();
            foreach (var kv in asset2bundle)
            {
                var asset = kv.Key;
                var bundle = kv.Value;
                collectDep(asset, bundle, depasset2bundles, asset2bundle);
            }

            int canSaveMemSize = 0;
            foreach (var kv in depasset2bundles)
            {
                var depinfo = kv.Value;
                if (depinfo.containingABs.Count > 1)
                {
                    depinfo.memsize =
                        Profiler.GetRuntimeMemorySize(AssetDatabase.LoadAssetAtPath<Object>(kv.Key));

                    Debug.Log(kv.Key + " count=" + depinfo.containingABs.Count + ", memsize=" +
                              ToolUtils.readableSize(depinfo.memsize));
                    foreach (var containingAB in depinfo.containingABs)
                    {
                        Debug.Log("    " + containingAB);
                    }
                    canSaveMemSize += depinfo.memsize*(depinfo.containingABs.Count - 1);
                }
            }

            Debug.Log("can save mem size=" + ToolUtils.readableSize(canSaveMemSize));
        }
    }
}