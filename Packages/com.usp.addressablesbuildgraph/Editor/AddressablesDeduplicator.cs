using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Content;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using UnityEngine.AddressableAssets;
using UnityEditor.VersionControl;
using UnityEditor.Build.Utilities;
using System.Runtime.Remoting.Contexts;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework;
using static UnityEditor.AddressableAssets.Build.Layout.BuildLayout;
using UnityEngine.Assertions;
using static Codice.Client.BaseCommands.BranchExplorer.ExplorerData.BrExTreeBuilder.BrExFilter;

namespace USP.AddressablesBuildGraph
{
    public class AddressablesDeduplicator
    {
        #region Static Methods
        [MenuItem("Tools/Addressables Deduplication")]
        private static void Run()
        {
            ReturnCode exitCode = AddressableBuildSpoof.GetExtractData(
                out AddressableAssetsBuildContext aaBuildContext,
                out ExtractDataTask extractDataTask);

            if (exitCode < ReturnCode.Success)
            {
                return;
            }

            //Dictionary<GUID, List<string>> implicitGuidToFilesMap = GetImplicitGuidToFilesMap(extractDataTask.WriteData);

            var groupsToBundlesToAssets = new Dictionary<AddressableAssetGroup, List<Dictionary<AssetBundleInfo, List<AssetInfo>>>>();

            // An asset is unique, but might be brought into multiple bundles (creating duplicates). A bundle is generated from one group.
            var assetsToBundlesToGroups = new Dictionary<AssetInfo, HashSet<Tuple<AssetBundleInfo, AddressableAssetGroup>>>();

            X(aaBuildContext, extractDataTask.WriteData, ref groupsToBundlesToAssets, ref assetsToBundlesToGroups);

            var x = from pair in assetsToBundlesToGroups where !pair.Key.IsAddressable && pair.Value.Count > 1 select pair;

            Debug.Log("x");
        }

