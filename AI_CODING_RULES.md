# AI Coding Rules for PushDashboard Project

## ?? **MANDATORY RULES - NO EXCEPTIONS**

### **1. Multi-Tenant Data Isolation**
```csharp
// ? CORRECT - Always filter by CompanyId
var items = await _context.Items
    .Where(i => i.CompanyId == companyId)
    .ToListAsync();

// ? WRONG - Never query without company filter
var items = await _context.Items.ToListAsync();
```

### **2. Controller Inheritance**
```csharp
// ? CORRECT - Inherit from BaseController
[Authorize]
public class YourController : BaseController
{
    public YourController(IUserContextService userContextService) 
        : base(userContextService) { }
}

// ? WRONG - Don't inherit directly from Controller
public class YourController : Controller { }
```

### **3. Company Validation Pattern**
```csharp
// ? CORRECT - Always validate company access
public async Task<IActionResult> Index()
{
    var companyId = GetCurrentUserCompanyId();
    if (companyId == null)
    {
        TempData["ErrorMessage"] = "Sirket bilgisi bulunamadi.";
        return RedirectToAction("Index", "Home");
    }
    
    // Continue with logic...
}
```

### **4. Error Handling Pattern**
```csharp
// ? CORRECT - Always use try-catch with logging
try
{
    // Operation logic
    _logger.LogInformation("Operation completed for company {CompanyId}", companyId);
    return (true, "Islem basarili.");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error in operation for company {CompanyId}", companyId);
    return (false, "Islem sirasinda hata olustu.");
}
```

## ?? **NAMING CONVENTIONS**

### **Files & Folders**
- Controllers: `ModuleNameController.cs`
- Services: `IModuleNameService.cs`, `ModuleNameService.cs`
- DTOs: `ModuleNameDTOs.cs` (grouped in regions)
- ViewModels: `ModuleNameViewModels.cs`
- Views: `Views/ModuleName/Action.cshtml`
- Partials: `Views/ModuleName/Partials/_ComponentName.cshtml`

### **Namespaces**
```csharp
// Controllers
namespace PushDashboard.Controllers;

// Services
namespace PushDashboard.Services.Modules.ModuleName;

// DTOs
namespace PushDashboard.DTOs;

// ViewModels
namespace PushDashboard.ViewModels;
```

### **Method Naming**
```csharp
// Service methods
Task<(bool Success, string Message)> CreateAsync(Guid companyId, CreateDto dto, string userId);
Task<List<ItemDto>> GetListAsync(Guid companyId);
Task<ItemDto?> GetByIdAsync(Guid companyId, int id);
Task<(bool Success, string Message)> UpdateAsync(Guid companyId, int id, UpdateDto dto, string userId);
Task<(bool Success, string Message)> DeleteAsync(Guid companyId, int id, string userId);
```

## ?? **SERVICE PATTERNS**

### **Service Interface Template**
```csharp
namespace PushDashboard.Services.Modules.ModuleName;

public interface IModuleNameService
{
    #region CRUD Operations
    Task<(bool Success, string Message)> CreateAsync(Guid companyId, CreateDto dto, string userId);
    Task<List<ItemDto>> GetListAsync(Guid companyId);
    Task<ItemDto?> GetByIdAsync(Guid companyId, int id);
    Task<(bool Success, string Message)> UpdateAsync(Guid companyId, int id, UpdateDto dto, string userId);
    Task<(bool Success, string Message)> DeleteAsync(Guid companyId, int id, string userId);
    #endregion

    #region Business Logic
    Task<(bool Success, string Message)> ProcessAsync(Guid companyId, ProcessDto dto, string userId);
    Task<StatsDto> GetStatsAsync(Guid companyId);
    #endregion
}
```

### **Service Implementation Template**
```csharp
public class ModuleNameService : IModuleNameService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleNameService> _logger;
    // Other dependencies...

    public ModuleNameService(
        ApplicationDbContext context,
        ILogger<ModuleNameService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> CreateAsync(Guid companyId, CreateDto dto, string userId)
    {
        try
        {
            var entity = new Entity
            {
                CompanyId = companyId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                // Map from DTO
            };

            _context.Entities.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Entity created for company {CompanyId}", companyId);
            return (true, "Basariyla olusturuldu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity for company {CompanyId}", companyId);
            return (false, "Olusturma sirasinda hata olustu.");
        }
    }
}
```

## ?? **DTO PATTERNS**

### **DTO File Structure**
```csharp
namespace PushDashboard.DTOs;

#region Request DTOs
public class CreateModuleRequestDto
{
    [Required(ErrorMessage = "Alan gereklidir")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter olabilir")]
    public string Name { get; set; } = string.Empty;
}

public class UpdateModuleRequestDto
{
    [Required(ErrorMessage = "Alan gereklidir")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter olabilir")]
    public string Name { get; set; } = string.Empty;
}
#endregion

#region Response DTOs
public class ModuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ModuleStatsDto
{
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
}
#endregion
```

### **Validation Rules**
- Use Turkish error messages
- Always provide default values
- Use appropriate data annotations
- Group DTOs with #region tags

