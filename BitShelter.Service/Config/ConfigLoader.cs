using BitShelter.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BitShelter.Service.Config
{
  public static class ConfigLoader
  {
    public const string AppConfigFileName = "appconfig.json";
    public const string SnapshotInstancesFileName = "instances.json";
    public const string RuleFileNameRegexPattern = "^rule_([0-9]+)\\.json$";
    public const int RuleFileHistoryCount = 10;
    public static readonly Regex RuleFileNameRegex = new Regex(RuleFileNameRegexPattern);



    //
    // Load methods

    public static async Task<AppConfig> LoadAppConfig()
    {
      string appConfigFilePath = GetAppConfigPath();

      return await SafeLoadJson<AppConfig>(appConfigFilePath);
    }

    public static async Task<IEnumerable<SnapshotInstance>> LoadSnapshotInstances()
    {
      return await LoadTrustedJson<IEnumerable<SnapshotInstance>>(GetSnapshotInstancesFilePath());
    }

    // Loads a state file only if SYSTEM or Administrators own it. See AppDataSecurity.
    public static async Task<T> LoadTrustedJson<T>(string filePath)
    {
      if (filePath == null || !File.Exists(filePath) || !IsTrustedStateFile(filePath))
        return default(T);

      return await SafeLoadJson<T>(filePath);
    }

    public static async Task<Tuple<IList<SnapshotRule>, string>> LoadLatestRules()
    {
      string appData = Const.GetAppDataFolderPath();
      string latestRulesFileName = GetLatestRulesFileName(appData, IsTrustedStateFile);

      IList<SnapshotRule> rules = latestRulesFileName == null
        ? new List<SnapshotRule>()
        : (await SafeLoadJson<IEnumerable<SnapshotRule>>(Path.Combine(appData, latestRulesFileName)))?.ToList() ?? new List<SnapshotRule>();

      return new Tuple<IList<SnapshotRule>, string>(
        rules,
        latestRulesFileName
      );
    }

    public static async Task<SnapshotRule> LoadRules(string rulesFileName)
    {
      string rulesFilePath = GetRulesPath(rulesFileName);

      return await SafeLoadJson<SnapshotRule>(rulesFilePath);
    }

    public static async Task<T> SafeLoadJson<T>(string filePath)
    {
      if (filePath != null && File.Exists(filePath))
        using (var reader = File.OpenText(filePath))
          return JsonConvert.DeserializeObject<T>(await reader.ReadToEndAsync());

      return default(T);
    }

    public static void SaveToFile<T>(T obj, string fileName)
    {
      WriteJsonAtomic(GetRulesPath(fileName), obj);
    }

    // Write to a sibling temp file, flush it to disk, then swap it in. A crash mid-write leaves the
    // previous file intact instead of a truncated one that would stop the service from starting.
    public static void WriteJsonAtomic<T>(string filePath, T obj)
    {
      string tempPath = filePath + ".tmp";

      using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
      {
        using (var sw = new StreamWriter(fs, leaveOpen: true))
          sw.Write(JsonConvert.SerializeObject(obj));

        fs.Flush(flushToDisk: true);
      }

      File.Move(tempPath, filePath, overwrite: true);
    }

    public static void DeleteOldRulesFiles(string rootFolderPath, int keepCount)
    {
      foreach (string filePath in GetAllRulesFileName(rootFolderPath).Skip(keepCount))
        File.Delete(filePath);
    }



    //
    // Path computing methods

    public static string GetAppConfigPath()
    {
      string appData = Const.GetAppDataFolderPath();

      return Path.Combine(appData, AppConfigFileName);
    }

    public static string GetSnapshotInstancesFilePath()
    {
      string appData = Const.GetAppDataFolderPath();

      return Path.Combine(appData, SnapshotInstancesFileName);
    }

    public static string GetRulesPath(string rulesFileName)
    {
      string appData = Const.GetAppDataFolderPath();

      if (rulesFileName == null)
        return null;

      return Path.Combine(appData, rulesFileName);
    }

    public static string GetLatestRulesFileName(string rootFolderPath, Func<string, bool> isTrusted = null)
    {
      return GetAllRulesFileName(rootFolderPath)
        .FirstOrDefault(f => isTrusted == null || isTrusted(f));
    }

    private static bool IsTrustedStateFile(string filePath)
    {
      bool trusted = AppDataSecurity.HasTrustedOwner(filePath);

      if (!trusted)
        Log.Warning("Ignoring {FilePath}: its owner is not SYSTEM or Administrators", filePath);

      return trusted;
    }

    public static IEnumerable<string> GetAllRulesFileName(string rootFolderPath)
    {
      return Directory.GetFiles(rootFolderPath)
        .Where(f => IsRulesFileName(f))
        .OrderByDescending(f => GetRulesTimestampFromFileName(f));
    }

    public static bool IsRulesFileName(string fileName)
    {
      return !String.IsNullOrWhiteSpace(fileName)
        && RuleFileNameRegex.IsMatch(Path.GetFileName(fileName));
    }

    public static long GetRulesTimestampFromFileName(string fileName)
    {
      return String.IsNullOrWhiteSpace(fileName)
        ? -1
        : Int64.Parse(RuleFileNameRegex.Match(Path.GetFileName(fileName)).Groups[1].Value);
    }
  }
}
