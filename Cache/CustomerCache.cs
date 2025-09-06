using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EcommerceAPI.Cache
{
    public class CustomerCache
    {
        private readonly ECommerceDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);


        public CustomerCache(ECommerceDbContext context, IMemoryCache cache, IMapper mapper)
        {
            _context = context;
            _cache = cache;
            _mapper = mapper;
        }

        public async Task<List<Customer>> GetCustomers(int cusId)
        {
            var cacheKey = $"customers_{cusId}";
            if (!_cache.TryGetValue(cacheKey, out List<Customer>? CustomerById))
            {
                CustomerById = await _context.Customers
                    .Where(c => c.Id == cusId)
                    .AsNoTracking()
                    .ProjectTo<Customer>(_mapper.ConfigurationProvider)
                    .ToListAsync();
                _cache.Set(cacheKey, CustomerById, _cacheExpiration);
            }

            return CustomerById ?? new List<Customer>();
        }
    }
}

