using swd.Application.DTOs.CartCombo;
using swd.Application.Exceptions;
using swd.Application.Services;
using swd.Domain.Interfaces;

namespace swd.Tests;

public class CartComboServiceTests
{
    [Fact]
    public async Task AddComboAsync_ShouldCreateCartAndApplyPromotion()
    {
        var cartRepo = new InMemoryCartRepository();
        var comboRepo = new InMemoryRepository<Combo>(new List<Combo>
        {
            new() { Id = "combo-1", Name = "Starter Combo", TotalPrice = 100m }
        });
        var promoRepo = new InMemoryRepository<Promotion>(new List<Promotion>
        {
            new()
            {
                Id = "promo-10",
                Name = "Ten Percent",
                DiscountPercent = 10m,
                StartAt = DateTime.UtcNow.AddDays(-1),
                EndAt = DateTime.UtcNow.AddDays(1),
                Status = "Active"
            }
        });
        var service = new CartComboService(cartRepo, comboRepo, promoRepo);

        var result = await service.AddComboAsync("user-1", new CreateCartComboRequest
        {
            ComboId = "combo-1",
            Quantity = 2,
            PromotionId = "promo-10",
            UseBestPromotion = false
        });

        var line = Assert.Single(result.Items);
        Assert.Equal("user-1", result.UserId);
        Assert.Equal("combo-1", line.ComboId);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(100m, line.UnitPrice);
        Assert.Equal(90m, line.FinalUnitPrice);
        Assert.Equal(180m, line.LineTotal);
        Assert.Equal(200m, result.SubTotal);
        Assert.Equal(20m, result.DiscountTotal);
        Assert.Equal(180m, result.GrandTotal);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task AddComboAsync_ShouldAutoApplyBestPromotion_WhenPromotionIdNotProvided()
    {
        var cartRepo = new InMemoryCartRepository();
        var comboRepo = new InMemoryRepository<Combo>(new List<Combo>
        {
            new() { Id = "combo-auto", Name = "Auto Promo Combo", TotalPrice = 200m }
        });
        var promoRepo = new InMemoryRepository<Promotion>(new List<Promotion>
        {
            new()
            {
                Id = "promo-10",
                Name = "Ten Percent",
                DiscountPercent = 10m,
                Priority = 1,
                StartAt = DateTime.UtcNow.AddDays(-1),
                EndAt = DateTime.UtcNow.AddDays(1),
                Status = "Active"
            },
            new()
            {
                Id = "promo-20",
                Name = "Twenty Percent",
                DiscountPercent = 20m,
                Priority = 2,
                StartAt = DateTime.UtcNow.AddDays(-1),
                EndAt = DateTime.UtcNow.AddDays(1),
                Status = "Active"
            }
        });
        var service = new CartComboService(cartRepo, comboRepo, promoRepo);

        var result = await service.AddComboAsync("user-auto", new CreateCartComboRequest
        {
            ComboId = "combo-auto",
            Quantity = 1
        });

        var line = Assert.Single(result.Items);
        Assert.Equal("promo-20", line.PromotionId);
        Assert.Equal(20m, line.DiscountPercent);
        Assert.Equal(160m, line.FinalUnitPrice);
    }

    [Fact]
    public async Task GetCartByUserIdAsync_ShouldReturnExistingCart()
    {
        var existingCart = new Cart
        {
            Id = "cart-1",
            UserId = "user-2",
            Version = 4,
            ComboItems = new List<CartComboItem>
            {
                new()
                {
                    ComboId = "combo-2",
                    ComboName = "Office Combo",
                    Quantity = 1,
                    UnitPrice = 200m,
                    DiscountPercent = 5m,
                    FinalUnitPrice = 190m,
                    LineTotal = 190m
                }
            }
        };

        var cartRepo = new InMemoryCartRepository(new List<Cart> { existingCart });
        var comboRepo = new InMemoryRepository<Combo>();
        var promoRepo = new InMemoryRepository<Promotion>();
        var service = new CartComboService(cartRepo, comboRepo, promoRepo);

        var result = await service.GetCartByUserIdAsync("user-2");

        Assert.Equal("user-2", result.UserId);
        Assert.Single(result.Items);
        Assert.Equal(200m, result.SubTotal);
        Assert.Equal(10m, result.DiscountTotal);
        Assert.Equal(190m, result.GrandTotal);
        Assert.Equal(4, result.Version);
    }

    [Fact]
    public async Task UpdateComboAsync_ShouldRejectExpiredPromotion()
    {
        var cartRepo = new InMemoryCartRepository(new List<Cart>
        {
            new()
            {
                Id = "cart-2",
                UserId = "user-3",
                Version = 1,
                ComboItems = new List<CartComboItem>
                {
                    new()
                    {
                        ComboId = "combo-3",
                        ComboName = "Gaming Combo",
                        Quantity = 1,
                        UnitPrice = 300m,
                        DiscountPercent = 0m,
                        FinalUnitPrice = 300m,
                        LineTotal = 300m
                    }
                }
            }
        });
        var comboRepo = new InMemoryRepository<Combo>(new List<Combo>
        {
            new() { Id = "combo-3", Name = "Gaming Combo", TotalPrice = 300m }
        });
        var promoRepo = new InMemoryRepository<Promotion>(new List<Promotion>
        {
            new()
            {
                Id = "promo-expired",
                Name = "Expired Promo",
                DiscountPercent = 15m,
                StartAt = DateTime.UtcNow.AddDays(-5),
                EndAt = DateTime.UtcNow.AddDays(-1),
                Status = "Active"
            }
        });
        var service = new CartComboService(cartRepo, comboRepo, promoRepo);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateComboAsync("user-3", "combo-3", new UpdateCartComboRequest
            {
                Quantity = 1,
                PromotionId = "promo-expired",
                ExpectedVersion = 1
            }));

        Assert.Contains("Invalid or expired promotion", ex.Message);
    }

