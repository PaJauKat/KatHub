using Octokit;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KatHub.KatPlugins
{
	public class KatPluginsService
	{

		private static JarRelease? jarRelease;
		public static string CurrentKatPluginsVersion { get; private set; } = "1.0";
		public static string LatestJarVersion { get; private set; } = "1.0";
		private static KatPluginsService instance;


        public static async Task<bool> ExecuteCrackearRunelite() 
		{
			string crackerFileName = "EthanVannInstaller.jar";
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string runeliteDir = Path.Combine(localAppData, "RuneLite");
			string configFile = Path.Combine(runeliteDir, "config.json");

			try
			{
				if (!Directory.Exists(runeliteDir))
				{
					throw new DirectoryNotFoundException("Runelite directory not found. Please install RuneLite first.");
				}

				string crackerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, crackerFileName);
				if (!File.Exists(crackerPath))
				{
					throw new FileNotFoundException($"Cracker file '{crackerFileName}' not found in application directory.");
				}

				File.Copy(crackerPath, Path.Combine(runeliteDir, crackerFileName), true);
				Console.WriteLine("Cracker file copied successfully to RuneLite directory.");

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
					Console.WriteLine("Config updated successfully.");
					return true;
				}
				else
				{
					Console.WriteLine("Config file not found.");
					return false;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				return false;
			}
		}

		public static async Task<bool> ExecuteInstallOrUpdateJar(IProgress<double> progress)
		{
			try
			{
				if (jarRelease == null)
				{
					await ObtainLastVersion();
				}

				if (jarRelease == null)
				{
					throw new Exception("Error obteniendo la última versión del Jar.");
				}

				string jarDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".runelite",
					"sideloaded-plugins"
				);
				Directory.CreateDirectory(jarDir);
				string jarPath = Path.Combine(jarDir, $"KatPlugins-{jarRelease.Version}.jar");

                var client = new HttpClient();
				client.DefaultRequestHeaders.UserAgent.ParseAdd("KatHub");
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", MainWindow.GITHUB_TOKEN);
				client.DefaultRequestHeaders.Accept.Clear();
				client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/octet-stream"));
				client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

				using var response = await client.GetAsync(jarRelease.DownloadUrl, HttpCompletionOption.ResponseHeadersRead); 
				response.EnsureSuccessStatusCode();
				var totalBytes = response.Content.Headers.ContentLength ?? -1L;
				using var contentStream = await response.Content.ReadAsStreamAsync();
				using var fileStream = new FileStream(jarPath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

				var buffer = new byte[8192];
				long totalRead = 0;
				int bytesRead;

				while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					await fileStream.WriteAsync(buffer, 0, bytesRead);
					totalRead += bytesRead;
					if (totalBytes > 0)
					{
						var porcentaje = (totalRead * 100) / totalBytes;
						progress.Report(porcentaje);
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
				CurrentKatPluginsVersion = ObtainCurrentVersion();
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
			client.Credentials = new Credentials(MainWindow.GITHUB_TOKEN);

			var release = await client.Repository.Release.GetLatest("PaJauKat", "KatPlugins");
			string version = release?.TagName?.TrimStart('v') ?? "-1";

			// Find asset whose name ends with ".jar"
			var asset = release?.Assets?.FirstOrDefault(x => !string.IsNullOrEmpty(x?.Name) && x.Name.EndsWith(".jar"));

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

		private static string ObtainCurrentVersion()
		{
			try
			{
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string runeliteDir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".runelite",
					"sideloaded-plugins"
				);
				string[] jars = Directory.GetFiles(runeliteDir, "KatPlugins-*.jar");
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