using swd.Application.DTOs.Order;

public interface ICheckoutService
{
    Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request);
}