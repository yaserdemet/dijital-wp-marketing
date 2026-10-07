# PushDashboard Project Context for AI Assistants

## ?? **PROJECT OVERVIEW**

**PushDashboard** is a multi-tenant SaaS platform that provides e-commerce businesses with marketing automation tools and customer engagement modules.

### **Core Purpose**
- Help e-commerce businesses automate customer communications
- Provide modular marketing tools (Birthday reminders, Gift wheels, Social proof, etc.)
- Integrate with multiple e-commerce platforms (Ticimax, Ikas, Shopify)
- Offer multi-channel notifications (Email, SMS, WhatsApp)

### **Business Model**
- Multi-tenant SaaS with company-based isolation
- Credit-based billing system for module usage
- Modular architecture - companies activate needed modules
- Integration-based data synchronization

## ??? **TECHNICAL ARCHITECTURE**

### **Technology Stack**
- **Backend**: ASP.NET Core MVC (.NET 8)
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: ASP.NET Core Identity + Cookie Authentication
- **Frontend**: Razor Views + Tailwind CSS + JavaScript
- **Real-time**: SignalR Hubs
- **External APIs**: SOAP/REST integrations with e-commerce platforms

### **Multi-Tenant Design**
- **Tenant Isolation**: Company-based (every entity has CompanyId)
- **User Context**: Custom middleware populates user context
- **Data Security**: Row-level security via CompanyId filtering
- **Module System**: Per-company module activation and billing

## ?? **DATABASE STRUCTURE**

### **Core Entities**
```
Company (Tenant root)
+-- ApplicationUser (Users belong to companies)
+-- CompanyModule (Activated modules per company)
+-- CompanyIntegration (E-commerce platform connections)
+-- Customer (Synced from e-commerce platforms)
+-- Basket (Shopping cart data)
+-- ModuleUsageLog (Billing and usage tracking)
```

### **Module Entities**
Each module has its own entities:
- **Birthday Module**: Birthday notifications and settings
- **Gift Wheel Module**: Wheel configuration, prizes, spins
- **Social Proof Module**: Active user display settings
- **Comment Scraper**: Trendyol review scraping
- **First Order Module**: New customer notifications

## ?? **EXISTING MODULES**

### **1. Birthday Module**
- **Purpose**: Send birthday greetings to customers
- **Features**: Automatic birthday detection, multi-channel notifications, gift voucher creation
- **Integration**: Requires e-commerce platform for customer data

### **2. Gift Wheel Module** 
- **Purpose**: Interactive prize wheel for customer engagement
- **Features**: Customizable wheel, prize management, voucher generation, WhatsApp notifications
- **Integration**: Embeddable JavaScript widget for e-commerce sites

### **3. Social Proof Module**
- **Purpose**: Display active user count on product pages
- **Features**: Configurable display settings, random number generation, JavaScript widget
- **Integration**: Embeddable script for product detail pages

### **4. Comment Scraper Module**
- **Purpose**: Scrape and transfer Trendyol reviews to e-commerce sites
- **Features**: Bulk product scraping, comment transfer, job management
- **Integration**: External scraping service + webhook processing

### **5. First Order Module**
- **Purpose**: Welcome new customers with special offers
- **Features**: First order detection, automated notifications, voucher creation
- **Integration**: Webhook-based order processing

### **6. Order Status Notifications**
- **Purpose**: Notify customers about order status changes
- **Features**: Status mapping, template management, multi-channel delivery
- **Integration**: Webhook-based order updates

## ?? **INTEGRATION SYSTEM**

### **E-commerce Platforms**
- **Ticimax**: SOAP-based integration (customers, baskets, orders, vouchers)
- **Ikas**: REST API integration (planned)
- **Shopify**: REST API integration (planned)

### **Notification Channels**
- **Email**: SMTP-based email delivery
- **SMS**: Third-party SMS provider integration
- **WhatsApp**: WhatsApp Business API integration

### **Integration Pattern**
```csharp
// Factory pattern for multiple platforms
var ecommerceService = _factory.CreateService(integration.Platform, integration);
var customers = await ecommerceService.GetCustomersAsync();
```

## ?? **BILLING & USAGE SYSTEM**

### **Credit-Based Model**
- Companies purchase credits
- Module operations consume credits
- Usage tracking via ModuleUsageLog
- Real-time balance checking

### **Cost Structure**
- Notification sending: 1-2 credits per message
- Voucher creation: 2-5 credits per voucher
- Data synchronization: Free
- Module activation: One-time or subscription

## ?? **USER INTERFACE PATTERNS**

### **Design System**
- **CSS Framework**: Tailwind CSS
- **Components**: Reusable partial views
- **Interactions**: AJAX-based operations
- **Real-time**: SignalR for progress updates

### **User Experience**
- **Dashboard**: Overview of all modules and statistics
- **Module Pages**: Dedicated pages for each module
- **Settings**: Company and integration management
- **Responsive**: Mobile-friendly design

## ?? **SECURITY & PERMISSIONS**

### **Authentication**
- ASP.NET Core Identity with cookie authentication
- Company-based user isolation
- Role-based permissions (CompanyOwner, User)

### **Authorization**
- All controllers require authentication
- Company data isolation enforced at query level
- Module access based on company activation

### **Data Protection**
- Company data never crosses tenant boundaries
- Secure API endpoints with rate limiting
- Input validation and sanitization

