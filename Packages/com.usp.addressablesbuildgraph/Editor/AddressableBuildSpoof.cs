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

public static partial class AddressableBuildSpoof
{
    // NOTE: can bundleInputDefinitions and bundleNameToGroupGuid be rolled into one Dictionary<AssetBundleBuild, string>
    private static void PopulateFromPackingPreparation(AddressableAssetSettings settings,
        ref List<AssetBundleBuild> bundleInputDefinitions,
        ref Dictionary<string, string> bundleNameToGroupGuid,
        ref List<AddressableAssetEntry> assetEntries,
        Action<int> groupProcessed = null)
    {
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

}

public static partial class AddressableBuildSpoof
{
    #region GetExtractData
    public static ReturnCode GetExtractData(
        out AddressableAssetsBuildContext aaBuildContext, 
        out ExtractDataTask extractDataTask)
    {
        DateTime buildStartTime = DateTime.Now;

        var settings = AddressableAssetSettingsDefaultObject.Settings;

        var bundleInputDefinitions = new List<AssetBundleBuild>();
        var bundleNameToGroupGuid = new Dictionary<string, string>();
        var assetEntries = new List<AddressableAssetEntry>();

        AddressableBuildSpoof.PopulateFromPackingPreparation(settings,
            ref bundleInputDefinitions,
            ref bundleNameToGroupGuid,
            ref assetEntries);

        // If there are no asset bundle input definitions in the collection, then:
        if (bundleInputDefinitions.Count <= 0)
        {
            aaBuildContext = default;
            extractDataTask = default;

            // Do nothing else.
            return ReturnCode.MissingRequiredObjects;
        }

        // Otherwise, there were asset bundle input definitions generated.

        // Get the build context needed to continue the process.
        aaBuildContext = GetBuildContext(settings,
            bundleNameToGroupGuid,
            assetEntries,
            buildStartTime);

        // Refresh the build using the build context that was generated.
        ReturnCode exitCode = RefreshBuild(aaBuildContext,
            bundleInputDefinitions,
            out extractDataTask);

        var span = DateTime.Now - aaBuildContext.buildStartTime;

        string message = $"Analyze build: {exitCode}, Total Seconds: {span.TotalSeconds}";

        // If the build was a failure, then:
        Action<object> logger = (exitCode < ReturnCode.Success) ? Debug.LogError : Debug.Log;

        logger(message);

        return exitCode;
    }

    private static AddressableAssetsBuildContext GetBuildContext(
        AddressableAssetSettings settings,
        Dictionary<string, string> bundleNameToGroupGuid,
        List<AddressableAssetEntry> assetEntries,
        DateTime buildStartTime)
    {
        // Create a new instance of the runtime configuration used to initialize the Addressables system.
        var runtimeData = new ResourceManagerRuntimeData()
        {
            // Set the only value necessary to spoof the system:
            // A value indicating whether or not the exceptions from the resource manager should be logged.
            LogResourceManagerExceptions = settings.buildSettings.LogResourceManagerExceptions,
        };

        // Create the addressables build context using the Addressables settings,
        // the map of group GUIDs associated by the asset bundle,
        // and some other stubbed values.
        var aaBuildContext = new AddressableAssetsBuildContext()
        {
            Settings = settings,
            runtimeData = runtimeData,
            bundleToAssetGroup = bundleNameToGroupGuid,
            assetEntries = assetEntries,
            buildStartTime = buildStartTime,
            locations = new List<ContentCatalogDataEntry>(),
            providerTypes = new HashSet<Type>(),
            assetGroupToBundles = new Dictionary<AddressableAssetGroup, List<string>>(),
        };

        return aaBuildContext;
    }