        private static void X(AddressableAssetsBuildContext aaBuildContext,
            IBundleWriteData bundleWriteData,
            ref Dictionary<AddressableAssetGroup, List<Dictionary<AssetBundleInfo, List<AssetInfo>>>> groupsToBundlesToAssets,
            ref Dictionary<AssetInfo, HashSet<Tuple<AssetBundleInfo, AddressableAssetGroup>>> assetsToBundlesToGroups)
        {
            #region
            // Create a data map of a list of asset bundles that are associated with the assets.
            var assetToBundles = new Dictionary<AssetInfo, List<AssetBundleInfo>>();
            // Create a data map of a list of assets that are associated with an asset bundle.
            var bundleToAssets = new Dictionary<AssetBundleInfo, List<AssetInfo>>();

            using (Dictionary<string, string>.ValueCollection.Enumerator bundleNameEnumerator = bundleWriteData.FileToBundle.Values.GetEnumerator())
            using (Dictionary<string, List<ObjectIdentifier>>.ValueCollection.Enumerator objectIdentifierEnumerator = bundleWriteData.FileToObjects.Values.GetEnumerator())
            //using (Dictionary<string, string>.KeyCollection.Enumerator archiveFileEnumerator = bundleWriteData.FileToBundle.Keys.GetEnumerator())
            {
                while (bundleNameEnumerator.MoveNext() && objectIdentifierEnumerator.MoveNext() /*&& archiveFileEnumerator.MoveNext()*/)
                {
                    // The archive file that is associated with the bundle name.
                    //string archiveFile = archiveFileEnumerator.Current;

                    var bundleInfo = new AssetBundleInfo(bundleNameEnumerator.Current);

                    List<ObjectIdentifier> objectIdentifierList = objectIdentifierEnumerator.Current;

                    ////var assets = new List<AssetInfo>(objectIdentifierList.Count);

                    // There is a list of object identifiers that is associated with each archive file.
                    // These objects identifiers represent all assets that were pulled into the build, not just the assets that were marked Addressable. 
                    // For every the object identifier in the list, perform the following:
                    foreach (ObjectIdentifier objectIdentifier in objectIdentifierList)
                    {
                        AssetInfo asset = AssetInfo.Create(objectIdentifier.guid, bundleWriteData.AssetToFiles, aaBuildContext.Settings);

                        #region
                        if (!bundleToAssets.TryGetValue(bundleInfo, out List<AssetInfo> assets))
                        {
                            assets = new List<AssetInfo>(objectIdentifierList.Count);
                        }

                        assets.Add(asset);
                        #endregion

                        #region
                        // If the guid of the valid implicit guid is not already associated with a list of guids, then:
                        if (!assetToBundles.TryGetValue(asset, out List<AssetBundleInfo> bundleList))
                        {
                            // Create a new list of bundles
                            bundleList = new List<AssetBundleInfo>();

                            // Associate the list of bundles with the same implicit asset.
                            assetToBundles.Add(asset, bundleList);
                        }

                        // There exists a list of bundles that is associated with the same implicit asset's guid.

                        // Add the bundles to the list.
                        bundleList.Add(bundleInfo);
                        #endregion

                        // Alternatively, we can check if the asset guid has an entry in the Addressables settings.
                        //var assetEntry = aaBuildContext.Settings.FindAssetEntry(assetGuid.ToString());

                        /*/
                        // Is the archive file that is associated with the bundle name also associated with the asset guid?
                        bool archiveFoundWithBundles = archiveFileList == null ? false : archiveFileList.Any(x => string.Compare(x, archiveFile, StringComparison.Ordinal) == 0);

                        var x = guidFoundWithArchive ? "Archives Found with Guid" : "Not Found";
                        var y = archiveFoundWithBundles ? "Archives Found in List" : "Not Found";
                        var z = (assetEntry != null) ? "Addressable asset" : "Not Addressable";

                        string content = $"bundle name: {bundleName}, guid: {assetGuid}, asset file: {AssetDatabase.GUIDToAssetPath(assetGuid)}, archive file: {archiveFile},";

                        Action<object> logger = Debug.Log;
                        if (guidFoundWithArchive && archiveFoundWithBundles && assetEntry != null)
                        {
                            logger = Debug.Log;
                        }
                        else if (!guidFoundWithArchive && archiveFoundWithBundles && assetEntry != null)
                        {
                            logger = Debug.Log;
                        }
                        else if (guidFoundWithArchive && !archiveFoundWithBundles && assetEntry != null)
                        {
                            logger = Debug.Log;
                        }
                        else if (!guidFoundWithArchive && !archiveFoundWithBundles && assetEntry != null)
                        {
                            logger = Debug.LogError;
                        }
                        else if(guidFoundWithArchive && archiveFoundWithBundles && assetEntry == null)
                        {
                            logger = Debug.Log;
                        }
                        else if (!guidFoundWithArchive && archiveFoundWithBundles && assetEntry == null)
                        {
                            logger = Debug.Log;
                        }
                        else if (guidFoundWithArchive && !archiveFoundWithBundles && assetEntry == null)
                        {
                            logger = Debug.Log;
                        }
                        else if (!guidFoundWithArchive && !archiveFoundWithBundles && assetEntry == null)
                        {
                            logger = Debug.LogWarning;
                        }

                        Debug.LogWarning($"[{x}, {y}, {z}]");

                        //logger = guidFoundWithArchive && archiveFoundWithBundles ? (assetEntry == null ? Debug.LogError : Debug.Log) : Debug.LogWarning;

                        logger($"Asset [{x}, {y}, {z}] - {content}");
                        //*/
                    }

                    ////bundleToAssetsMap.Add(bundleInfo, assets);
                }
            }
            #endregion

            #region
            // Create a data map of a list of assets associated with an asset bundle, which is in turn associated with the Addressable group.
            groupsToBundlesToAssets = new Dictionary<AddressableAssetGroup, List<Dictionary<AssetBundleInfo, List<AssetInfo>>>>();
            //assetsToBundlesToGroups = new Dictionary<AssetInfo, List<Tuple<AssetBundleInfo, AddressableAssetGroup>>>();

            foreach (AddressableAssetGroup group in aaBuildContext.Settings.groups)
            {
                // Attempt to find the list of asset bundle names that are associated with the group.
                bool found = aaBuildContext.assetGroupToBundles.TryGetValue(group, out List<string> bundleNames);

                // If there was no list of asset bundle names associated with the group, then:
                if (!found)
                {
                    // Move on to the next group in the collection.
                    continue;
                }

                // Otherwise, there was a list of asset bundle names that are associated with the group.

                ///var bundlesToAssets = new List<Dictionary<AssetBundleInfo, List<AssetInfo>>>(bundleNames.Count);

                foreach (string bundleName in bundleNames)
                {
                    var bundleInfo = new AssetBundleInfo(bundleName);

                    List<AssetInfo> assets;
                    if (!bundleToAssets.TryGetValue(bundleInfo, out assets))
                    {
                        // TODO: Shouldn't happen. throw exception instead?
                        assets = new List<AssetInfo>();
                    }

                    ////var bundleToAssets = new Dictionary<AssetBundleInfo, List<AssetInfo>>();
                    ////bundleToAssets.Add(bundleInfo, assets);
                    ///bundlesToAssets.Add(bundleToAssets);

                    if (!groupsToBundlesToAssets.TryGetValue(group, out List<Dictionary<AssetBundleInfo, List<AssetInfo>>> bundlesToAssets))
                    {
                        bundlesToAssets = new List<Dictionary<AssetBundleInfo, List<AssetInfo>>>(bundleNames.Count);
                        groupsToBundlesToAssets.Add(group, bundlesToAssets);
                    }

                    var x = new Dictionary<AssetBundleInfo, List<AssetInfo>>();
                    bundlesToAssets.Add(x);

                    if (!x.TryGetValue(bundleInfo, out List<AssetInfo> assetList))
                    {
                        assetList = assets;
                    }

                    x.Add(bundleInfo, assetList);
                }

                ///result.Add(group, bundlesToAssets);
            }
            #endregion
            //*/

            AssetInfo.PopulateAssetDependencyGraph(assetToBundles.Keys);
        }