## ?? **DEPLOYMENT & ENVIRONMENT**

### **Development Environment**
- Visual Studio / Rider IDE
- PostgreSQL database
- Local development with hot reload

### **Production Considerations**
- Docker containerization
- Environment-specific configurations
- Database migrations
- Health checks and monitoring

## ?? **PERFORMANCE CONSIDERATIONS**

### **Database Optimization**
- Proper indexing on CompanyId columns
- Pagination for large datasets
- AsNoTracking for read-only queries
- Connection pooling

### **Caching Strategy**
- In-memory caching for frequently accessed data
- User context caching via middleware
- Static file caching

### **Background Processing**
- Background services for long-running tasks
- SignalR for progress updates
- Job queuing for bulk operations

## ?? **TESTING APPROACH**

### **Testing Strategy**
- Unit tests for service layer
- Integration tests for controllers
- Mock external dependencies
- Test data isolation per company

## ?? **DEVELOPMENT WORKFLOW**

### **Code Organization**
- Feature-based folder structure
- Separation of concerns (Controllers, Services, DTOs)
- Interface-based dependency injection
- Configuration-based settings

### **Error Handling**
- Structured logging with context
- User-friendly error messages in Turkish
- Technical error logging for debugging
- Graceful degradation for non-critical failures

## ?? **BUSINESS RULES**

### **Multi-Tenancy Rules**
- Every database query must filter by CompanyId
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

## ?? **DATA FLOW**

### **Typical Module Operation**
1. User accesses module page
2. System validates company access and module activation
3. Module loads company-specific data and settings
4. User performs operations (creates campaigns, sends notifications)
5. System tracks usage and deducts credits
6. Results are logged and displayed to user

### **Integration Data Flow**
1. External system sends webhook or API call
2. System validates company and integration
3. Data is processed and stored with CompanyId
4. Relevant modules are triggered if needed
5. Notifications are sent if configured
6. Usage is tracked and billed

This context should help AI assistants understand the project structure, business logic, and technical requirements when working on the PushDashboard project.

# #   =Ø»Ü  * * C O D I N G   S T A N D A R D S * *  
  
 # # #   * * N a m i n g   C o n v e n t i o n s * *  
 -   * * C #   C o d e * * :   U s e   P a s c a l C a s e   f o r   a l l   C #   e l e m e n t s   ( c l a s s e s ,   m e t h o d s ,   p r o p e r t i e s ,   e t c . )  
 -   * * J a v a S c r i p t   C o d e * * :   U s e   c a m e l C a s e   f o r   J a v a S c r i p t   v a r i a b l e s ,   f u n c t i o n s ,   a n d   p r o p e r t i e s  
 -   * * D a t a b a s e * * :   U s e   P a s c a l C a s e   f o r   t a b l e   a n d   c o l u m n   n a m e s  
 -   * * F i l e s * * :   U s e   P a s c a l C a s e   f o r   C #   f i l e s ,   c a m e l C a s e   f o r   J a v a S c r i p t   f i l e s  
  
 # # #   * * U s e r   E x p e r i e n c e   R u l e s * *  
 -   * * I n i t i a l   P a g e   L o a d * * :   L o a d   p a g e   s t r u c t u r e   f i r s t ,   t h e n   p o p u l a t e   w i t h   d a t a  
 -   * * D y n a m i c   O p e r a t i o n s * * :   U s e   A J A X   f o r   a l l   p o s t - l o a d   o p e r a t i o n s   t o   m a i n t a i n   h i g h   u s e r   e x p e r i e n c e  
 -   * * N o   P a g e   R e l o a d s * * :   A v o i d   f u l l   p a g e   r e l o a d s   a f t e r   i n i t i a l   l o a d  
 -   * * R e a l - t i m e   U p d a t e s * * :   U s e   S i g n a l R   f o r   p r o g r e s s   u p d a t e s   a n d   r e a l - t i m e   d a t a  
 -   * * L o a d i n g   S t a t e s * * :   S h o w   l o a d i n g   i n d i c a t o r s   d u r i n g   A J A X   o p e r a t i o n s  
  
 # # #   * * C o d e   E x a m p l e s * *  
 ` ` ` c s h a r p  
 / /   C #   -   P a s c a l C a s e  
 p u b l i c   c l a s s   M o d u l e S e r v i c e   :   I M o d u l e S e r v i c e  
 {  
         p u b l i c   a s y n c   T a s k < M o d u l e D t o >   G e t M o d u l e A s y n c ( i n t   m o d u l e I d )  
         {  
                 / /   I m p l e m e n t a t i o n  
         }  
 }  
 ` ` `  
  
 ` ` ` j a v a s c r i p t  
 / /   J a v a S c r i p t   -   c a m e l C a s e  
 f u n c t i o n   l o a d M o d u l e D a t a ( m o d u l e I d )   {  
         $ . a j a x ( {  
                 u r l :   ' / M o d u l e / G e t D a t a ' ,  
                 d a t a :   {   m o d u l e I d :   m o d u l e I d   } ,  
                 s u c c e s s :   f u n c t i o n ( r e s p o n s e )   {  
                         u p d a t e M o d u l e D i s p l a y ( r e s p o n s e ) ;  
                 }  
         } ) ;  
 }  
 ` ` `  
 