    [Fact]
    public async Task UpdateComboAsync_ShouldThrowConcurrencyException_WhenExpectedVersionMismatch()
    {
        var cartRepo = new InMemoryCartRepository(new List<Cart>
        {
            new()
            {
                Id = "cart-concurrency",
                UserId = "user-concurrency",
                Version = 3,
                ComboItems = new List<CartComboItem>
                {
                    new()
                    {
                        ComboId = "combo-concurrency",
                        ComboName = "Concurrency Combo",
                        Quantity = 1,
                        UnitPrice = 100m,
                        DiscountPercent = 0m,
                        FinalUnitPrice = 100m,
                        LineTotal = 100m
                    }
                }
            }
        });
        var comboRepo = new InMemoryRepository<Combo>(new List<Combo>
        {
            new() { Id = "combo-concurrency", Name = "Concurrency Combo", TotalPrice = 100m }
        });
        var promoRepo = new InMemoryRepository<Promotion>();
        var service = new CartComboService(cartRepo, comboRepo, promoRepo);

        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            service.UpdateComboAsync("user-concurrency", "combo-concurrency", new UpdateCartComboRequest
            {
                Quantity = 2,
                ExpectedVersion = 2,
                UseBestPromotion = false
            }));
    }

    [Fact]
    public async Task RemoveComboAsync_ShouldRemoveComboLine()
    {
        var cartRepo = new InMemoryCartRepository(new List<Cart>
        {
            new()
            {
                Id = "cart-3",
                UserId = "user-4",
                Version = 2,
                ComboItems = new List<CartComboItem>
                {
                    new()
                    {
                        ComboId = "combo-4",
                        ComboName = "Travel Combo",
                        Quantity = 2,
                        UnitPrice = 120m,
                        DiscountPercent = 0m,
                        FinalUnitPrice = 120m,
                        LineTotal = 240m
                    }
                }
            }
        });
        var comboRepo = new InMemoryRepository<Combo>();
        var promoRepo = new InMemoryRepository<Promotion>();
        var service = new CartComboService(cartRepo, comboRepo, promoRepo);

        var result = await service.RemoveComboAsync("user-4", "combo-4", expectedVersion: 2);

        Assert.Empty(result.Items);
        Assert.Equal(0m, result.SubTotal);
        Assert.Equal(0m, result.DiscountTotal);
        Assert.Equal(0m, result.GrandTotal);
        Assert.Equal(3, result.Version);
    }

    private sealed class InMemoryCartRepository : ICartRepository
    {
        private readonly List<Cart> _carts;

        public InMemoryCartRepository(List<Cart>? seed = null)
        {
            _carts = seed ?? new List<Cart>();
        }

        public Task<List<Cart>> GetAllAsync()
        {
            return Task.FromResult(_carts.ToList());
        }

        public Task<Cart> GetByIdAsync(string id)
        {
            var cart = _carts.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(cart!);
        }

        public Task CreateAsync(Cart entity)
        {
            entity.Id ??= Guid.NewGuid().ToString();
            _carts.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Cart entity)
        {
            var index = _carts.FindIndex(x => x.Id == id);
            if (index >= 0)
            {
                _carts[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _carts.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }

        public Task<Cart> GetByUserIdAsync(string userId)
        {
            var cart = _carts.FirstOrDefault(x => x.UserId == userId);
            return Task.FromResult(cart!);
        }
    }

    private sealed class InMemoryRepository<T> : IRepository<T>
        where T : class
    {
        private readonly List<T> _items;

        public InMemoryRepository(List<T>? seed = null)
        {
            _items = seed ?? new List<T>();
        }

        public Task<List<T>> GetAllAsync()
        {
            return Task.FromResult(_items.ToList());
        }

        public Task<T> GetByIdAsync(string id)
        {
            var item = _items.FirstOrDefault(x => GetId(x) == id);
            return Task.FromResult(item!);
        }

        public Task CreateAsync(T entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, T entity)
        {
            var index = _items.FindIndex(x => GetId(x) == id);
            if (index >= 0)
            {
                _items[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _items.RemoveAll(x => GetId(x) == id);
            return Task.CompletedTask;
        }

        private static string? GetId(T item)
        {
            return item.GetType().GetProperty("Id")?.GetValue(item)?.ToString();
        }
    }
}
