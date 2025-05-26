using FileWebApiDemo.Model;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using System.Text.RegularExpressions;

namespace FileWebApiDemo.Services
{
    public class MinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinioSettings _settings;

        public MinioService(IOptions<MinioSettings> settings)
        {
            _settings = settings.Value;

            _minioClient = new MinioClient()
                .WithEndpoint(_settings.Endpoint)
                .WithCredentials(_settings.AccessKey, _settings.SecretKey)
                .WithSSL(_settings.WithSSL)
                .Build();
        }

        public async Task UploadFileAsync(IFormFile file,string folderName )
        {
            //folder VAR MI? YOKSA OLUŞTUR
            await EnsureFolderExistsAsync(folderName);

            //DOSYAYI YÜKLE
            await using var stream = file.OpenReadStream();

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(folderName)
                .WithObject(file.FileName)
                .WithStreamData(stream)
                .WithObjectSize(file.Length)
                .WithContentType(file.ContentType);

            await _minioClient.PutObjectAsync(putObjectArgs);
        }

        public async Task<Stream> GetFileAsync(string fileName, string folderName)
        {
            // Bucket (klasör) mevcut değilse oluştur
            await EnsureFolderExistsAsync(folderName);

            // İlgili dosya MinIO üzerinde var mı kontrol et
            try
            {
                await _minioClient.StatObjectAsync(
                    new StatObjectArgs()
                        .WithBucket(folderName)
                        .WithObject(fileName)
                );
            }
            catch (Minio.Exceptions.ObjectNotFoundException)
            {
                throw new FileNotFoundException("İstenen dosya bulunamadı.", fileName);
            }

            // Dosya varsa, belleğe al ve döndür
            var memoryStream = new MemoryStream();

            await _minioClient.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(folderName)
                    .WithObject(fileName)
                    .WithCallbackStream(stream =>
                    {
                        stream.CopyTo(memoryStream);
                    })
            );

            memoryStream.Position = 0;
            return memoryStream;
        }

        private async Task EnsureFolderExistsAsync(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                throw new ArgumentException("Klasör adı girmediniz.", nameof(folderName));

            if (folderName.Length < 3 || folderName.Length > 63)
                throw new ArgumentException("Klasör adı 3 ile 63 karakter arasında olmalıdır.");

            if (!Regex.IsMatch(folderName, @"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$"))
                throw new ArgumentException("Klasör adı yalnızca küçük harf, rakam ve tire (-) içerebilir. Tire ile başlayamaz veya bitemez.");

            if (folderName.Contains("--"))
                throw new ArgumentException("Klasör adı arka arkaya iki tire (--) içeremez.");

            bool exists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(folderName)
            );

            if (!exists)
            {
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(folderName)
                );
            }
        }


    }
}
