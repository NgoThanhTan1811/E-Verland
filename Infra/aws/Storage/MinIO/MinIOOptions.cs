namespace Infra.AWS.Storage.MinIO;

public sealed class MinIOOptions
{
    public const string SectionName = "Storage:MinIO";

    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "e-verland-media";
    public bool UseSsl { get; set; }
}
