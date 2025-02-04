using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace SecureCrudAPI.Services
{
    public class FileUploadService
    {
        private readonly Cloudinary _cloudinary;

        public FileUploadService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file.Length > 0)
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream())
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return uploadResult.SecureUrl.ToString();
                }

                throw new Exception("Error uploading file.");
            }

            throw new Exception("File is empty.");
        }
    }
}