        /*/
        #region GetImplicitGuidToFilesMap
        private static Dictionary<GUID, List<string>> GetImplicitGuidToFilesMap(IBundleWriteData bundleWriteData)
        {
            // Collect valid implicit assets, which are a data map of archive files associated with an object id for an implicit object.
            IEnumerable<KeyValuePair<ObjectIdentifier, string>> validImplicitObjectIdentifiersToArchiveFile =
                // For every pair of game object identifiers that are associated with an archive file,
                from archiveFileToObjectIdentifier in bundleWriteData.FileToObjects
                    // get the list of object identifiers associated with the archive file,
                from objectIdentifier in archiveFileToObjectIdentifier.Value
                    // And only get the object identifiers in the list
                    // if their asset guid is not directly associated with any archive files that were used to generate the build,
                    // which is a asset that was implicitly added to the build.
                where !bundleWriteData.AssetToFiles.Keys.Contains(objectIdentifier.guid)
                // Then, transform the data so that the archive file associated with an object identifier for an implicit asset.
                select new KeyValuePair<ObjectIdentifier, string>(objectIdentifier, archiveFileToObjectIdentifier.Key);

            // Create a data map of a list of file paths that are associated with the implicit asset's guid.
            var implicitAssetGuidsToArchiveFile = new Dictionary<GUID, List<string>>();

            // For every valid implicit asset, perform the following:
            foreach (var pair in validImplicitObjectIdentifiersToArchiveFile)
            {
                // Build our Dictionary from our list of valid implicit guids (guids not already in explicit guids)

                // If the guid of the valid implicit guid is not already associated with a list of guids, then:
                if (!implicitAssetGuidsToArchiveFile.ContainsKey(pair.Key.guid))
                {
                    // Create a new list of file paths that are associated with the same implicit asset.
                    implicitAssetGuidsToArchiveFile.Add(pair.Key.guid, new List<string>());
                }

                // There exists a list of file paths that are associated with the same implicit asset's guid.

                // Get the list of file paths that are associated with the implicit asset's guid.
                // Add the file path to the list to associate it with the implicit asset.
                implicitAssetGuidsToArchiveFile[pair.Key.guid].Add(pair.Value);
            }

            // Return the result.
            return implicitAssetGuidsToArchiveFile;
        }
        #endregion
        //*/

