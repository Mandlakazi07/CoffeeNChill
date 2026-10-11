using CoffeeNChillFunctions.DTOs;

namespace CoffeeNChillFunctions.Validation
{
    public static class OrderRequestValidator
    {
        public static List<string> Validate(OrderRequest request)
        {
            List<string> errors = new();

            if (request == null)
            {
                errors.Add("The order request is required.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(request.OrderId))
            {
                errors.Add("OrderId is required.");
            }

            if (string.IsNullOrWhiteSpace(request.CustomerName))
            {
                errors.Add("CustomerName is required.");
            }

            if (request.SelectedItemSkus == null ||
                request.SelectedItemSkus.Count == 0)
            {
                errors.Add("At least one menu item SKU is required.");
            }
            else if (request.SelectedItemSkus.Any(
                sku => string.IsNullOrWhiteSpace(sku)))
            {
                errors.Add("SelectedItemSkus cannot contain empty values.");
            }

            if (request.TotalPrice <= 0)
            {
                errors.Add("TotalPrice must be greater than zero.");
            }

            return errors;
        }
    }
}