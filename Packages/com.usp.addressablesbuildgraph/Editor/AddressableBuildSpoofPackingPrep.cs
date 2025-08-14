using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace USP.AddressablesBuildGraph
{
    public static partial class AddressableBuildSpoof
    {
        #region Static Methods
        // NOTE: can bundleInputDefinitions and bundleNameToGroupGuid be rolled into one Dictionary<AssetBundleBuild, string>
        private static void PopulateFromPackingPreparation(AddressableAssetSettings settings,
            ref List<AssetBundleBuild> bundleInputDefinitions,
            ref Dictionary<string, string> bundleNameToGroupGuid,
            ref List<AddressableAssetEntry> assetEntries,
            Action<int> groupProcessed = null)
        {
            var buildScript = settings.ActivePlayerDataBuilder as BuildScriptPackedMode;

            // For every Addressables group, perform the folowing:
            for (int groupIndex = 0; groupIndex < settings.groups.Count; ++groupIndex)
            {
                // Get the current group.
                AddressableAssetGroup group = settings.groups[groupIndex];

                PopulateFromPackingPreparation(group,
                    ref bundleInputDefinitions,
                    ref bundleNameToGroupGuid,
                    ref assetEntries);

                groupProcessed?.Invoke(groupIndex);
            }
        }

        private static bool PopulateFromPackingPreparation(AddressableAssetGroup group,
            ref List<AssetBundleBuild> bundleInputDefinitions,
            ref Dictionary<string, string> bundleNameToGroupGuid,
            ref List<AddressableAssetEntry> assetEntries)
        {
            // If there is no valid group, then:
            if (group == null)
            {
                // Skip this group and move on to the next group in the list.
                return false;
            }

            // Otherwise, the group is valid.

            // Attempt to get the BundledAssetGroupSchema associated with the Addressables group.
            var schema = group.GetSchema<BundledAssetGroupSchema>();

            // If the group has an asset bundle schema, then the group using the properties
            // of this schema to determine how asset bundles are built. 
            if (schema == null)
            {
                // Skip this group and move on to the next group in the list.
                return false;
            }

            // Create an empty list of input definitions to pass into the process to be populated.
            var bundleInputDefs = new List<AssetBundleBuild>();

            // Request that the default build script generate bundle input definitions.
            List<AddressableAssetEntry> processedEntries = BuildScriptPackedMode.PrepGroupBundlePacking(
                group, bundleInputDefs, schema);

            // Compares bundle input definitions against the map of bundle names.
            // Corrects any duplicates and updates the map of bundle names with new entries.
            EnsureUniqiueAndAdd(group, ref bundleNameToGroupGuid, ref bundleInputDefs);

            // Add the list of asset bundle input definitions that were generated from the preparation.
            bundleInputDefinitions.AddRange(bundleInputDefs);

            // Add the list of the Addressable asset entries that were generated from the preparation.
            assetEntries.AddRange(processedEntries);

            return true;
        }

        private static void EnsureUniqiueAndAdd(AddressableAssetGroup group, ref Dictionary<string, string> bundleNameToGroupGuid,
            ref List<AssetBundleBuild> bundleInputDefinitions)
        {
            // For every input definition generated, perform the following:
            for (int i = 0; i < bundleInputDefinitions.Count; i++)
            {
                // If there is a collision with a group already in the map of groups associated
                // with the asset bundle name generated, then:
                if (bundleNameToGroupGuid.ContainsKey(bundleInputDefinitions[i].assetBundleName))
                {
                    // Ensure that there is a unique bundle in the input definition.
                    bundleInputDefinitions[i] = UniqueAssetBundleBuild(bundleInputDefinitions[i], bundleNameToGroupGuid);
                }

                // Associate the group's GUID with a unique asset bundle that it generates.
                bundleNameToGroupGuid.Add(bundleInputDefinitions[i].assetBundleName, group.Guid);
            }
        }

        private static AssetBundleBuild UniqueAssetBundleBuild(AssetBundleBuild assetBundleBuild,
            Dictionary<string, string> bundleToAssetGroup)
        {
            // Replace the name with a unique name if there is a name collision in the map.
            assetBundleBuild.assetBundleName = CreateUniqueFilename(assetBundleBuild.assetBundleName, bundleToAssetGroup);

            // Return a copy of the asset bundle build.
            return assetBundleBuild;
        }

        // NOTE: consider refactoring so that for loop condition is more abstract. 
        private static string CreateUniqueFilename(string filenameWithExtension,
            IReadOnlyDictionary<string, string> map, int startIndex = 1, int maxCount = 1000)
        {
            // Define the asset bundle name for a new asset bundle build entry.
            // By default, it is the same name as the input bundle build item.
            var result = filenameWithExtension;

            // Locate the index where the file extension starts.
            int index = filenameWithExtension.LastIndexOf('.');

            if (index == -1)
            {
                index = filenameWithExtension.Length;
            }

            // While there is an Addressables group associated with the bundle,
            // and we have iterated less than the maximum iterations, perform the following:
            for (int count = startIndex; map.ContainsKey(result) && count < maxCount; count++)
            {
                // Take the name and add number of attempts to the end of the filename before the extension.
                result = filenameWithExtension.Insert(index, count.ToString());
            }

            return result;
        }
        #endregion
    }
}