        /*/
        #region CalculateDuplicates
        #region Types
        struct DuplicateResult
        {
            #region Fields
            /// <summary>
            /// The runtime instance of an Addressable group. 
            /// </summary>
            public AddressableAssetGroup Group;

            /// <summary>
            /// The file path of the asset bundle.
            /// </summary>
            public string DuplicatedFile;

            /// <summary>
            /// The file path of the asset that was implicitly pulled into addressables and duplicated in multiple bundles.
            /// </summary>
            public string AssetPath;

            /// <summary>
            /// The guid of the asset.
            /// </summary>
            public GUID DuplicatedGroupGuid;
            #endregion
        }
        #endregion

        private IEnumerable<DuplicateResult> CalculateDuplicates(
            Dictionary<GUID, List<string>> implicitAssetGuidsToArchiveFiles,
            AddressableAssetsBuildContext aaContext,
            IBundleWriteData bundleWriteData,
            ref Dictionary<List<string>, List<string>> bundleFilepathsToDuplicateImplicitAssetFilepaths)
        {
            // Clear the data map of a list of asset file paths associated with the asset bundle file paths that they are implicitly duplicated in.
            bundleFilepathsToDuplicateImplicitAssetFilepaths.Clear();

            // Collect all assets that have more than one bundle referencing them.
            IEnumerable<KeyValuePair<GUID, List<string>>> validDuplicateImplicitAssetsGuidToBundles =
                // For every pair of a list of asset bundle file paths associated with the implicit asset's guid,
                from pair in implicitAssetGuidsToArchiveFiles
                // only get the pairs that have more than one unique file path in the list. 
                where pair.Value.Distinct().Count() > 1
                // and only keep the pairs where the the implicit asset's file path is a valid path,
                where IsValidPath(AssetDatabase.GUIDToAssetPath(pair.Key.ToString()))
                // The remaining pair of a list of asset bundle file paths that are associated with the implicit asset's guid
                // represent implicit assets that are duplicated in more than one bundle.
                select pair;

            // Key = a set of bundle parents
            // Value = asset paths that share the same bundle parents
            // e.g. <{"bundle1", "bundle2"} , {"Assets/Sword_D.tif", "Assets/Sword_N.tif"}>

            foreach (var pair in validDuplicateImplicitAssetsGuidToBundles)
            {
                // Get the guid of the asset that is duplicated and implicit.
                // Get the file path of the asset in this project from the GUID. 
                string assetPath = AssetDatabase.GUIDToAssetPath(pair.Key.ToString());

                // Get the list of bundle files that the duplicated implicit asset exists in.
                List<string> bundleFilePaths = pair.Value;

                var uniqueBundleFilePaths = new List<string>();
                foreach (var bundleFilePath in bundleFilePaths)
                {
                    // If the item is already contained in the list, then:
                    if (uniqueBundleFilePaths.Contains(bundleFilePath))
                    {
                        // Do nothing with this item. Move onto the next item.
                        continue;
                    }

                    // Otherwise, the item is not yet contained in the list.

                    // Add the bundle file path to the list.
                    uniqueBundleFilePaths.Add(bundleFilePath);
                }

                // The bundle file paths are now unique.
                bundleFilePaths = uniqueBundleFilePaths;

                // A value indicating whether the a unique set of bundles have this asset implicitly duplicated in them. 
                bool found = false;

                // For every , perform the following:
                // NOTE: there must be a better way to add this; right now it may end up being exhaustive,
                // and the complexity in practice is saved by the break statement
                foreach (var key in bundleFilepathsToDuplicateImplicitAssetFilepaths.Keys)
                {
                    // If this set of bundle parents equals our set of bundle parents, then:

                    // Compare the items in the list of bundle files that the duplicated implicit asset exists in with the key
                    if (Enumerable.SequenceEqual(key, bundleFilePaths))
                    {
                        // Add the file path of the asset in this project to the set of asset bundles that it is implicitly duplicated in.
                        bundleFilepathsToDuplicateImplicitAssetFilepaths[key].Add(assetPath);

                        // The bucket is found.
                        found = true;

                        // At least one is found.
                        break;
                    }
                }
                // If a unique set of bundles fo not have this asset implicitly duplicated in them, then:
                if (!found)
                {
                    // Add a new entry with the asset path as the first item.
                    bundleFilepathsToDuplicateImplicitAssetFilepaths.Add(bundleFilePaths, new List<string>() { assetPath });
                }
            }

            IEnumerable<DuplicateResult> result =
                // For every pair of a list of asset bundle file paths associated with the implicit asset's guid,
                from assetsGuidToBundles in validDuplicateImplicitAssetsGuidToBundles

                // For every asset bundle file path in the list, perform the following:
                from bundleFilePath in assetsGuidToBundles.Value

                // get the bundle name that is associated with the bundle file path,
                let bundleName = bundleWriteData.FileToBundle[bundleFilePath]

                // get the asset group guid associated with the bundle name, which is the group that the bundle originated from.
                let groupGuid = aaContext.bundleToAssetGroup[bundleName]

                // get the asset groups instance associated with the group guid.
                let selectedGroup = aaContext.Settings.FindGroup(findGroup => findGroup != null && findGroup.Guid == groupGuid)

                // and finally, create a new instance of duplicate result.
                select new DuplicateResult
                {
                    Group = selectedGroup,
                    DuplicatedFile = bundleFilePath,
                    AssetPath = AssetDatabase.GUIDToAssetPath(assetsGuidToBundles.Key.ToString()),
                    DuplicatedGroupGuid = assetsGuidToBundles.Key,
                };

            // Return the results.
            return result;
        }

        private static bool IsValidPath(string path)
        {
            return IsPathValidForEntry(path) &&
                !path.ToLower().Contains("/resources/") &&
                !path.ToLower().StartsWith("resources/");
        }

        private static HashSet<string> excludedExtensions = new HashSet<string>(new string[] { ".cs", ".js", ".boo", ".exe", ".dll", ".meta" });

        private static bool IsPathValidForEntry(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (!path.StartsWith("assets", StringComparison.OrdinalIgnoreCase) && !IsPathValidPackageAsset(path))
                return false;

            if (path == CommonStrings.UnityEditorResourcePath ||
                path == CommonStrings.UnityDefaultResourcePath ||
                path == CommonStrings.UnityBuiltInExtraPath)
                return false;

            if (path.EndsWith("/Editor") || path.Contains("/Editor/"))
                return false;

            if (path == "Assets")
                return false;

            var settings = AddressableAssetSettingsDefaultObject.SettingsExists ? AddressableAssetSettingsDefaultObject.Settings : null;
            if (settings != null && path.StartsWith(settings.ConfigFolder) || path.StartsWith(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder))
                return false;

            return !excludedExtensions.Contains(Path.GetExtension(path));
        }

        private static bool IsPathValidPackageAsset(string path)
        {
            string convertPath = path.ToLower().Replace("\\", "/");
            string[] splitPath = convertPath.Split('/');

            if (splitPath.Length < 3)
                return false;

            if (splitPath[0] != "packages")
                return false;

            if (splitPath[2] == "package.json")
                return false;

            return true;
        }
        #endregion
        //*/

