using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Amazon.EventBridge;
using Amazon.Runtime;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Infra.AWS.S3;
using Infra.AWS.SQS;
using Infra.AWS.SNS;
using Infra.AWS.EventBridge;
using Infra.AWS.CloudWatch;
using Infra.AWS.Configuration;
using Infra.AWS.Storage;
using Infra.Meilisearch;
using Microsoft.Extensions.Options;


namespace Infra.AWS;

public static class AWSServiceExtensions
{
    public static IServiceCollection AddAWSInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var awsRegion = ConfigurationValueResolver.GetOptional(configuration, "AWS:Region", "AWS_REGION") ?? "ap-southeast-1";
        var awsAccessKey = ConfigurationValueResolver.GetOptional(configuration, "AWS:AccessKey", "AWS_ACCESS_KEY_ID");
        var awsSecretKey = ConfigurationValueResolver.GetOptional(configuration, "AWS:SecretKey", "AWS_SECRET_ACCESS_KEY");

        var awsOptions = configuration.GetAWSOptions();
        awsOptions.Region = RegionEndpoint.GetBySystemName(awsRegion);

        if (!string.IsNullOrWhiteSpace(awsAccessKey) && !string.IsNullOrWhiteSpace(awsSecretKey))
        {
            awsOptions.Credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
        }

        services.AddDefaultAWSOptions(awsOptions);

        // Configure options
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.Configure<SQSOptions>(configuration.GetSection(SQSOptions.SectionName));
        services.Configure<SNSOptions>(configuration.GetSection(SNSOptions.SectionName));
        services.Configure<EventBridgeOptions>(configuration.GetSection(EventBridgeOptions.SectionName));
        services.Configure<CloudWatchOptions>(configuration.GetSection(CloudWatchOptions.SectionName));
        services.Configure<MeilisearchOptions>(configuration.GetSection(MeilisearchOptions.SectionName));

        ApplyEnvironmentFallbacks(services, configuration);

        ValidateAwsConfiguration(configuration, awsAccessKey, awsSecretKey);

        // Register AWS SDK clients
        services.AddSingleton<IAmazonSQS>(sp =>
        {
            var awsOptions = sp.GetRequiredService<IOptions<AWSOptions>>().Value;

            var config = new AmazonSQSConfig
            {
                RegionEndpoint = awsOptions.Region,
                Timeout = TimeSpan.FromSeconds(60),
            };

            return awsOptions.Credentials != null
                ? new AmazonSQSClient(awsOptions.Credentials, config)
                : new AmazonSQSClient(config);
        });
        services.AddAWSService<IAmazonSimpleNotificationService>();
        services.AddAWSService<IAmazonEventBridge>();
        services.AddSingleton<IAmazonS3>(sp => CreateS3Client(sp, awsOptions));

        // Register custom services
        services.AddSingleton<IS3StorageService, S3StorageService>();
        RegisterStorageProvider(services, configuration);
        services.AddSingleton<ISQSService, SQSService>();
        services.AddSingleton<ISNSService, SNSService>();
        services.AddSingleton<IEventBridgeService, EventBridgeService>();
        services.AddSingleton<ICloudWatchService, CloudWatchService>();
        // Register HttpClient for Meilisearch and Meilisearch service
        services.AddHttpClient("meilisearch")
            .ConfigureHttpClient((sp, client) =>
            {
                var opts = sp.GetRequiredService<IOptions<MeilisearchOptions>>().Value;
                client.BaseAddress = new Uri(opts.Endpoint);
                client.Timeout = TimeSpan.FromSeconds(Math.Max(5, opts.RequestTimeoutSeconds));
                if (!string.IsNullOrWhiteSpace(opts.MasterKey))
                    client.DefaultRequestHeaders.Add("X-Meili-API-Key", opts.MasterKey);
            });

        services.AddSingleton<IMeilisearchService, MeilisearchService>();

