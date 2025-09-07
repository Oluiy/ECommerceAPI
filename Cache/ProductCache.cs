using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EcommerceAPI.Cache
{
    public class ProductCache
    {
        private readonly ECommerceDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);


        public ProductCache(ECommerceDbContext context, IMemoryCache cache, IMapper mapper)
        {
            _context = context;
            _cache = cache;
            _mapper = mapper;
        }

        public async Task<List<Product>> GetProduct()
        {
            var cacheKey = "GetProducts";
            if (!_cache.TryGetValue(cacheKey, out List<Product>? products))
            {
                products = await _context.Products
                    .AsNoTracking()
                    .ProjectTo<Product>(_mapper.ConfigurationProvider)
                    .ToListAsync();
                _cache.Set(cacheKey, products, _cacheExpiration);
            }
            return products ?? new List<Product>();
        }

        public async Task<List<Product>> GetProdById(int prdId)
        {
            var cacheKey = $"GetProductById_{prdId}";
            if (!_cache.TryGetValue(cacheKey, out List<Product>? productById))
            {
                productById = await _context.Products
                    .Where(p => p.Id == prdId)
                    .AsNoTracking()
                    .ProjectTo<Product>(_mapper.ConfigurationProvider)
                    .ToListAsync();
                _cache.Set(cacheKey, productById, _cacheExpiration);
            }
            return productById ?? new List<Product>();
        }
    }
}

