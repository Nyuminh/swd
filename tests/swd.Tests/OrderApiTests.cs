using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using swd.Application.DTOs.Order;
using swd.Application.Facades;
using swd.Application.Services;
using swd.Domain.Interfaces;
using swd.Presentation.Controllers;

namespace swd.Tests;

public class OrderApiTests
{
    [Fact]
    public void OrdersController_ShouldRequireAuthorization()
    {
        Assert.NotNull(typeof(OrdersController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void OrdersController_AdminEndpoints_ShouldRequireAdminOrStaffRole()
    {
        Assert.Equal("Admin,Staff", typeof(OrdersController).GetMethod(nameof(OrdersController.GetAllOrders))?.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.Equal("Admin,Staff", typeof(OrdersController).GetMethod(nameof(OrdersController.UpdateOrder))?.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
        Assert.Equal("Admin,Staff", typeof(OrdersController).GetMethod(nameof(OrdersController.DeleteOrder))?.GetCustomAttribute<AuthorizeAttribute>()?.Roles);
    }

    [Fact]
    public async Task CheckoutFacade_ShouldRejectNonPositiveQuantity()
    {
        var productRepository = new InMemoryProductRepository(new List<Product>
        {
            new()
            {
                Id = "product-1",
                Name = "Frame A",
                Price = 100m,
                Inventory = new InventoryInfo { Quantity = 5 },
                Warranty = new WarrantyInfo { Months = 12 }
            }
        });
        var orderRepository = new InMemoryOrderRepository();
        var facade = new CheckoutFacade(productRepository, orderRepository, new InMemoryRepository<Promotion>());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => facade.PlaceOrder(new CheckoutRequest
        {
            UserId = "user-1",
            Items = new List<CheckoutItemRequest>
            {
                new() { ProductId = "product-1", Quantity = 0 }
            },
            Shipping = new CheckoutShippingRequest { FullName = "Test", Address = "Test", Phone = "0909" },
            PaymentMethod = "COD"
        }));

        Assert.Contains("Quantity", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckoutFacade_ShouldRestoreInventory_WhenOrderCreationFails()
    {
        var productRepository = new InMemoryProductRepository(new List<Product>
        {
            new()
            {
                Id = "product-2",
                Name = "Frame B",
                Price = 150m,
                Inventory = new InventoryInfo { Quantity = 5 },
                Warranty = new WarrantyInfo { Months = 12 }
            }
        });
        var orderRepository = new InMemoryOrderRepository { FailOnCreate = true };
        var facade = new CheckoutFacade(productRepository, orderRepository, new InMemoryRepository<Promotion>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => facade.PlaceOrder(new CheckoutRequest
        {
            UserId = "user-1",
            Items = new List<CheckoutItemRequest>
            {
                new() { ProductId = "product-2", Quantity = 2 }
            },
            Shipping = new CheckoutShippingRequest { FullName = "Test", Address = "Test", Phone = "0909" },
            PaymentMethod = "COD"
        }));

        var product = await productRepository.GetByIdAsync("product-2");
        Assert.Equal(5, product.Inventory.Quantity);
    }

    [Fact]
    public async Task Checkout_ShouldUseAuthenticatedUserId_InsteadOfBodyUserId()
    {
        var productRepository = new InMemoryProductRepository(new List<Product>
        {
            new()
            {
                Id = "product-3",
                Name = "Frame C",
                Price = 90m,
                Inventory = new InventoryInfo { Quantity = 3 },
                Warranty = new WarrantyInfo { Months = 6 }
            }
        });
        var orderRepository = new InMemoryOrderRepository();
        var controller = CreateController(
            new CheckoutFacade(productRepository, orderRepository, new InMemoryRepository<Promotion>()),
            new OrderService(orderRepository),
            userId: "claim-user",
            role: "Customer");

        var result = await controller.Checkout(new CheckoutRequest
        {
            UserId = "body-user",
            Items = new List<CheckoutItemRequest>
            {
                new() { ProductId = "product-3", Quantity = 1 }
            },
            Shipping = new CheckoutShippingRequest { FullName = "Test", Address = "Test", Phone = "0909" },
            PaymentMethod = "COD"
        });

        Assert.IsType<OkObjectResult>(result);
        var createdOrder = Assert.Single(orderRepository.Items);
        Assert.Equal("claim-user", createdOrder.UserId);
    }

    [Fact]
    public async Task GetOrdersByUser_ShouldReturnForbid_WhenCustomerRequestsAnotherUsersOrders()
    {
        var controller = CreateController(
            new CheckoutFacade(new InMemoryProductRepository(), new InMemoryOrderRepository(), new InMemoryRepository<Promotion>()),
            new OrderService(new InMemoryOrderRepository()),
            userId: "user-1",
            role: "Customer");

        var result = await controller.GetOrdersByUser("user-2");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnForbid_WhenCustomerRequestsAnotherUsersOrder()
    {
        var orderRepository = new InMemoryOrderRepository(new List<Order>
        {
            new()
            {
                Id = "order-1",
                UserId = "owner-user",
                Status = "Pending",
                TotalAmount = 100m,
                Shipping = new ShippingInfo(),
                Payment = new PaymentInfo(),
                Items = new List<OrderItem>()
            }
        });
        var controller = CreateController(
            new CheckoutFacade(new InMemoryProductRepository(), orderRepository, new InMemoryRepository<Promotion>()),
            new OrderService(orderRepository),
            userId: "another-user",
            role: "Customer");

        var result = await controller.GetOrderById("order-1");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetOrderByIdAsync_ShouldExposeShippingCarrierAndTimeline()
    {
        var shippedAt = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        var deliveredAt = new DateTime(2026, 3, 11, 9, 30, 0, DateTimeKind.Utc);
        var orderRepository = new InMemoryOrderRepository(new List<Order>
        {
            new()
            {
                Id = "order-2",
                UserId = "user-2",
                Status = "Shipped",
                TotalAmount = 220m,
                Items = new List<OrderItem>(),
                Shipping = new ShippingInfo
                {
                    FullName = "Test User",
                    Address = "123 Street",
                    Phone = "0909",
                    Carrier = "GHN",
                    Method = "Standard",
                    Fee = 30m,
                    Status = "InTransit",
                    ShippedAt = shippedAt,
                    DeliveredAt = deliveredAt
                },
                Payment = new PaymentInfo
                {
                    Method = "COD",
                    Status = "Pending"
                }
            }
        });
        var service = new OrderService(orderRepository);

        var response = await service.GetOrderByIdAsync("order-2");
        var shipping = Assert.IsType<ShippingInfoDto>(response.Shipping);
        var shippingType = typeof(ShippingInfoDto);

        Assert.NotNull(shippingType.GetProperty("Carrier"));
        Assert.NotNull(shippingType.GetProperty("ShippedAt"));
        Assert.NotNull(shippingType.GetProperty("DeliveredAt"));
        Assert.Equal("GHN", shippingType.GetProperty("Carrier")?.GetValue(shipping));
        Assert.Equal(shippedAt, shippingType.GetProperty("ShippedAt")?.GetValue(shipping));
        Assert.Equal(deliveredAt, shippingType.GetProperty("DeliveredAt")?.GetValue(shipping));
    }

    private static OrdersController CreateController(
        CheckoutFacade checkoutFacade,
        OrderService orderService,
        string userId,
        string role)
    {
        var controller = new OrdersController(checkoutFacade, orderService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, userId),
                        new Claim(ClaimTypes.Role, role)
                    }, "TestAuth"))
                }
            }
        };

        return controller;
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products;

        public InMemoryProductRepository(List<Product>? seed = null)
        {
            _products = seed ?? new List<Product>();
        }

        public Task<List<Product>> GetAllAsync()
        {
            return Task.FromResult(_products.ToList());
        }

        public Task<Product> GetByIdAsync(string id)
        {
            return Task.FromResult(_products.FirstOrDefault(x => x.Id == id)!);
        }

        public Task CreateAsync(Product entity)
        {
            _products.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Product entity)
        {
            var index = _products.FindIndex(x => x.Id == id);
            if (index >= 0)
            {
                _products[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _products.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }

        public Task<List<Product>> GetByCategoryAsync(string categoryId)
        {
            return Task.FromResult(_products.Where(x => x.CategoryId == categoryId).ToList());
        }

        public Task<bool> TryReserveInventoryAsync(string id, int quantity)
        {
            var product = _products.FirstOrDefault(x => x.Id == id);
            if (product is null || product.Inventory is null || product.Inventory.Quantity < quantity)
            {
                return Task.FromResult(false);
            }

            product.Inventory.Quantity -= quantity;
            return Task.FromResult(true);
        }

        public Task ReleaseInventoryAsync(string id, int quantity)
        {
            var product = _products.FirstOrDefault(x => x.Id == id);
            if (product?.Inventory is not null)
            {
                product.Inventory.Quantity += quantity;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders;

        public InMemoryOrderRepository(List<Order>? seed = null)
        {
            _orders = seed ?? new List<Order>();
        }

        public bool FailOnCreate { get; set; }

        public IReadOnlyList<Order> Items => _orders;

        public Task<List<Order>> GetAllAsync()
        {
            return Task.FromResult(_orders.ToList());
        }

        public Task<Order> GetByIdAsync(string id)
        {
            return Task.FromResult(_orders.FirstOrDefault(x => x.Id == id)!);
        }

        public Task CreateAsync(Order entity)
        {
            if (FailOnCreate)
            {
                throw new InvalidOperationException("Create failed.");
            }

            entity.Id ??= Guid.NewGuid().ToString();
            _orders.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, Order entity)
        {
            var index = _orders.FindIndex(x => x.Id == id);
            if (index >= 0)
            {
                _orders[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _orders.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }

        public Task<List<Order>> GetByUserAsync(string userId)
        {
            return Task.FromResult(_orders.Where(x => x.UserId == userId).ToList());
        }

        public Task<List<Order>> GetByStatusAsync(string status)
        {
            return Task.FromResult(_orders.Where(x => x.Status == status).ToList());
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
            var item = _items.FirstOrDefault(x => x?.GetType().GetProperty("Id")?.GetValue(x)?.ToString() == id);
            return Task.FromResult(item!);
        }

        public Task CreateAsync(T entity)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string id, T entity)
        {
            var index = _items.FindIndex(x => x?.GetType().GetProperty("Id")?.GetValue(x)?.ToString() == id);
            if (index >= 0)
            {
                _items[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id)
        {
            _items.RemoveAll(x => x?.GetType().GetProperty("Id")?.GetValue(x)?.ToString() == id);
            return Task.CompletedTask;
        }
    }
}
