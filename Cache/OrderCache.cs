using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EcommerceAPI.Cache
{
    public class OrderCache
    {
        private readonly ECommerceDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);


        public OrderCache(ECommerceDbContext context, IMemoryCache cache, IMapper mapper)
        {
            _context = context;
            _cache = cache;
            _mapper = mapper;
        }

        public async Task<List<OrderDTO>> GetOrdersAsync(int id)
        {
            var cacheKey = $"GetOrderById_{id}";
            if (!_cache.TryGetValue(cacheKey, out List<OrderDTO>? OrderById))
            {
                OrderById = await _context.Orders
                    .Where(o => o.Id == id)
                    .AsNoTracking()
                    .ProjectTo<OrderDTO>(_mapper.ConfigurationProvider)
                    .ToListAsync();
                _cache.Set(cacheKey, OrderById, _cacheExpiration);
            }

            return OrderById ?? new List<OrderDTO>();
        }

        public async Task<List<OrderDTO>> GetOrdersByCustomersId(int id)
        {
            var cacheKey = $"GetOrdersByCustomerId_{id}";
            if (_cache.TryGetValue(cacheKey, out List<OrderDTO>? orders)) return orders ?? new List<OrderDTO>();
            orders = await _context.Orders
                .Where(c => c.Id == id)
                .AsNoTracking()
                .ProjectTo<OrderDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();
            _cache.Set(cacheKey, orders, _cacheExpiration);

            return orders ?? new List<OrderDTO>();
        }
    }
}