## ?? **VIEW PATTERNS**

### **View Structure**
```html
@model ModuleIndexViewModel
@{
    ViewData["Title"] = "Module Management";
}

<div class="container mx-auto px-4 py-6">
    <div class="flex justify-between items-center mb-6">
        <h1 class="text-2xl font-bold text-gray-900">Module Management</h1>
        <button class="btn btn-primary" onclick="openCreateModal()">
            Yeni Ekle
        </button>
    </div>

    <!-- Content -->
    @await Html.PartialAsync("Partials/_ModuleList", Model.Items)
    
    <!-- Modals -->
    @await Html.PartialAsync("Partials/_CreateModal")
</div>
```

### **Partial View Naming**
- `_ComponentName.cshtml` for reusable components
- `_ModalName.cshtml` for modal dialogs
- `_FormName.cshtml` for forms
- `_ListName.cshtml` for lists/tables

## ?? **DATABASE PATTERNS**

### **Entity Configuration**
```csharp
public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Company relationship
        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(e => new { e.CompanyId, e.Name });
        builder.HasIndex(e => e.CreatedAt);
    }
}
```

### **Migration Naming**
- Use descriptive names: `AddModuleNameTables`
- Include date: `20250717120000_AddModuleNameTables`

## ?? **ERROR HANDLING RULES**

### **Service Layer Errors**
```csharp
// Return tuple with success flag and message
return (false, "Kullanici dostu hata mesaji");

// Log technical details
_logger.LogError(ex, "Technical error details with context {CompanyId}", companyId);
```

### **Controller Layer Errors**
```csharp
// For page requests
TempData["ErrorMessage"] = "Kullanici dostu hata mesaji";
return RedirectToAction("Index");

// For AJAX requests
return Json(new { success = false, message = "Kullanici dostu hata mesaji" });
```

## ?? **MODULE DEVELOPMENT RULES**

### **Module Service Structure**
```
Services/Modules/ModuleName/
+-- IModuleNameService.cs           # Main business logic
+-- ModuleNameService.cs
+-- IModuleNameModuleService.cs     # Module management (activation, settings)
+-- ModuleNameModuleService.cs
```

### **Module Registration**
```csharp
// In Program.cs
builder.Services.AddScoped<IModuleNameService, ModuleNameService>();
builder.Services.AddScoped<IModuleNameModuleService, ModuleNameModuleService>();
```

## ?? **INTEGRATION PATTERNS**

### **E-commerce Integration**
```csharp
// Always check for active integration
var integration = await _context.CompanyIntegrations
    .Include(ci => ci.Integration)
    .FirstOrDefaultAsync(ci => ci.CompanyId == companyId && 
                              ci.Integration.Type == "ecommerce" && 
                              ci.IsActive);

if (integration == null)
{
    return (false, "E-ticaret entegrasyonu bulunamadi.");
}
```

## ?? **PERFORMANCE RULES**

### **Database Queries**
```csharp
// ? Use AsNoTracking for read-only queries
var items = await _context.Items
    .Where(i => i.CompanyId == companyId)
    .AsNoTracking()
    .ToListAsync();

// ? Use pagination for large datasets
var items = await _context.Items
    .Where(i => i.CompanyId == companyId)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// ? Use Include for navigation properties
var items = await _context.Items
    .Include(i => i.RelatedEntity)
    .Where(i => i.CompanyId == companyId)
    .ToListAsync();
```

## ?? **TESTING RULES**

### **Unit Test Structure**
```csharp
[TestFixture]
public class ModuleNameServiceTests
{
    private ApplicationDbContext _context;
    private ILogger<ModuleNameService> _logger;
    private ModuleNameService _service;

    [SetUp]
    public void Setup()
    {
        // Setup test context and service
    }

    [Test]
    public async Task CreateAsync_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var dto = new CreateDto { Name = "Test" };

        // Act
        var result = await _service.CreateAsync(companyId, dto, "userId");

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Basariyla olusturuldu.", result.Message);
    }
}
```

## ?? **CHECKLIST FOR AI ASSISTANTS**

Before suggesting any code, verify:

- [ ] Company validation is implemented
- [ ] BaseController is inherited (for auth controllers)
- [ ] Error handling with try-catch is present
- [ ] Logging is implemented with proper context
- [ ] Turkish error messages are used
- [ ] Async/await pattern is followed
- [ ] CompanyId filtering is applied to all queries
- [ ] Proper validation attributes are used
- [ ] Service registration is included
- [ ] Database configuration is added (if new entities)
- [ ] Naming conventions are followed
- [ ] Performance considerations are applied

## ?? **COMMON MISTAKES TO AVOID**

