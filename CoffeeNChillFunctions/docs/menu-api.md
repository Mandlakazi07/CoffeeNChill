# CoffeeNChill Menu API

## Overview

The CoffeeNChill Menu API uses HTTP-triggered Azure Functions and Azure Table Storage to manage canteen menu items.

Menu items are stored in the `MenuItems` table. The category is used as the `PartitionKey`, while the unique item SKU is used as the `RowKey`.

## Base URL

Local development URL:

```text
http://localhost:7071/api