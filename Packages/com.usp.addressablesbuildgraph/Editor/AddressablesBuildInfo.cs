using System.Collections.Generic;

using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Content;
using System;

namespace USP.AddressablesBuildGraph
{
    public class AddressablesBuildInfo
    {
        #region Static Methods
        public static AddressablesBuildInfo Create(AddressableAssetSettings settings)
        {
            ReturnCode exitCode = AddressableBuildSpoof.GetExtractData(settings,
                    out AddressableAssetsBuildContext aaBuildContext,
                    out ExtractDataTask extractDataTask);

            if (exitCode < ReturnCode.Success)
            {
                return null;
            }

            return Create(aaBuildContext, extractDataTask.WriteData);
        }

        public static AddressablesBuildInfo Create(AddressableAssetsBuildContext aaBuildContext, IBundleWriteData bundleWriteData)
        {
            var buildInfo = new AddressablesBuildInfo();

            using (Dictionary<string, string>.ValueCollection.Enumerator bundleNameEnumerator = bundleWriteData.FileToBundle.Values.GetEnumerator())
            using (Dictionary<string, List<ObjectIdentifier>>.ValueCollection.Enumerator objectIdentifierEnumerator = bundleWriteData.FileToObjects.Values.GetEnumerator())
            {
                // The bundle names and the object identifiers are ordered by the same archive files,
                // so they can be enumerated in tandem and treated as if they are zipped together:
                // a list of guids for assets, associated with the name of the bundle that there were built into.
                // (These asset guids represent all assets that were pulled into the build, not just the assets that were marked Addressable.)
                while (bundleNameEnumerator.MoveNext() && objectIdentifierEnumerator.MoveNext())
                {
                    // Pack the current bundle name in an info object.
                    var bundlekey = new AssetBundleInfo(bundleNameEnumerator.Current);

                    if (!buildInfo.AssetBundles.TryGetValue(bundlekey, out AssetBundleInfo bundleInfo))
                    {
                        bundleInfo = bundlekey;
                        buildInfo.AssetBundles.Add(bundleInfo);
                    }

                    // Get the current list of assets that are in associated with the bundle.
                    List<ObjectIdentifier> objectIdentifierList = objectIdentifierEnumerator.Current;

                    buildInfo.Assets.EnsureCapacity(objectIdentifierList.Count);
                    
                    // For every the object identifier in the list, perform the following:
                    foreach (ObjectIdentifier objectIdentifier in objectIdentifierList)
                    {
                        // Create a new instance of the info to act as a search key.
                        var assetKey = new AssetInfo(objectIdentifier.guid);

                        // Determine if this is already an asset that matches an asset in the
                        // unique set used to generate the build.
                        // If there is no asset that matches, then:
                        if (!buildInfo.Assets.TryGetValue(assetKey, out AssetInfo asset))
                        {
                            // Create a proper instance of the asset info.
                            asset = AssetInfo.Create(objectIdentifier.guid, bundleWriteData.AssetToFiles, aaBuildContext.Settings);

                            // Add the asset to the unique set of assets used to generate the build.
                            buildInfo.Assets.Add(asset);
                        }

                        assetKey = asset;
                        asset = null;

                        // Determine if this is already an asset that matches an asset in the
                        // unique set of assets that are packed into the bundle.
                        // If there is no asset that matches, then:
                        if (!bundleInfo.Assets.TryGetValue(assetKey, out asset))
                        {
                            asset = assetKey;

                            // Add the asset to unique set of assets that are packed into the bundle.
                            bundleInfo.Assets.Add(asset);
                        }

                        bundlekey = bundleInfo;
                        bundleInfo = null;

                        // Determine if this is already an bundle that matches a bundle in the
                        // unique set of asset bundles that the asset is packed in.
                        // If there is no asset that matches, then:
                        if (!asset.Bundles.TryGetValue(bundlekey, out bundleInfo))
                        {
                            // Cache the reference
                            bundleInfo = bundlekey;

                            // Add the bundle to the unique set of asset bundles that the asset is packed in.
                            // (Multiple bundles indicate that the asset is being duplicated).
                            asset.Bundles.Add(bundleInfo);
                        }
                    }
                }
            }

            // For every Addressables group in the list, perform the following:
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

                var groupKey = new GroupInfo(group);

                if (!buildInfo.Groups.TryGetValue(groupKey, out GroupInfo groupInfo))
                {
                    groupInfo = groupKey;

                    // Add the group to the unique set of all Addressbles groups used to generate the build.
                    buildInfo.Groups.Add(groupInfo);
                }

                // Otherwise, there was a list of asset bundle names that are associated with the group.

                // For every bundle name associated with the group, perform the following:
                foreach (string bundleName in bundleNames)
                {
                    var bundleKey = new AssetBundleInfo(bundleName);

                    if (!buildInfo.AssetBundles.TryGetValue(bundleKey, out AssetBundleInfo bundleInfo))
                    {
                        // NOTE: This shouldn't happen. Should we throw an error instead?

                        bundleInfo = bundleKey;
                        buildInfo.AssetBundles.Add(bundleInfo);
                    }

                    bundleInfo.Group = groupInfo;

                    if (!groupInfo.AssetBundles.TryGetValue(bundleInfo, out AssetBundleInfo bundleInfo2))
                    {
                        bundleInfo2 = bundleInfo;
                        groupInfo.AssetBundles.Add(bundleInfo2);
                    }
                }
            }

            AssetInfo.PopulateAssetDependencyGraph(buildInfo.Assets);
            
            return buildInfo;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets a unique set of all Addressbles groups used to generate the build.
        /// </summary>
        public HashSet<GroupInfo> Groups { get; } = new HashSet<GroupInfo>();

        /// <summary>
        /// Gets a unique set of all assets used to generate the build.
        /// </summary>
        public HashSet<AssetInfo> Assets { get; } = new HashSet<AssetInfo>();

        /// <summary>
        /// Gets a unique set of all bundles generated by the build.
        /// </summary>
        public HashSet<AssetBundleInfo> AssetBundles { get; } = new HashSet<AssetBundleInfo>();
        #endregion
    }
}