using Amazon.S3;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Amazon.EventBridge;
using Amazon.Runtime;
using Amazon;
using Infra.AWS.S3;
using Infra.AWS.SQS;
using Infra.AWS.SNS;
using Infra.AWS.EventBridge;
using Infra.AWS.CloudWatch;
using Infra.AWS.Configuration;
using Infra.AWS.Storage;
using Infra.AWS.Storage.MinIO;
using Infra.Meilisearch;


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
        services.Configure<MinIOOptions>(configuration.GetSection(MinIOOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.Configure<SQSOptions>(configuration.GetSection(SQSOptions.SectionName));
        services.Configure<SNSOptions>(configuration.GetSection(SNSOptions.SectionName));
        services.Configure<EventBridgeOptions>(configuration.GetSection(EventBridgeOptions.SectionName));
        services.Configure<CloudWatchOptions>(configuration.GetSection(CloudWatchOptions.SectionName));

        ApplyEnvironmentFallbacks(services);

        ValidateAwsConfiguration(configuration, awsAccessKey, awsSecretKey);

        // Register AWS SDK clients
        services.AddAWSService<IAmazonS3>();
        services.AddAWSService<IAmazonSQS>();
        services.AddAWSService<IAmazonSimpleNotificationService>();
        services.AddAWSService<IAmazonEventBridge>();

        // Register custom services
        services.AddSingleton<IS3StorageService, S3StorageService>();
        RegisterStorageProvider(services, configuration);
        services.AddSingleton<ISQSService, SQSService>();
        services.AddSingleton<ISNSService, SNSService>();
        services.AddSingleton<IEventBridgeService, EventBridgeService>();
        services.AddSingleton<ICloudWatchService, CloudWatchService>();
        services.AddSingleton<IMeilisearchService, MeilisearchService>();

        return services;
    }

    private static void RegisterStorageProvider(IServiceCollection services, IConfiguration configuration)
    {
        var provider = ConfigurationValueResolver.GetOptional(configuration, "Storage:Provider", "STORAGE_PROVIDER") ?? "MinIO";

        if (provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IStorageService>(sp =>
                (IStorageService)sp.GetRequiredService<IS3StorageService>());
            return;
        }

        services.AddSingleton<IStorageService, MinIOStorageService>();
    }

    private static void ApplyEnvironmentFallbacks(IServiceCollection services)
    {
        services.PostConfigure<MinIOOptions>(options =>
        {
            options.Endpoint = options.Endpoint ?? Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "http://localhost:9000";
            options.AccessKey = string.IsNullOrWhiteSpace(options.AccessKey)
                ? Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? string.Empty
                : options.AccessKey;
            options.SecretKey = string.IsNullOrWhiteSpace(options.SecretKey)
                ? Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? string.Empty
                : options.SecretKey;
            options.BucketName = string.IsNullOrWhiteSpace(options.BucketName)
                ? Environment.GetEnvironmentVariable("MINIO_BUCKET_NAME") ?? "e-verland-media"
                : options.BucketName;
        });

        services.PostConfigure<S3Options>(options =>
        {
            options.BucketName = string.IsNullOrWhiteSpace(options.BucketName)
                ? Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME") ?? string.Empty
                : options.BucketName;
            options.BaseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? Environment.GetEnvironmentVariable("AWS_S3_BASE_URL") ?? string.Empty
                : options.BaseUrl;
            options.Region = string.IsNullOrWhiteSpace(options.Region)
                ? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1"
                : options.Region;
        });

        services.PostConfigure<SQSOptions>(options =>
        {
            options.OrderEventsQueueUrl = string.IsNullOrWhiteSpace(options.OrderEventsQueueUrl)
                ? Environment.GetEnvironmentVariable("AWS_SQS_ORDER_EVENTS_QUEUE_URL") ?? string.Empty
                : options.OrderEventsQueueUrl;
            options.PaymentNotificationsQueueUrl = string.IsNullOrWhiteSpace(options.PaymentNotificationsQueueUrl)
                ? Environment.GetEnvironmentVariable("AWS_SQS_PAYMENT_EVENTS_QUEUE_URL") ?? string.Empty
                : options.PaymentNotificationsQueueUrl;
        });

        services.PostConfigure<SNSOptions>(options =>
        {
            options.OrderNotificationsTopicArn = string.IsNullOrWhiteSpace(options.OrderNotificationsTopicArn)
                ? Environment.GetEnvironmentVariable("AWS_SNS_ORDER_EVENTS_TOPIC_ARN") ?? string.Empty
                : options.OrderNotificationsTopicArn;
            options.PaymentNotificationsTopicArn = string.IsNullOrWhiteSpace(options.PaymentNotificationsTopicArn)
                ? Environment.GetEnvironmentVariable("AWS_SNS_PAYMENT_EVENTS_TOPIC_ARN") ?? string.Empty
                : options.PaymentNotificationsTopicArn;
            options.UserNotificationsTopicArn = string.IsNullOrWhiteSpace(options.UserNotificationsTopicArn)
                ? Environment.GetEnvironmentVariable("AWS_SNS_NOTIFICATION_TOPIC_ARN") ?? string.Empty
                : options.UserNotificationsTopicArn;
        });

        services.PostConfigure<EventBridgeOptions>(options =>
        {
            options.EventBusName = string.IsNullOrWhiteSpace(options.EventBusName)
                ? Environment.GetEnvironmentVariable("AWS_EVENTBRIDGE_BUS_NAME") ?? "e-verland-events"
                : options.EventBusName;
            options.OrderEventSource = string.IsNullOrWhiteSpace(options.OrderEventSource)
                ? Environment.GetEnvironmentVariable("AWS_EVENTBRIDGE_ORDER_SOURCE") ?? "e-verland.orders"
                : options.OrderEventSource;
            options.PaymentEventSource = string.IsNullOrWhiteSpace(options.PaymentEventSource)
                ? Environment.GetEnvironmentVariable("AWS_EVENTBRIDGE_PAYMENT_SOURCE") ?? "e-verland.payments"
                : options.PaymentEventSource;
            options.ProductEventSource = string.IsNullOrWhiteSpace(options.ProductEventSource)
                ? Environment.GetEnvironmentVariable("AWS_EVENTBRIDGE_PRODUCT_SOURCE") ?? "e-verland.products"
                : options.ProductEventSource;
        });
    }

    private static void ValidateAwsConfiguration(IConfiguration configuration, string? awsAccessKey, string? awsSecretKey)
    {
        var shouldValidateCredentials = HasAnyConfiguredValue(configuration,
            "AWS:SQS:OrderEventsQueueUrl",
            "AWS:SNS:OrderNotificationsTopicArn",
            "AWS:EventBridge:EventBusName") ||
            string.Equals(ConfigurationValueResolver.GetOptional(configuration, "Storage:Provider", "STORAGE_PROVIDER"), "S3", StringComparison.OrdinalIgnoreCase);

        if (!shouldValidateCredentials)
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

    private static bool HasAnyConfiguredValue(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(configuration[key]))
            {
                return true;
            }
        }

        return false;
    }
}
