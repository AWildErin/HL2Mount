using Sandbox.Mounting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using vpkpp;

namespace HL2Mount;

public class Hl2Mount : BaseGameMount
{
	private string? RootFolder { get; set; }

	public override string Ident { get; } = "hl2";
	public override string Title { get; } = "Half-Life 2";

	private const long APP_ID = 220L;

	// Credit to some random stack overflow thread I cannot find anymore
	// This has to exist otherwise we'd need to place the dependent dlls in sbox/bin/managed, which sucks and we shouldn't do at all
	// This should also be cleaned up better, right now it spits out a lot of warnings and that's not great
	static Assembly? LoadFromSameFolder(object? sender, ResolveEventArgs args)
	{
		string? folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		string assemblyPath = Path.Combine(folderPath ?? "", new AssemblyName(args.Name).Name + ".dll");
		if (!File.Exists(assemblyPath)) return null;
		Assembly assembly = Assembly.LoadFrom(assemblyPath);
		return assembly;
	}

	protected override void Initialize(InitializeContext context)
	{
		if (!context.IsAppInstalled(APP_ID)) return;

		RootFolder = context.GetAppDirectory(APP_ID);

		AppDomain currentDomain = AppDomain.CurrentDomain;
		currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

		IsInstalled = File.Exists(RootFolder);
	}

	protected override Task Mount(MountContext context)
	{
		if (!File.Exists(RootFolder + "/hl2.exe"))
		{
			context.AddError(RootFolder + " is not installed");
			return Task.CompletedTask;
		}

		Log.Info("Adding HL2 files...");

		List<string> vpkPaths = new()
		{
			// Add other VPKs here
			$"{RootFolder}/hl2/hl2_textures_dir.vpk",

			//$"{RootFolder}/hl2/hl2_misc_dir.vpk",
			//$"{RootFolder}/hl2/hl2_sound_misc_dir.vpk",
		};

		foreach (var path in vpkPaths)
		{
			Log.Info($"Loading {path}...");

			List<string> paths = new();

			var vpk = PackFile.Open(path, (path, entry) =>
			{
				paths.Add(path);
			});

			if (vpk == null)
			{
				Log.Error("VPK was null!");
				context.AddError("VPK was null!");
				continue;
			}

			foreach (var filePath in paths)
			{
				var extension = Path.GetExtension(filePath);

				switch (extension)
				{
					case ".vtf":
						context.Add(ResourceType.Texture, Path.Combine(Path.GetFileNameWithoutExtension(vpk.FileName), filePath), new VTF
						{
							FileName = filePath,
							PackFile = vpk
						});
						break;

					default:
						Log.Warning($"Unknown file type {extension}. Skipping.");
						break;
				}
			}
		}

		IsMounted = true;
		return Task.CompletedTask;
	}

}