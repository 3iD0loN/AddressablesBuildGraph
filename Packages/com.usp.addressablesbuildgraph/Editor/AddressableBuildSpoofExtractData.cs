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

namespace USP.AddressablesBuildGraph
{
    public static partial class AddressableBuildSpoof
    {
        #region Static Methods
        public static ReturnCode GetExtractData(
            out AddressableAssetsBuildContext aaBuildContext,
            out ExtractDataTask extractDataTask)
        {
            DateTime buildStartTime = DateTime.Now;

            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                aaBuildContext = default;
                extractDataTask = default;

                // Do nothing else.
                return ReturnCode.MissingRequiredObjects;
            }

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
}