    private static ReturnCode RefreshBuild(AddressableAssetsBuildContext aaBuildContext,
        List<AssetBundleBuild> bundleInputDefinitions,
        out ExtractDataTask extractDataTask)
    {
        // Pack the settings associated with the context into the data builder input.
        var builderInput = new AddressablesDataBuilderInput(aaBuildContext.Settings);

        // Create an instance of the bundle parameters container,
        // which is used provide custom compression settings per bundle during a asset bundle build.
        // Uses the Addressables settings, the bundle  
        var buildParams = new AddressableAssetsBundleBuildParameters(
            aaBuildContext.Settings,
            aaBuildContext.bundleToAssetGroup,
            builderInput.Target,
            builderInput.TargetGroup,
            aaBuildContext.Settings.buildSettings.bundleBuildPath);

        // Format and compose the file name of the built-in bundle.
        string builtinBundleName = GetBuiltInBundleName(aaBuildContext);

        // Generate the tasks to perform for the build.
        IList<IBuildTask> buildTasks = RuntimeDataBuildTasks(builtinBundleName);

        extractDataTask = new ExtractDataTask();

        // Add the task to extract the data.
        buildTasks.Add(extractDataTask);

        // Use the input definitions to generate a list of assets, scenes, addresses, and bundle layouts that inform the asset bundle build.
        var buildContent = new BundleBuildContent(bundleInputDefinitions);

        // Build the asset bundles using the above parameters, content, contexts, and tasks in order to get the results of the build.
        var exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent,
            out IBundleBuildResults buildResults, buildTasks, aaBuildContext);

        return exitCode;
    }

    private static string GetBuiltInBundleName(AddressableAssetsBuildContext aaContext)
    {
        return $"{GetBuiltInBundleNamePrefix(aaContext.Settings)}{BuildScriptBase.BuiltInBundleBaseName}.bundle";
    }

    private static string GetBuiltInBundleNamePrefix(AddressableAssetSettings settings)
    {
        switch (settings.BuiltInBundleNaming)
        {
            case BuiltInBundleNaming.DefaultGroupGuid:
                return settings.DefaultGroup.Guid;
            
            case BuiltInBundleNaming.ProjectName:
                return Hash128.Compute(GetProjectName()).ToString();

            case BuiltInBundleNaming.Custom:
                return settings.BuiltInBundleCustomNaming;
        }

        return string.Empty;
    }

    private static string GetProjectName()
    {
        // Get the name of the current data path for the project.
        return new DirectoryInfo(Path.GetDirectoryName(Application.dataPath)).Name;
    }

    private static IList<IBuildTask> RuntimeDataBuildTasks(string builtinBundleName)
    {
        // Create the list of build tasks.
        var buildTasks = new List<IBuildTask>();

        //// Setup ////

        // The build tasks start with forcing a switch to the platform in focus.
        buildTasks.Add(new SwitchToBuildPlatform());

        // Then the next task is to rebuild the sprite atlas cache.
        buildTasks.Add(new RebuildSpriteAtlasCache());

        //// Player Scripts ////

        // Add the player script compilation task.
        buildTasks.Add(new BuildPlayerScripts());

        //// Dependency ////

        // Calculate scene dependencies.
        buildTasks.Add(new CalculateSceneDependencyData());

        // Calculate asset dependencies.
        buildTasks.Add(new CalculateAssetDependencyData());

        // Remove sprote resources that are not included.
        buildTasks.Add(new StripUnusedSpriteSources());

        // Create the built-in bundles
        buildTasks.Add(new CreateBuiltInBundle(builtinBundleName));

        //// Packing ////

        // Generate asset bundle packing.
        buildTasks.Add(new GenerateBundlePacking());

        // Update the bundle object layout.
        buildTasks.Add(new UpdateBundleObjectLayout());

        // Generate the bundle commands
        buildTasks.Add(new GenerateBundleCommands());

        // Generate the path mapping for sub-assets.
        buildTasks.Add(new GenerateSubAssetPathMaps());

        // Generate bundle maps.
        buildTasks.Add(new GenerateBundleMaps());

        //// Writing ////

        // Generate the list of locations.
        buildTasks.Add(new GenerateLocationListsTask());

        return buildTasks;
    }
    #endregion
}