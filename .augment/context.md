# PushDashboard Project Context

## ?? **PROJECT OVERVIEW**

**PushDashboard** is a multi-tenant SaaS platform providing e-commerce businesses with marketing automation tools and customer engagement modules.

### **Core Purpose**
- Automate customer communications for e-commerce businesses
- Provide modular marketing tools (Birthday reminders, Gift wheels, Social proof, etc.)
- Integrate with multiple e-commerce platforms (Ticimax, Ikas, Shopify)
- Offer multi-channel notifications (Email, SMS, WhatsApp)

### **Technology Stack**
- **Backend**: ASP.NET Core MVC (.NET 8)
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: ASP.NET Core Identity + Cookie Authentication
- **Frontend**: Razor Views + Tailwind CSS + JavaScript
- **Real-time**: SignalR Hubs

## ??? **MULTI-TENANT ARCHITECTURE**

### **Tenant Isolation**
- **Company-based tenancy**: Every entity has `CompanyId`
- **Row-level security**: All queries filtered by `CompanyId`
- **User context**: Custom middleware populates user context
- **Data separation**: No cross-company data access allowed

### **User Context Service**
```csharp
public interface IUserContextService
{
    string? UserId { get; }
    Guid? CompanyId { get; }
    ApplicationUser? User { get; }
    bool IsAuthenticated { get; }
}
```

## ?? **DATABASE STRUCTURE**

### **Core Entities**
- **Company**: Tenant root entity
- **ApplicationUser**: Users belong to companies
- **CompanyModule**: Activated modules per company
- **CompanyIntegration**: E-commerce platform connections
- **Customer**: Synced from e-commerce platforms
- **Basket**: Shopping cart data
- **ModuleUsageLog**: Billing and usage tracking

### **Module Entities**
Each module has dedicated entities:
- **Birthday Module**: Birthday notifications and settings
- **Gift Wheel Module**: Wheel configuration, prizes, spins
- **Social Proof Module**: Active user display settings
- **Comment Scraper**: Trendyol review scraping
- **First Order Module**: New customer notifications

## ?? **EXISTING MODULES**

### **1. Birthday Module**
- **Purpose**: Send birthday greetings to customers
- **Features**: Automatic detection, multi-channel notifications, voucher creation
- **Location**: `Services/Modules/Birthday/`

### **2. Gift Wheel Module**
- **Purpose**: Interactive prize wheel for customer engagement
- **Features**: Customizable wheel, prize management, voucher generation
- **Location**: `Services/Modules/GiftWheel/`

### **3. Social Proof Module**
- **Purpose**: Display active user count on product pages
- **Features**: Configurable display, random number generation, JavaScript widget
- **Location**: `Services/SocialProofService.cs`

### **4. Comment Scraper Module**
- **Purpose**: Scrape and transfer Trendyol reviews
- **Features**: Bulk scraping, comment transfer, job management
- **Location**: `Services/TrendyolScraperService.cs`

### **5. First Order Module**
- **Purpose**: Welcome new customers with special offers
- **Features**: First order detection, automated notifications
- **Location**: `Services/Modules/FirstOrder/`

## ?? **INTEGRATION SYSTEM**

### **E-commerce Platforms**
- **Ticimax**: SOAP-based integration (customers, baskets, orders, vouchers)
- **Ikas**: REST API integration (planned)
- **Shopify**: REST API integration (planned)

### **Notification Channels**
- **Email**: SMTP-based delivery
- **SMS**: Third-party provider integration
- **WhatsApp**: WhatsApp Business API

### **Integration Factory Pattern**
```csharp
public class EcommerceServiceFactory
{
    public IEcommerceService CreateService(CompanyIntegration integration)
    {
        return integration.Integration.Name switch
        {
            "Ticimax" => new TicimaxService(integration),
            "Ikas" => new IkasService(integration),
            _ => throw new NotSupportedException()
        };
    }
}
```

## ?? **BILLING SYSTEM**

### **Credit-Based Model**
- Companies purchase credits
- Module operations consume credits
- Usage tracking via `ModuleUsageLog`
- Real-time balance checking

### **Cost Structure**
- Notification sending: 1-2 credits per message
- Voucher creation: 2-5 credits per voucher
- Data synchronization: Free

## ?? **FRONTEND ARCHITECTURE**

### **Design System**
- **CSS Framework**: Tailwind CSS
- **Components**: Reusable partial views
- **Interactions**: AJAX-based operations
- **Real-time**: SignalR for progress updates

