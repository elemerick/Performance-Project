using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Timers;
using CarvedRock.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CarvedRock.Data
{
    public class CarvedRockRepository :ICarvedRockRepository
    {
        private readonly LocalContext _ctx;
        private readonly ILogger<CarvedRockRepository> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger _factoryLogger;

        public CarvedRockRepository(LocalContext ctx, ILogger<CarvedRockRepository> logger,
            ILoggerFactory loggerFactory, 
            IMemoryCache memoryCache,
            IDistributedCache distributedCache)
        {
            _ctx = ctx;
            _logger = logger;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _factoryLogger = loggerFactory.CreateLogger("DataAccessLayer");
        }
        public async Task<List<Product>> GetProductsAsync(string category)
        {            
            _logger.LogInformation("Getting products in repository for {category}", category);
            /*if (category == "clothing")
            {
                var ex = new ApplicationException("Database error occurred!!");
                ex.Data.Add("Category", category);
                throw ex;
            }
            if (category == "equip")
            {
                throw new SqliteException("Simulated fatal database error occurred!", 551);
            }*/

            try
            {
                var cacheKey = $"products_{category}";
                /*if (!_memoryCache.TryGetValue(cacheKey, out List<Product> results)) {
                    Thread.Sleep(5000);
                    results =  await _ctx.Products.Where(p => p.Category == category || category == "all").ToListAsync();
                    _memoryCache.Set(cacheKey, results, TimeSpan.FromMinutes(2));
                }
                return results;*/

                var resultsByte = _distributedCache.Get(cacheKey);
                if (resultsByte == null) {
                    var timer = new Stopwatch();
                    timer.Start();
                    Thread.Sleep(5000);
                    var productsToSerialize = await _ctx.Products
                        .Where(p => p.Category == category || category == "all")
                        .Include(p => p.Rating).ToListAsync();

                    var serializedProducts = JsonSerializer.Serialize(productsToSerialize, 
                        CacheSourceGenerationContext.Default.ListProduct);

                    _distributedCache.Set(cacheKey, Encoding.UTF8.GetBytes(serializedProducts),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                        });
                    timer.Stop();
                    _logger.LogInformation("Database caching took {ElapsedMs}", timer.ElapsedMilliseconds);
                    return productsToSerialize;
                } else
                {
                    var results = JsonSerializer.Deserialize(Encoding.UTF8.GetString(resultsByte),
                        CacheSourceGenerationContext.Default.ListProduct);
                    return results ?? new List<Product>();
                }

                
            } 
            catch (Exception ex)
            {
                var newEx = new ApplicationException("Something bad happened in database", ex);
                newEx.Data.Add("Category", category);
                throw newEx;
            }
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _ctx.Products.FindAsync(id);
        }

        public List<Product> GetProducts(string category)
        {
            return _ctx.Products.Where(p => p.Category == category || category == "all").ToList();
        }

        public Product? GetProductById(int id)
        {
            var timer = new Stopwatch();  
            timer.Start();
            
            var product = _ctx.Products.Find(id);
            timer.Stop();

            _logger.LogDebug("Querying products for {id} finished in {milliseconds} milliseconds", 
                id, timer.ElapsedMilliseconds);	 

            _factoryLogger.LogInformation("(F) Querying products for {id} finished in {ticks} ticks", 
                id, timer.ElapsedTicks);           

            return product;
        }       
    }
}
