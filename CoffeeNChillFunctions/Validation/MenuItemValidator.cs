using CoffeeNChillFunctions.DTOs;

namespace CoffeeNChillFunctions.Validation
{
    public static class MenuItemValidator
    {
        private static readonly char[] InvalidTableKeyCharacters =
        {
            '/',
            '\\',
            '#',
            '?'
        };

        public static List<string> ValidateCreate(
            CreateMenuItemDto input)
        {
            List<string> errors = new();

            if (string.IsNullOrWhiteSpace(input.Category))
            {
                errors.Add("Category is required.");
            }
            else if (ContainsInvalidKeyCharacters(input.Category))
            {
                errors.Add(
                    "Category cannot contain /, \\, # or ? characters.");
            }

            if (string.IsNullOrWhiteSpace(input.Id))
            {
                errors.Add("ID is required.");
            }
            else if (ContainsInvalidKeyCharacters(input.Id))
            {
                errors.Add(
                    "ID cannot contain /, \\, # or ? characters.");
            }

            if (string.IsNullOrWhiteSpace(input.Name))
            {
                errors.Add("Name is required.");
            }
            else if (input.Name.Trim().Length > 100)
            {
                errors.Add("Name cannot exceed 100 characters.");
            }

            if (input.Description != null &&
                input.Description.Trim().Length > 500)
            {
                errors.Add(
                    "Description cannot exceed 500 characters.");
            }

            if (input.Price < 0)
            {
                errors.Add("Price cannot be negative.");
            }

            if (double.IsNaN(input.Price) ||
                double.IsInfinity(input.Price))
            {
                errors.Add("Price must be a valid number.");
            }

            return errors;
        }

        public static List<string> ValidateUpdate(
            UpdateMenuItemDto input)
        {
            List<string> errors = new();

            if (!input.Price.HasValue &&
                !input.IsAvailable.HasValue)
            {
                errors.Add(
                    "Provide a price, availability status, or both.");
            }

            if (input.Price.HasValue &&
                input.Price.Value < 0)
            {
                errors.Add("Price cannot be negative.");
            }

            if (input.Price.HasValue &&
                (double.IsNaN(input.Price.Value) ||
                 double.IsInfinity(input.Price.Value)))
            {
                errors.Add("Price must be a valid number.");
            }

            return errors;
        }

        private static bool ContainsInvalidKeyCharacters(
            string value)
        {
            return value.IndexOfAny(
                InvalidTableKeyCharacters) >= 0;
        }
    }
}
