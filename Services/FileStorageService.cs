namespace FindIt.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string subFolder = "items");
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subFolder = "items")
    {
        if (file == null || file.Length == 0) return string.Empty;

        var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{subFolder}/{uniqueFileName}";
    }
}
