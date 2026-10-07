# Augment Code Rules for PushDashboard Project

## ?? **MANDATORY RULES - ALWAYS FOLLOW**

### **1. Multi-Tenant Data Security**
- **ALWAYS** filter database queries by `CompanyId`
- **NEVER** allow cross-company data access
- **VALIDATE** company access in every controller action
- **USE** `GetCurrentUserCompanyId()` from BaseController

### **2. Authentication & Authorization**
- **INHERIT** from `BaseController` for authenticated controllers
- **APPLY** `[Authorize]` attribute on controllers
- **USE** `IUserContextService` for user context access

### **3. Error Handling**
- **WRAP** all service methods in try-catch blocks
- **LOG** errors with structured logging and context
- **RETURN** user-friendly Turkish error messages
- **USE** `(bool Success, string Message)` tuple pattern

### **4. Async Programming**
- **USE** async/await for all database operations
- **APPLY** `AsNoTracking()` for read-only queries
- **IMPLEMENT** proper cancellation token support

## ??? **ARCHITECTURE PATTERNS**

### **Controller Pattern**
```csharp
[Authorize]
public class ModuleController : BaseController
{
    public ModuleController(IUserContextService userContextService) 
        : base(userContextService) { }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            TempData["ErrorMessage"] = "Sirket bilgisi bulunamadi.";
            return RedirectToAction("Index", "Home");
        }
        
        try
        {
            var data = await _service.GetDataAsync(companyId.Value);
            return View(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data for company {CompanyId}", companyId);
            TempData["ErrorMessage"] = "Veriler y�klenirken hata olustu.";
            return View();
        }
    }
}
```

### **Service Pattern**
```csharp
namespace PushDashboard.Services.Modules.ModuleName;

public interface IModuleService
{
    Task<(bool Success, string Message)> CreateAsync(Guid companyId, CreateDto dto, string userId);
    Task<List<ItemDto>> GetListAsync(Guid companyId);
}

public class ModuleService : IModuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleService> _logger;

    public async Task<(bool Success, string Message)> CreateAsync(Guid companyId, CreateDto dto, string userId)
    {
        try
        {
            var entity = new Entity
            {
                CompanyId = companyId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
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

### **Database Query Pattern**
```csharp
// ? CORRECT - Always filter by CompanyId
var items = await _context.Items
    .Where(i => i.CompanyId == companyId)
    .Include(i => i.RelatedEntity)
    .AsNoTracking()
    .ToListAsync();

// ? WRONG - Never query without company filter
var items = await _context.Items.ToListAsync();
```

## ?? **DTO & VALIDATION PATTERNS**

### **DTO Structure**
```csharp
namespace PushDashboard.DTOs;

#region Request DTOs
public class CreateModuleRequestDto
{
    [Required(ErrorMessage = "Alan gereklidir")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter olabilir")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "1 ile 100 arasinda olmalidir")]
    public int Value { get; set; }
}
#endregion

#region Response DTOs
public class ModuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
#endregion
```

## ?? **NAMING CONVENTIONS**

### **Files & Folders**
- Controllers: `ModuleNameController.cs`
- Services: `IModuleNameService.cs`, `ModuleNameService.cs`
- DTOs: `ModuleNameDTOs.cs`
- Views: `Views/ModuleName/Action.cshtml`
- Partials: `Views/ModuleName/Partials/_ComponentName.cshtml`

### **Namespaces**
- Controllers: `namespace PushDashboard.Controllers;`
- Services: `namespace PushDashboard.Services.Modules.ModuleName;`
- DTOs: `namespace PushDashboard.DTOs;`

## ?? **FRONTEND PATTERNS**

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

    @await Html.PartialAsync("Partials/_ModuleList", Model.Items)
</div>
```

### **AJAX Pattern**
```javascript
function performAction(data) {
    $.ajax({
        url: '/Controller/Action',
        type: 'POST',
        data: data,
        success: function(response) {
            if (response.success) {
                showSuccess(response.message);
            } else {
                showError(response.message);
            }
        },
        error: function() {
            showError('Islem sirasinda hata olustu.');
        }
    });
}
```

## ?? **MODULE DEVELOPMENT RULES**

### **Module Structure**
```
Services/Modules/ModuleName/
+-- IModuleNameService.cs           # Main business logic
+-- ModuleNameService.cs
+-- IModuleNameModuleService.cs     # Module management
+-- ModuleNameModuleService.cs
```

### **Module Validation**
```csharp
// Always check if company has active module
var hasActiveModule = await _context.CompanyModules
    .AnyAsync(cm => cm.CompanyId == companyId && 
                   cm.Module.Name == "ModuleName" && 
                   cm.IsActive);

if (!hasActiveModule)
{
    return (false, "Mod�l aktif degil.");
}
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

## ?? **CRITICAL CHECKLIST**

Before implementing any feature, verify:

- [ ] Company validation is implemented
- [ ] BaseController is inherited (for auth controllers)
- [ ] Error handling with try-catch is present
- [ ] Logging is implemented with proper context
- [ ] Turkish error messages are used
- [ ] Async/await pattern is followed
- [ ] CompanyId filtering is applied to all queries
- [ ] Proper validation attributes are used
- [ ] Service registration is included in Program.cs
- [ ] Database configuration is added (if new entities)

## ?? **COMMON MISTAKES TO AVOID**

1. **NEVER** query database without CompanyId filter
2. **NEVER** skip error handling in services
3. **NEVER** use English error messages for users
4. **NEVER** forget to register services in Program.cs
5. **NEVER** skip logging important operations
6. **NEVER** use synchronous database operations
7. **NEVER** forget to validate user input
8. **NEVER** expose internal errors to users
9. **NEVER** skip authentication checks
10. **NEVER** break existing naming conventions

## ?? **QUICK REFERENCE**

### **Service Registration**
```csharp
// In Program.cs
builder.Services.AddScoped<IModuleService, ModuleService>();
```

### **Entity Configuration**
```csharp
// In Data/Configurations/
public class EntityConfiguration : IEntityTypeConfiguration<Entity>
{
    public void Configure(EntityTypeBuilder<Entity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(e => new { e.CompanyId, e.Name });
        
        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### **Migration Command**
```bash
dotnet ef migrations add AddModuleNameTables
dotnet ef database update
```
