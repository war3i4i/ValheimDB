using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AsyncModLoader;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.Utils;
using ServerSync;
using ValheimDB.DataTypes;
using YamlDotNet.Serialization;

namespace ValheimDB
{
    [BepInPlugin(GUID, Name, VERSION)]
    [BepInDependency("kg.AsyncModLoader", BepInDependency.DependencyFlags.HardDependency)]
    public class ValheimDB : BaseUnityPlugin
    {  
        internal const string GUID = "kg.ValheimDB";
        internal const string Name = "ValheimDB";
        internal const string VERSION = "1.0.0";
        private static ServerSync.ConfigSync configSync = new(GUID) { DisplayName = Name, CurrentVersion = VERSION, MinimumRequiredVersion = VERSION, IsLocked = true};
        public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(GUID);
        private static string ItemsDirectory;
        private static string PiecesDirectory;
        private static string MonstersDirectory;  
        public static CustomSyncedValue<Dictionary<string, ItemInfoWrapper.ItemInfo>> ItemInfos = new(configSync, "ValheimDB_ItemInfos", []);
        public static CustomSyncedValue<Dictionary<string, PieceInfoWrapper.PieceInfo>> PieceInfos = new(configSync, "ValheimDB_PieceInfos", []);
        public static CustomSyncedValue<Dictionary<string, MonsterInfoWrapper.MonsterInfo>> MonsterInfos = new(configSync, "ValheimDB_MonsterInfos", []);
        private static Harmony Harmony = new(GUID);
        private void Awake() => StartCoroutine(AsyncAwake());
        private IEnumerator AsyncAwake()
        { 
            this.AsyncModLoaderInit();
            Harmony.PatchAll(); 
            CreateFoldersAndFsw();
            yield return ReloadConfigs(ItemsDirectory, ItemInfos);
            yield return ReloadConfigs(PiecesDirectory, PieceInfos);
            yield return ReloadConfigs(MonstersDirectory, MonsterInfos);
            ItemInfos.ValueChanged += ObjectModifier.ApplyItems;
            PieceInfos.ValueChanged += ObjectModifier.ApplyPieces;
            MonsterInfos.ValueChanged += ObjectModifier.ApplyMonsters;
            this.AsyncModLoaderDone();
            yield return null;
            Harmony.Patch(AccessTools.Method(typeof(ZNetScene), nameof(ZNetScene.Awake)), postfix: new HarmonyMethod(AccessTools.Method(typeof(ObjectModifier), nameof(ObjectModifier.ZNS_Awake_Postfix))));
        }
        private void CreateFoldersAndFsw()
        {  
            string mainDir = Path.Combine(BepInEx.Paths.ConfigPath, "ValheimDB");
            if (!Directory.Exists(mainDir)) Directory.CreateDirectory(mainDir);
            ItemsDirectory = Path.Combine(mainDir, "Items");
            if (!Directory.Exists(ItemsDirectory)) Directory.CreateDirectory(ItemsDirectory);
            PiecesDirectory = Path.Combine(mainDir, "Pieces");
            if (!Directory.Exists(PiecesDirectory)) Directory.CreateDirectory(PiecesDirectory);
            MonstersDirectory = Path.Combine(mainDir, "Monsters");
            if (!Directory.Exists(MonstersDirectory)) Directory.CreateDirectory(MonstersDirectory);
            FileSystemWatcher itemFsw = new FileSystemWatcher(ItemsDirectory, "*.y*ml") { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite, EnableRaisingEvents = true, SynchronizingObject = ThreadingHelper.SynchronizingObject };
            itemFsw.Changed += (s, e) => StartCoroutine(ReloadConfigs(ItemsDirectory, ItemInfos));
            FileSystemWatcher pieceFsw = new FileSystemWatcher(PiecesDirectory, "*.y*ml") { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite, EnableRaisingEvents = true, SynchronizingObject = ThreadingHelper.SynchronizingObject };
            pieceFsw.Changed += (s, e) => StartCoroutine(ReloadConfigs(PiecesDirectory, PieceInfos));
            FileSystemWatcher monsterFsw = new FileSystemWatcher(MonstersDirectory, "*.y*ml") { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite, EnableRaisingEvents = true, SynchronizingObject = ThreadingHelper.SynchronizingObject };
            monsterFsw.Changed += (s, e) => StartCoroutine(ReloadConfigs(MonstersDirectory, MonsterInfos));
        }
        private IEnumerator ReloadConfigs<T>(string directoryPath, CustomSyncedValue<Dictionary<string, T>> targetConfig)
        {
            Task<Dictionary<string, T>> configLoad = Task<Dictionary<string, T>>.Run(() => ProcessConfigDirectory<T>(directoryPath));
            while (!configLoad.IsCompleted) yield return null;
            targetConfig.Value = configLoad.Result;
            Logger.LogInfo($"Loaded {targetConfig.Value.Count} {typeof(T).Name} configs.");
        }
        private static readonly object ConfigLock = new();
        private static Dictionary<string, T> ProcessConfigDirectory<T>(string directoryPath)
        { 
            bool TryDeserialize<T>(IDeserializer d, TextReader reader, out T value, string filePath = null)
            {
                try { value = d.Deserialize<T>(reader); return value != null; }
                catch (Exception ex)
                {
                    value = default;
                    if (!string.IsNullOrEmpty(filePath)) Logger.LogWarning($"Failed to parse {Path.GetFileName(filePath)}");
                    return false;
                }
            }
            Dictionary<string, T> result = [];
            string[] files = Directory.GetFiles(directoryPath, "*.yml", SearchOption.AllDirectories).Concat(Directory.GetFiles(directoryPath, "*.yaml", SearchOption.AllDirectories)).ToArray();
            if (files.Length == 0) return result;
            int degree = Math.Max(1, Environment.ProcessorCount / 2);
            int chunkSize = (int)Math.Ceiling(files.Length / (double)degree);
            List<(int Start, int End)> partitions = Enumerable.Range(0, degree).Select(i => (Start: i * chunkSize, End: Math.Min((i + 1) * chunkSize, files.Length))).Where(r => r.Start < r.End).ToList();  
            using ThreadLocal<IDeserializer> threadLocalDeserializer = new ThreadLocal<IDeserializer>(() => new DeserializerBuilder().IgnoreUnmatchedProperties().Build(), true);
            Parallel.ForEach(partitions, new ParallelOptions { MaxDegreeOfParallelism = degree }, range =>
            {
                IDeserializer deserializer = threadLocalDeserializer.Value;
                Dictionary<string, T> localResult = []; 
                for (int i = range.Start; i < range.End; ++i)
                {
                    string filePath = files[i];
                    using StreamReader reader = File.OpenText(filePath);
                    if (TryDeserialize(deserializer, reader, out Dictionary<string, T> dict, null) && dict.Count > 0)
                    { 
                        localResult.AddRangeForced(dict);
                        continue;  
                    }
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    reader.DiscardBufferedData();
                    if (TryDeserialize(deserializer, reader, out T single, filePath))
                    {
                        localResult[Path.GetFileNameWithoutExtension(filePath)] = single;
                        continue;
                    }
                } 
                if (localResult.Count == 0) return; 
                lock (ConfigLock) { result.AddRangeForced(localResult); }
            });
            return result;
        }
    }
}