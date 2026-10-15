namespace SRM.Api.Data
{
    public static class ImageSeeder
    {
        public static void SeedImages(string seedSourcePath, string storagePath)
        {
            Directory.CreateDirectory(storagePath);

            if (Directory.EnumerateFiles(storagePath).Any())
            {
                return; // ya hay imágenes, no pisamos nada
            }

            foreach (var file in Directory.GetFiles(seedSourcePath))
            {
                var destFile = Path.Combine(storagePath, Path.GetFileName(file));
                File.Copy(file, destFile);
            }
        }
    }
}
