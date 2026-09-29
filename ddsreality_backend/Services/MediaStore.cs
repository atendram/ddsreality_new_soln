using System.Text;
namespace DdsCms.Services;
public class MediaStore(IWebHostEnvironment env, IConfiguration config)
{
    public string DirectoryPath => Path.GetFullPath(config["Storage:MediaPath"] ?? "App_Data/media", env.ContentRootPath);
    public async Task<string?> Save(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > 5 * 1024 * 1024) throw new ArgumentException("Choose an image smaller than 5 MB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var n = await input.ReadAsync(header);
        bool valid = n == 12 && (ext switch {
            ".jpg" or ".jpeg" => header[0] == 255 && header[1] == 216 && header[2] == 255,
            ".png" => header.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),
            ".webp" => Encoding.ASCII.GetString(header,0,4) == "RIFF" && Encoding.ASCII.GetString(header,8,4) == "WEBP",
            _ => false
        });
        if (!valid) throw new ArgumentException("Upload a valid JPG, PNG or WebP image. SVG, GIF, PDF and video uploads are not accepted.");
        Directory.CreateDirectory(DirectoryPath);
        var name = Guid.NewGuid().ToString("N") + ext;
        await using var output = new FileStream(Path.Combine(DirectoryPath,name), FileMode.CreateNew);
        await output.WriteAsync(header.AsMemory(0,n));
        await input.CopyToAsync(output);
        return "/media/" + name;
    }
    public bool IsImage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (value.Contains("..") || value.Contains('\\') || value.Contains('%') || value.Contains('?') || value.Contains('#')) return false;
        var root = value.StartsWith("/media/",StringComparison.Ordinal) ? DirectoryPath : value.StartsWith("/images/",StringComparison.Ordinal) ? Path.Combine(env.WebRootPath,"images") : null;
        if (root == null) return false;
        var leaf = value[(value.IndexOf('/',1)+1)..];
        return new[]{".png",".jpg",".jpeg",".webp"}.Contains(Path.GetExtension(leaf).ToLowerInvariant()) && File.Exists(Path.Combine(root,leaf));
    }
    public static bool SafeLink(string? s)
    {
        if (string.IsNullOrEmpty(s)) return true;
        if (s.Any(char.IsControl) || s.Contains('\\')) return false;
        if (s.StartsWith('/') && !s.StartsWith("//") && !s.Contains('%')) return true;
        return Uri.TryCreate(s,UriKind.Absolute,out var u) && u.Scheme == "https" && string.IsNullOrEmpty(u.UserInfo);
    }
    public static bool SafeMap(string? s) => string.IsNullOrEmpty(s) ||
        (Uri.TryCreate(s,UriKind.Absolute,out var u) && u.Scheme == "https" && u.Host == "www.google.com" && u.AbsolutePath.StartsWith("/maps/embed",StringComparison.Ordinal) && string.IsNullOrEmpty(u.UserInfo));
}