        return services;
    }

    private static void RegisterStorageProvider(IServiceCollection services, IConfiguration configuration)
    {
        // Resolve configured provider and register appropriate IStorageService
        var storageSection = configuration.GetSection(StorageOptions.SectionName);
        var provider = storageSection.GetValue<string>("Provider") ?? "S3";

        if (string.Equals(provider, "MinIO", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<Storage.MinIO.MinIOOptions>(configuration.GetSection(Storage.MinIO.MinIOOptions.SectionName));
            services.AddSingleton<IStorageService, Storage.MinIO.MinIOStorageService>();
        }
        else
        {
            // Default to S3 (also works with R2 via ServiceUrl)
            services.AddSingleton(sp =>
                (IStorageService)sp.GetRequiredService<IS3StorageService>());
        }
    }

    private static void ApplyEnvironmentFallbacks(IServiceCollection services, IConfiguration configuration)
    {
        services.PostConfigure<S3Options>(options =>
        {
            options.BucketName = string.IsNullOrWhiteSpace(options.BucketName)
                ? configuration["Aws:S3:BucketName"] ?? string.Empty
                : options.BucketName;
            options.BaseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? configuration["Aws:S3:BaseUrl"] ?? string.Empty
                : options.BaseUrl;
            options.Region = string.IsNullOrWhiteSpace(options.Region)
                ? configuration["Aws:S3:Region"] ?? "ap-southeast-1"
                : options.Region;
            options.ServiceUrl = string.IsNullOrWhiteSpace(options.ServiceUrl)
                ? configuration["Aws:S3:ServiceUrl"] ?? string.Empty
                : options.ServiceUrl;
            options.AccessKey = string.IsNullOrWhiteSpace(options.AccessKey)
                ? configuration["Aws:S3:AccessKey"] ?? string.Empty
                : options.AccessKey;
            options.SecretKey = string.IsNullOrWhiteSpace(options.SecretKey)
                ? configuration["Aws:S3:SecretKey"] ?? string.Empty
                : options.SecretKey;

            var forcePathStyle = configuration["Aws:S3:ForcePathStyle"];
            if (!string.IsNullOrWhiteSpace(forcePathStyle) && bool.TryParse(forcePathStyle, out var parsedForcePathStyle))
            {
                options.ForcePathStyle = parsedForcePathStyle;
            }
        });

        services.PostConfigure<SQSOptions>(options =>
        {
            options.OrderEventsQueueUrl = string.IsNullOrWhiteSpace(options.OrderEventsQueueUrl)
                ? configuration["Aws:SQS:OrderEventsQueueUrl"] ?? string.Empty
                : options.OrderEventsQueueUrl;
            options.PaymentNotificationsQueueUrl = string.IsNullOrWhiteSpace(options.PaymentNotificationsQueueUrl)
                ? configuration["Aws:SQS:PaymentEventsQueueUrl"] ?? string.Empty
                : options.PaymentNotificationsQueueUrl;
        });

        services.PostConfigure<SNSOptions>(options =>
        {
            options.OrderNotificationsTopicArn = string.IsNullOrWhiteSpace(options.OrderNotificationsTopicArn)
                ? configuration["Aws:SNS:OrderEventsTopicArn"] ?? string.Empty
                : options.OrderNotificationsTopicArn;
            options.PaymentNotificationsTopicArn = string.IsNullOrWhiteSpace(options.PaymentNotificationsTopicArn)
                ? configuration["Aws:SNS:PaymentEventsTopicArn"] ?? string.Empty
                : options.PaymentNotificationsTopicArn;
            options.UserNotificationsTopicArn = string.IsNullOrWhiteSpace(options.UserNotificationsTopicArn)
                ? configuration["Aws:SNS:NotificationTopicArn"] ?? string.Empty
                : options.UserNotificationsTopicArn;
        });

        services.PostConfigure<EventBridgeOptions>(options =>
        {
            options.EventBusName = string.IsNullOrWhiteSpace(options.EventBusName)
                ? configuration["Aws:EventBridge:EventBusName"] ?? "e-verland-events"
                : options.EventBusName;
            options.OrderEventSource = string.IsNullOrWhiteSpace(options.OrderEventSource)
                ? configuration["Aws:EventBridge:OrderEventSource"] ?? "e-verland.orders"
                : options.OrderEventSource;
            options.PaymentEventSource = string.IsNullOrWhiteSpace(options.PaymentEventSource)
                ? configuration["Aws:EventBridge:PaymentEventSource"] ?? "e-verland.payments"
                : options.PaymentEventSource;
            options.ProductEventSource = string.IsNullOrWhiteSpace(options.ProductEventSource)
                ? configuration["Aws:EventBridge:ProductEventSource"] ?? "e-verland.products"
                : options.ProductEventSource;
        });

        services.PostConfigure<MeilisearchOptions>(options =>
        {
            options.Endpoint = string.IsNullOrWhiteSpace(options.Endpoint)
                ? configuration["Meilisearch:Endpoint"] ?? "http://meilisearch:7700"
                : options.Endpoint;
            options.MasterKey = string.IsNullOrWhiteSpace(options.MasterKey)
                ? configuration["Meilisearch:MasterKey"] ?? string.Empty
                : options.MasterKey;
            options.IndexName = string.IsNullOrWhiteSpace(options.IndexName)
                ? configuration["Meilisearch:IndexName"] ?? "products"
                : options.IndexName;

            var timeoutStr = configuration["Meilisearch:RequestTimeoutSeconds"];
            if (!string.IsNullOrWhiteSpace(timeoutStr) && int.TryParse(timeoutStr, out var timeout))
            {
                options.RequestTimeoutSeconds = timeout;
            }
        });
    }

    private static void ValidateAwsConfiguration(IConfiguration configuration, string? awsAccessKey, string? awsSecretKey)
    {
        if (string.IsNullOrWhiteSpace(awsAccessKey) && string.IsNullOrWhiteSpace(awsSecretKey))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(awsAccessKey))
        {
            throw new InvalidOperationException("Missing required configuration 'AWS:AccessKey' (or environment variable 'AWS_ACCESS_KEY_ID').");
        }

        if (string.IsNullOrWhiteSpace(awsSecretKey))
        {
            throw new InvalidOperationException("Missing required configuration 'AWS:SecretKey' (or environment variable 'AWS_SECRET_ACCESS_KEY').");
        }
    }

    private static IAmazonS3 CreateS3Client(IServiceProvider sp, AWSOptions awsOptions)
    {
        var s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;
        var config = new AmazonS3Config
        {
            RegionEndpoint = awsOptions.Region
        };

        if (!string.IsNullOrWhiteSpace(s3Options.ServiceUrl))
        {
            config.ServiceURL = s3Options.ServiceUrl;
            config.ForcePathStyle = s3Options.ForcePathStyle;
            config.UseHttp = s3Options.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            config.AuthenticationRegion = s3Options.Region;
        }

        if (!string.IsNullOrWhiteSpace(s3Options.AccessKey) && !string.IsNullOrWhiteSpace(s3Options.SecretKey))
        {
            var credentials = new BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey);
            return new AmazonS3Client(credentials, config);
        }

        if (awsOptions.Credentials != null)
        {
            return new AmazonS3Client(awsOptions.Credentials, config);
        }

        return new AmazonS3Client(config);
    }

}
