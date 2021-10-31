using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RestCountries.Appliation.Models;
using System.Text.Json;

namespace RestCountries.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly IDistributedCache _distributedCache;
        private const string ContriesKey = "Countries";
        private const string RestContriesURL = "https://restcountries.com/v2/all";

        public CountryController(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        [HttpGet]
        public async Task<IActionResult> GetCountry()
        {
            var contriesObject = await _distributedCache.GetStringAsync(ContriesKey);

            if (!string.IsNullOrWhiteSpace(contriesObject))
                return Ok(contriesObject);

            using var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(RestContriesURL);
            
            var responseData = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var countries = JsonSerializer.Deserialize<List<Country>>(responseData, options);

            var memoryCacheEntryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(2)
            };

            await _distributedCache.SetStringAsync(ContriesKey, responseData, memoryCacheEntryOptions);

            return Ok(countries);
        }
    }
}