1. **Never** query database without CompanyId filter
2. **Never** skip error handling in services
3. **Never** use English error messages for users
4. **Never** forget to register services in Program.cs
5. **Never** skip logging important operations
6. **Never** use synchronous database operations
7. **Never** forget to validate user input
8. **Never** expose internal errors to users
9. **Never** skip authentication checks
10. **Never** break existing naming conventions

 
 # #   =Ø»Ü  * * C O D I N G   S T A N D A R D S * *  
 # # #   * * N a m i n g   C o n v e n t i o n s * *  
 -   * * C #   C o d e * * :   A L W A Y S   u s e   P a s c a l C a s e   ( c l a s s e s ,   m e t h o d s ,   p r o p e r t i e s ,   v a r i a b l e s )  
 -   * * J a v a S c r i p t   C o d e * * :   A L W A Y S   u s e   c a m e l C a s e   ( v a r i a b l e s ,   f u n c t i o n s ,   p r o p e r t i e s )  
 -   * * D a t a b a s e * * :   U s e   P a s c a l C a s e   f o r   t a b l e   a n d   c o l u m n   n a m e s  
 -   * * F i l e s * * :   P a s c a l C a s e   f o r   C #   f i l e s ,   c a m e l C a s e   f o r   J a v a S c r i p t   f i l e s  
  
 # # #   * * U s e r   E x p e r i e n c e   R u l e s * *  
 -   * * I n i t i a l   P a g e   L o a d * * :   L o a d   p a g e   s t r u c t u r e   f i r s t ,   t h e n   p o p u l a t e   w i t h   A J A X  
 -   * * D y n a m i c   O p e r a t i o n s * * :   U s e   A J A X   f o r   a l l   p o s t - l o a d   o p e r a t i o n s  
 -   * * N o   P a g e   R e l o a d s * * :   A v o i d   f u l l   p a g e   r e l o a d s   a f t e r   i n i t i a l   l o a d  
 -   * * L o a d i n g   S t a t e s * * :   S h o w   l o a d i n g   i n d i c a t o r s   d u r i n g   A J A X   o p e r a t i o n s  
  
 # # #   * * C o d e   E x a m p l e s * *  
 ` ` ` c s h a r p  
 / /   '  C O R R E C T   -   C #   P a s c a l C a s e  
 p u b l i c   c l a s s   M o d u l e S e r v i c e   :   I M o d u l e S e r v i c e  
 {  
         p u b l i c   a s y n c   T a s k < M o d u l e D t o >   G e t M o d u l e A s y n c ( i n t   M o d u l e I d )  
         {  
                 v a r   M o d u l e D a t a   =   a w a i t   _ c o n t e x t . M o d u l e s . F i n d A s y n c ( M o d u l e I d ) ;  
                 r e t u r n   M o d u l e D a t a ;  
         }  
 }  
 ` ` `  
  
 ` ` ` j a v a s c r i p t  
 / /   '  C O R R E C T   -   J a v a S c r i p t   c a m e l C a s e  
 f u n c t i o n   l o a d M o d u l e D a t a ( m o d u l e I d )   {  
         $ . a j a x ( {  
                 u r l :   ' / M o d u l e / G e t D a t a ' ,  
                 d a t a :   {   m o d u l e I d :   m o d u l e I d   } ,  
                 s u c c e s s :   f u n c t i o n ( r e s p o n s e )   {  
                         u p d a t e M o d u l e D i s p l a y ( r e s p o n s e ) ;  
                 }  
         } ) ;  
 }  
  
 / /   '  C O R R E C T   -   A J A X   p a t t e r n   f o r   u s e r   e x p e r i e n c e  
 $ ( d o c u m e n t ) . r e a d y ( f u n c t i o n ( )   {  
         / /   L o a d   i n i t i a l   p a g e   s t r u c t u r e  
         l o a d P a g e S t r u c t u r e ( ) ;  
          
         / /   T h e n   l o a d   d a t a   d y n a m i c a l l y  
         l o a d M o d u l e D a t a ( ) ;  
 } ) ;  
 ` ` `  
  
 # # #   * * A J A X   I m p l e m e n t a t i o n   R u l e s * *  
 ` ` ` j a v a s c r i p t  
 / /   '  C O R R E C T   -   A l w a y s   s h o w   l o a d i n g   s t a t e  
 f u n c t i o n   p e r f o r m A c t i o n ( d a t a )   {  
         s h o w L o a d i n g ( ) ;  
          
         $ . a j a x ( {  
                 u r l :   ' / C o n t r o l l e r / A c t i o n ' ,  
                 t y p e :   ' P O S T ' ,  
                 d a t a :   d a t a ,  
                 s u c c e s s :   f u n c t i o n ( r e s p o n s e )   {  
                         h i d e L o a d i n g ( ) ;  
                         i f   ( r e s p o n s e . s u c c e s s )   {  
                                 u p d a t e U I ( r e s p o n s e . d a t a ) ;  
                                 s h o w S u c c e s s ( r e s p o n s e . m e s s a g e ) ;  
                         }   e l s e   {  
                                 s h o w E r r o r ( r e s p o n s e . m e s s a g e ) ;  
                         }  
                 } ,  
                 e r r o r :   f u n c t i o n ( )   {  
                         h i d e L o a d i n g ( ) ;  
                         s h o w E r r o r ( ' 0_l e m   s 1r a s 1n d a   h a t a   o l u _t u . ' ) ;  
                 }  
         } ) ;  
 }  
 ` ` `  
 