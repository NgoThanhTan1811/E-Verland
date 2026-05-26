using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Modules.Redis.Infrastructure;

namespace Modules.Product.Api.Controllers
{
    [ApiController]
    [Route("api/test")]
    [Authorize(Policy = "AdminPolicy")]
    public class TestController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<TestController> _logger;

        public TestController(ICacheService cacheService, ILogger<TestController> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        [HttpGet("test-redis")]
        public async Task<IActionResult> TestRedis()
        {
            try
            {
                // Dữ liệu test
                var testData = new
                {
                    Id = 1,
                    Message = "Hello Redis!",
                    Timestamp = DateTime.UtcNow,
                    Status = "Success"
                };

                // Khóa để lưu trữ
                var cacheKey = "test:data:sample";

                // Lưu vào Redis với thời hạn 1 giờ
                await _cacheService.SetAsync(cacheKey, testData, TimeSpan.FromHours(1));

                _logger.LogInformation("Data saved to Redis with key: {CacheKey}", cacheKey);

                return Ok(new
                {
                    Success = true,
                    Message = "Data saved to Redis successfully",
                    Data = testData,
                    CacheKey = cacheKey,
                    ExpirationTime = "1 hour"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving data to Redis");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error saving data to Redis",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("get-redis/{key}")]
        public async Task<IActionResult> GetRedisData(string key)
        {
            try
            {
                var data = await _cacheService.GetAsync<dynamic>($"test:data:{key}");

                if (data == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"No data found for key: test:data:{key}"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Data retrieved from Redis",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data from Redis");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error retrieving data from Redis",
                    Error = ex.Message
                });
            }
        }

        [HttpDelete("delete-redis/{key}")]
        public async Task<IActionResult> DeleteRedisData(string key)
        {
            try
            {
                await _cacheService.RemoveAsync($"test:data:{key}");

                return Ok(new
                {
                    Success = true,
                    Message = $"Data deleted from Redis for key: test:data:{key}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting data from Redis");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error deleting data from Redis",
                    Error = ex.Message
                });
            }
        }
    }
}
