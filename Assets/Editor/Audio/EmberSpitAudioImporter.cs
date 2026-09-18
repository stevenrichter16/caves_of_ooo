using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Preserve the approved Ember mix; no lossy encoding or mono normalization.</summary>
    public sealed class EmberSpitAudioImporter : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if(!assetPath.StartsWith("Assets/Resources/Audio/EmberSpit/", System.StringComparison.Ordinal)) return;
            var importer=(AudioImporter)assetImporter;
            importer.forceToMono=false;
            importer.loadInBackground=false;
            var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.PCM;
            settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData=true;
            importer.defaultSampleSettings=settings;
        }
    }
}
