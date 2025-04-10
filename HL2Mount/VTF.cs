using Sandbox;
using Sandbox.Mounting;
using vpkpp;
using vtfpp;

namespace HL2Mount;

class VTF : ResourceLoader<Hl2Mount>
{
	public string? FileName { get; set; }

	public PackFile? PackFile { get; set; }

	protected override object? Load()
	{
		object obj;

		var buffer = PackFile?.ReadEntry(FileName ?? "");
		if (buffer == null)
		{
			Log.Error($"Failed to read {FileName} from {PackFile?.FilePath}");
		}

		var vtf = vtfpp.VTF.OpenFromMemory(buffer ?? [], 0);

		ushort width = 0;
		ushort height = 0;
		if (vtf != null)
		{
			width = vtf?.GetWidth() ?? 0;
			height = vtf?.GetHeight() ?? 0;
		}

		// Only uses mip 0 for now
		obj = Texture.Create(width, height).WithData(vtf?.GetImageDataAsRGBA8888(0, 0, 0, 0) ?? []).Finish();

		return obj;
	}
}
