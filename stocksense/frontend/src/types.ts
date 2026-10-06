export interface AuthInfo {
  token: string;
  email: string;
  shopName: string;
}

export interface Product {
  id: number;
  name: string;
  sku: string;
  category: string | null;
  costPrice: number;
  sellPrice: number;
  quantity: number;
  reorderLevel: number;
  isLowStock: boolean;
  updatedAt: string;
}

export interface ProductInput {
  name: string;
  sku: string;
  category: string;
  costPrice: number;
  sellPrice: number;
  quantity: number;
  reorderLevel: number;
}

export interface LowStockItem extends Product {
  suggestedReorderQuantity: number;
}

export interface StockMovement {
  id: number;
  change: number;
  reason: string;
  createdAt: string;
}

export interface DashboardSummary {
  totalProducts: number;
  totalUnits: number;
  inventoryCostValue: number;
  inventoryRetailValue: number;
  lowStockCount: number;
  outOfStockCount: number;
}
