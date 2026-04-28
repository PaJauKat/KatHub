using Octokit;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KatHub.KatPlugins
{
	public class KatPluginsService
	{
		private static JarRelease? jarRelease;
		public static string CurrentKatPluginsVersion { get; private set; } = "1.0";
		public static string LatestJarVersion { get; private set; } = "1.0";
		private static readonly string gitHubRepoOwner = "PaJauKat";
		private static readonly string gitHubRepoName = "PaJau-plugins";


        public static async Task<bool> ExecuteCrackearRunelite(bool crack) 
		{
            string crackerFileName = "EthanVannInstaller.jar";
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string runeliteDir = Path.Combine(localAppData, "RuneLite");
            string configFile = Path.Combine(runeliteDir, "config.json");

            if (!Directory.Exists(runeliteDir))
			{
				throw new DirectoryNotFoundException("Runelite directory not found. Please install RuneLite first.");
			}

            if (crack)
            {
				string destinationPath = Path.Combine(runeliteDir, crackerFileName);

                var assembly = Assembly.GetExecutingAssembly();
                var names = assembly.GetManifestResourceNames();
                foreach (var name in names) Debug.WriteLine($"Recurso encontrado: {name}");

                var stream = assembly.GetManifestResourceStream(crackerFileName);

                // Si no se encuentra como recurso embebido, intenta buscarlo en el directorio de la aplicación
                if (stream == null)
				{
					string crackerFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, crackerFileName);
					if (File.Exists(crackerFilePath))
					{
                        stream = new FileStream(crackerFilePath, System.IO.FileMode.Open, FileAccess.Read);
                    }
                }

				if(stream == null)
				{
					throw new FileNotFoundException("Cracker JAR not found as embedded resource or in application directory.", crackerFileName);
                }

                using (stream)
				{
                    using var fileStream = new FileStream(destinationPath, System.IO.FileMode.Create, FileAccess.Write);
                    stream.CopyTo(fileStream);
                }
                   

                Debug.WriteLine("Cracker file copied successfully to RuneLite directory.");

                // Modificando el config.json para incluir el plugin sin perder otros datos
                if (File.Exists(configFile))
                {
                    var jsonText = File.ReadAllText(configFile);
                    var node = JsonNode.Parse(jsonText) ?? new JsonObject();

                    // Establece "mainClass"
                    node["mainClass"] = "ca.arnah.runelite.LauncherHijack";

                    // Asegura que exista "classPath" como array y añade los elementos si no están
                    JsonArray classPathArray;
                    if (node["classPath"] is JsonArray existing)
                    {
                        classPathArray = existing;
                    }
                    else
                    {
                        classPathArray = new JsonArray();
                        node["classPath"] = classPathArray;
                    }

                    bool hasCracker = classPathArray.Any(x => x?.GetValue<string>() == crackerFileName);
                    if (!hasCracker) classPathArray.Add(crackerFileName);

                    bool hasRuneliteJar = classPathArray.Any(x => x?.GetValue<string>() == "RuneLite.jar");
                    if (!hasRuneliteJar) classPathArray.Add("RuneLite.jar");

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(configFile, node.ToJsonString(options));
                    Debug.WriteLine("Config updated successfully.");
                    return true;
                }
                else
                {
                    Debug.WriteLine("Config file not found.");
                    return false;
                }
            }
            else
            {
                if (File.Exists(configFile))
                {
                    var jsonText = File.ReadAllText(configFile);
                    var node = JsonNode.Parse(jsonText) ?? new JsonObject();

                    // Establece "mainClass"
                    node["mainClass"] = "net.runelite.launcher.Launcher";

                    // Asegura que exista "classPath" como array y añade los elementos si no están
                    JsonArray classPathArray;
                    if (node["classPath"] is JsonArray existing)
                    {
                        classPathArray = existing;
                    }
                    else
                    {
                        classPathArray = new JsonArray();
                        node["classPath"] = classPathArray;
                    }

					classPathArray.RemoveAll(x => x?.GetValue<string>() == crackerFileName);

                    bool hasRuneliteJar = classPathArray.Any(x => x?.GetValue<string>() == "RuneLite.jar");
                    if (!hasRuneliteJar) classPathArray.Add("RuneLite.jar");

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(configFile, node.ToJsonString(options));
                    Debug.WriteLine("Config updated successfully.");
                    return true;
                }
                else
                {
                    Debug.WriteLine("Config file not found.");
                    return false;
                }
            }
        }

		public static async Task<bool> ExecuteInstallOrUpdateJar(IProgress<double> progress)
		{
			try
			{

                string jarDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".runelite",
                    "sideloaded-plugins"
                );
                Directory.CreateDirectory(jarDir);
                

                var client = new GitHubClient(new ProductHeaderValue("KatHub"));
				var latestRelease = await client.Repository.Release.GetLatest(gitHubRepoOwner, gitHubRepoName);
				var asset = latestRelease.Assets.FirstOrDefault(x => x?.Name?.EndsWith(".jar") == true);
				
				if (asset != null)
				{
                    var jarFileName = asset.Name!;
                    string jarPath = Path.Combine(jarDir, jarFileName);

                    using var httpClient = new HttpClient();
					var downloadUrl = asset.BrowserDownloadUrl ?? throw new Exception("Asset download URL is null");
					var response = await httpClient.GetAsync(downloadUrl);
					response.EnsureSuccessStatusCode();
					var totalBytes = response.Content.Headers.ContentLength ?? -1L;
					var downloadedBytes = 0L;
					using var contentStream = await response.Content.ReadAsStreamAsync();
					using var fileStream = new FileStream(jarPath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
					var buffer = new byte[8192];
					int bytesRead;
					while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
					{
						await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
						downloadedBytes += bytesRead;
						if (totalBytes > 0)
						{
							double progressValue = (double)downloadedBytes / totalBytes * 100;
							progress.Report(progressValue);
						}
					}
				}

                
				return true;


            }
			catch (Exception ex)
			{
				Debug.WriteLine("Error descargando el plugin: " + ex.Message);
				return false;
			}
		}

		public static async Task<bool> ExecuteUninstallJar()
		{
			try
			{
				string jarDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".runelite",
					"sideloaded-plugins"
				);
				string[] jars = Directory.GetFiles(jarDir, "Kat*.jar");
				foreach (var jar in jars)
				{
					File.Delete(jar);
					Debug.WriteLine("Eliminando: " + jar);
				}
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error eliminando el plugin: " + ex.Message);
				return false;
			}
		}


		public static async Task<KatPluginsState> CalculateKatPluginsState()
		{
			try
			{
				if (!(await IsRuneliteCracked()))
				{
					return KatPluginsState.Crackear;
				}

				LatestJarVersion = await ObtainLastVersion();
				CurrentKatPluginsVersion = ObtainInstalledVersion();
				Debug.WriteLine("Current version:" + CurrentKatPluginsVersion);
				Debug.WriteLine("Last version:" + LatestJarVersion);
				if(CurrentKatPluginsVersion == "-69")
				{
					return KatPluginsState.MultipleVersions;
				}
				if (CurrentKatPluginsVersion == "-1")
				{
					return KatPluginsState.Install;
				}
				else if (CurrentKatPluginsVersion.CompareTo(LatestJarVersion) < 0)
				{
					return KatPluginsState.Update;
				}
				else
				{
					return KatPluginsState.UpToDate;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error calculando el estado de KatPlugins: " + ex.Message);
				return KatPluginsState.Unknown;
			}
		}

		private static async Task<bool> IsRuneliteCracked()
		{
			try
			{
				string crackerFileName = "EthanVannInstaller.jar";
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string runeliteDir = Path.Combine(localAppData, "RuneLite");
				string configFile = Path.Combine(runeliteDir, "config.json");

				if (!Directory.Exists(runeliteDir))
					return false;

				string crackerPath = Path.Combine(runeliteDir, crackerFileName);
				if (!File.Exists(crackerPath))
					return false;

				if (!File.Exists(configFile))
					return false;

				var jsonText = await File.ReadAllTextAsync(configFile);
				var node = JsonNode.Parse(jsonText);
				if (node == null)
					return false;

				var mainClass = node["mainClass"]?.GetValue<string>();
				if (mainClass != "ca.arnah.runelite.LauncherHijack")
					return false;

				if (node["classPath"] is not JsonArray classPathArray)
					return false;

				bool hasCracker = classPathArray.Any(x => x?.GetValue<string>() == crackerFileName);
				bool hasRuneliteJar = classPathArray.Any(x => x?.GetValue<string>() == "RuneLite.jar");

				return hasCracker && hasRuneliteJar;
			}
			catch
			{
				return false;
			}
		}

		private static async Task<string> ObtainLastVersion()
		{
            var client = new GitHubClient(new ProductHeaderValue("KatHub"));
			var release = await client.Repository.Release.GetLatest(gitHubRepoOwner, gitHubRepoName);
			string version = release?.TagName?.TrimStart('v') ?? "-1";

			// Find asset whose name ends with ".jar"
			var asset = release?.Assets?.FirstOrDefault(x => x?.Name?.EndsWith(".jar") == true);

			if (asset == null || string.IsNullOrEmpty(asset.Url))
			{
				return "-1";
			}

			jarRelease = new JarRelease
			{
				Version = version,
				DownloadUrl = asset.Url
			};

			return version;
		}

		private static string ObtainInstalledVersion()
		{
			try
			{
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string runeliteDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".runelite",
					"sideloaded-plugins"
				);
				string[] jars = Directory.GetFiles(runeliteDir, "Kat*.jar");
				if (jars.Length > 1)
                    return "-69";
                if (jars.Length == 0)
					return "-1";
				string version = Path.GetFileNameWithoutExtension(jars[0]).Split('-').Last();
				return version;
			}
			catch
			{
				return "-1";
			}
		}
	}
}