### **View Organization**
```
Views/ControllerName/
+-- Index.cshtml
+-- Partials/
¦   +-- _ComponentName.cshtml
¦   +-- _ModalName.cshtml
¦   +-- _FormName.cshtml
```

## ?? **SECURITY & PERMISSIONS**

### **Authentication**
- ASP.NET Core Identity with cookie authentication
- Company-based user isolation
- Role-based permissions (CompanyOwner, User)

### **Authorization**
- All controllers require authentication via `[Authorize]`
- Company data isolation enforced at query level
- Module access based on company activation

## ?? **PERFORMANCE PATTERNS**

### **Database Optimization**
- Proper indexing on `CompanyId` columns
- Pagination for large datasets
- `AsNoTracking()` for read-only queries
- Connection pooling

### **Caching Strategy**
- In-memory caching for frequently accessed data
- User context caching via middleware

## ?? **DEVELOPMENT WORKFLOW**

### **Code Organization**
- Feature-based folder structure
- Separation of concerns (Controllers, Services, DTOs)
- Interface-based dependency injection
- Configuration-based settings

### **Error Handling**
- Structured logging with context
- User-friendly Turkish error messages
- Technical error logging for debugging
- Graceful degradation for non-critical failures

## ?? **BUSINESS RULES**

### **Multi-Tenancy Rules**
- Every database query must filter by `CompanyId`
- Users can only access their company's data
- Module activation is per-company
- Billing is per-company

### **Module Development & Store Rules**
- **Module Store Integration**: Every new module must be added to the module store
- **Purchase Flow**: Modules must be purchasable through the store system
- **Menu Visibility**: Modules appear in navigation menu only after purchase/activation
- **Settings Location**: Core module settings (active/passive status) managed from store settings
- **Module Dependencies**: Modules can require e-commerce integration or other modules
- **Module Usage Tracking**: All module operations are tracked and billed

### **E-commerce Integration Rules**
- **Factory Pattern**: Use factory pattern for e-commerce platform selection
- **Dynamic Routing**: Module operations route to appropriate platform class based on company's selected integration
- **API Authentication**: E-commerce API keys retrieved from integration settings (like Ticimax integration settings)
- **Platform Abstraction**: Modules work with any e-commerce platform through common interface

### **Integration Rules**
- Companies can have one active e-commerce integration
- Integration settings are encrypted and secure
- Data synchronization respects company boundaries
- Webhook processing validates company ownership

## ?? **TYPICAL DATA FLOW**

### **Module Operation Flow**
1. User accesses module page
2. System validates company access and module activation
3. Module loads company-specific data and settings
4. User performs operations (creates campaigns, sends notifications)
5. System tracks usage and deducts credits
6. Results are logged and displayed to user

### **Integration Data Flow**
1. External system sends webhook or API call
2. System validates company and integration
3. Data is processed and stored with `CompanyId`
4. Relevant modules are triggered if needed
5. Notifications are sent if configured
6. Usage is tracked and billed

## ?? **FOLDER STRUCTURE**

```
PushDashboard/
+-- Controllers/           # MVC Controllers (inherit BaseController)
+-- Services/             # Business Logic Services
¦   +-- Modules/         # Module-specific services
¦   +-- Integrations/    # E-commerce platform integrations
¦   +-- Notifications/   # Notification channel services
¦   +-- WhatsApp/       # WhatsApp-specific services
+-- Models/              # Entity Framework Models
+-- DTOs/               # Data Transfer Objects
+-- ViewModels/         # View-specific models
+-- Views/              # Razor Views with Partials
+-- Data/               # DbContext, Configurations, Seeders
+-- Middleware/         # Custom middleware
+-- Hubs/              # SignalR Hubs
+-- wwwroot/           # Static files
```

This context helps AI assistants understand the project's architecture, business logic, and technical requirements when working on PushDashboard.

 
 # #   =Ø»Ü  * * C O D I N G   S T A N D A R D S * *  
 -   * * C #   C o d e * * :   U s e   P a s c a l C a s e   f o r   a l l   e l e m e n t s  
 -   * * J a v a S c r i p t   C o d e * * :   U s e   c a m e l C a s e   f o r   a l l   e l e m e n t s  
 -   * * U s e r   E x p e r i e n c e * * :   U s e   A J A X   f o r   a l l   p o s t - l o a d   o p e r a t i o n s  
 