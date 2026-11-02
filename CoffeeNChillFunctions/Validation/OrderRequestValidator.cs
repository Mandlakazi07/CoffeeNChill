using CoffeeNChillFunctions.DTOs;

namespace CoffeeNChillFunctions.Validation
{
    public static class OrderRequestValidator
    {
        public const int MaxOrderIdLength = 64;
        public const int MaxCustomerNameLength = 100;
        public const int MaxItems = 50;
        public const int MaxSkuLength = 64;
        public const decimal MaxTotalPrice = 100_000m;

        // Clock skew allowed between the client and the server.
        public static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(5);

        private static readonly DateTimeOffset EarliestOrderTimestamp =
            new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Azure Table Storage does not allow these in PartitionKey/RowKey.
        private static readonly char[] InvalidKeyCharacters =
        {
            '/',
            '\\',
            '#',
            '?'
        };

        public static List<string> Validate(OrderRequest? request)
        {
            return Validate(request, DateTimeOffset.UtcNow);
        }

        public static List<string> Validate(
            OrderRequest? request,
            DateTimeOffset utcNow)
        {
            List<string> errors = new();

            if (request == null)
            {
                errors.Add("The order request is required.");
                return errors;
            }

            ValidateOrderId(request.OrderId, errors);
            ValidateCustomerName(request.CustomerName, errors);
            ValidateSkus(request.SelectedItemSkus, errors);
            ValidateTotalPrice(request.TotalPrice, errors);
            ValidateTimestamp(request.OrderTimestamp, utcNow, errors);

            return errors;
        }

        private static void ValidateOrderId(string? orderId, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(orderId))
            {
                errors.Add("OrderId is required.");
                return;
            }

            string trimmed = orderId.Trim();

            if (trimmed.Length > MaxOrderIdLength)
            {
                errors.Add($"OrderId cannot exceed {MaxOrderIdLength} characters.");
            }

            if (ContainsInvalidKeyCharacters(trimmed))
            {
                errors.Add("OrderId cannot contain /, \\, #, ? or control characters.");
            }
        }

        private static void ValidateCustomerName(string? customerName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                errors.Add("CustomerName is required.");
                return;
            }

            string trimmed = customerName.Trim();

            if (trimmed.Length > MaxCustomerNameLength)
            {
                errors.Add($"CustomerName cannot exceed {MaxCustomerNameLength} characters.");
            }

            if (trimmed.Any(char.IsControl))
            {
                errors.Add("CustomerName cannot contain control characters.");
            }
        }

        private static void ValidateSkus(List<string?>? skus, List<string> errors)
        {
            if (skus == null || skus.Count == 0)
            {
                errors.Add("At least one menu item SKU is required.");
                return;
            }

            if (skus.Count > MaxItems)
            {
                errors.Add($"An order cannot contain more than {MaxItems} items.");
            }

            if (skus.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add("SelectedItemSkus cannot contain empty values.");
            }

            List<string> present = skus
                .Where(sku => !string.IsNullOrWhiteSpace(sku))
                .Select(sku => sku!.Trim())
                .ToList();

            if (present.Any(sku => sku.Length > MaxSkuLength))
            {
                errors.Add($"Each SKU cannot exceed {MaxSkuLength} characters.");
            }

            if (present.Any(ContainsInvalidKeyCharacters))
            {
                errors.Add("SKUs cannot contain /, \\, #, ? or control characters.");
            }
        }

        private static void ValidateTotalPrice(decimal? totalPrice, List<string> errors)
        {
            if (totalPrice == null)
            {
                errors.Add("TotalPrice is required.");
                return;
            }

            if (totalPrice.Value <= 0)
            {
                errors.Add("TotalPrice must be greater than zero.");
                return;
            }

            if (totalPrice.Value > MaxTotalPrice)
            {
                errors.Add($"TotalPrice cannot exceed {MaxTotalPrice:0.00}.");
            }

            if (decimal.Round(totalPrice.Value, 2) != totalPrice.Value)
            {
                errors.Add("TotalPrice cannot have more than two decimal places.");
            }
        }

        private static void ValidateTimestamp(
            DateTimeOffset? orderTimestamp,
            DateTimeOffset utcNow,
            List<string> errors)
        {
            // Optional: the producer stamps the current UTC time when absent.
            if (orderTimestamp == null)
            {
                return;
            }

            if (orderTimestamp.Value < EarliestOrderTimestamp)
            {
                errors.Add("OrderTimestamp must be on or after 2000-01-01.");
            }
            else if (orderTimestamp.Value > utcNow.Add(MaxFutureSkew))
            {
                errors.Add("OrderTimestamp cannot be in the future.");
            }
        }

        private static bool ContainsInvalidKeyCharacters(string value)
        {
            return value.IndexOfAny(InvalidKeyCharacters) >= 0 ||
                   value.Any(char.IsControl);
        }
    }
}