        /*/
        #region BuildImplicitDuplicatedAssetsSet
        private void BuildImplicitDuplicatedAssetsSet(IEnumerable<DuplicateResult> dupeResults)
        {
            // For every duplicate asset in the list, perform the following:
            foreach (var dupeResult in dupeResults)
            {
                // Get the name of the Addressable group.
                // Attempt to retrieve the group data associated with the group name.
                // If group data is associated with the group name, then:
                if (!m_AllIssues.TryGetValue(dupeResult.Group.Name, out Dictionary<string, List<string>> groupData))
                {
                    // Add the data to the AllIssues container which is shown in the Analyze window

                    // Create a new instance of the group data and cache it.
                    groupData = new Dictionary<string, List<string>>();

                    // Associate the group data with the group name.
                    m_AllIssues.Add(dupeResult.Group.Name, groupData);
                }

                // There exists a group data associated with the group name.

                // Get the asset bundle name associated with the bundle file path.
                var bundleName = m_ExtractData.WriteData.FileToBundle[dupeResult.DuplicatedFile];

                // Attempt to retrieve the list of assets associated with the bundle name.
                // If there is no list of asset file paths associated with the bundle name, then:
                if (!groupData.TryGetValue(bundleName, out List<string> assetFilePaths))
                {
                    // Create a new list of asset file paths.
                    assetFilePaths = new List<string>();

                    // Associate the list of asset file paths with the bundle name.
                    groupData.Add(bundleName, assetFilePaths);
                }

                // There exists a list of asset file paths associated with the bundle name.

                // Add the asset file path to the list of file paths associated with the bundle name.  
                assetFilePaths.Add(dupeResult.AssetPath);
            }
        }
        #endregion
        //*/

        #endregion
    }
}