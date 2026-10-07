# PushDashboard Development Guidelines for AI Assistants

## 🎯 **CRITICAL RULES - MUST FOLLOW**

### **Multi-Tenant Architecture**
- **ALWAYS** filter by `CompanyId` in database queries
- **NEVER** allow cross-company data access
- **ALWAYS** use `GetCurrentUserCompanyId()` from BaseController
- **VALIDATE** company access in every controller action

### **Authentication & Authorization**
- **INHERIT** from `BaseController` for authenticated controllers
- **USE** `IUserContextService` for user context access
- **APPLY** `[Authorize]` attribute on controllers
- **CHECK** user authentication before operations

## 🏗️ **ARCHITECTURE PATTERNS**

### **Technology Stack**
- ASP.NET Core MVC (.NET 8)
- PostgreSQL + Entity Framework Core
- ASP.NET Core Identity + Cookie Auth
- Razor Views + Tailwind CSS + JavaScript
- SignalR for real-time updates

### **Folder Structure**
```
PushDashboard/
├── Controllers/           # MVC Controllers (inherit BaseController)
├── Services/             # Business Logic
│   ├── Modules/         # Module services (Birthday, GiftWheel, etc.)
│   ├── Integrations/    # E-commerce integrations
│   └── Notifications/   # Notification services
├── Models/              # EF Models
├── DTOs/               # Data Transfer Objects
├── ViewModels/         # View models
├── Views/              # Razor Views + Partials
├── Data/               # DbContext, Configurations
├── Middleware/         # Custom middleware
└── wwwroot/           # Static files
```

## 🎯 **CONTROLLER PATTERNS**

### **Base Controller Usage**
```csharp
[Authorize]
public class YourController : BaseController
{
    public YourController(IUserContextService userContextService) 
        : base(userContextService) { }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Index", "Home");
        }
        
        try
        {
            // Your logic here
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in action for company {CompanyId}", companyId);
            TempData["ErrorMessage"] = "İşlem sırasında hata oluştu.";
            return View();
        }
    }
}
```

### **API Controller Pattern**
```csharp
[ApiController]
[Route("api/[controller]")]
public class YourApiController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ResponseDto>> Create([FromBody] RequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _service.CreateAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating resource");
            return StatusCode(500, new { message = "İşlem sırasında hata oluştu." });
        }
    }
}
```

## 🔧 **SERVICE LAYER PATTERNS**

### **Service Interface & Implementation**
```csharp
namespace PushDashboard.Services.Modules.ModuleName;

public interface IModuleService
{
    Task<(bool Success, string Message)> CreateAsync(Guid companyId, CreateDto dto, string userId);
    Task<List<ItemDto>> GetListAsync(Guid companyId);
    Task<ItemDto?> GetByIdAsync(Guid companyId, int id);
}

public class ModuleService : IModuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleService> _logger;
    
    public ModuleService(ApplicationDbContext context, ILogger<ModuleService> logger)
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
                // Map from DTO
            };

            _context.Entities.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Entity created for company {CompanyId}", companyId);
            return (true, "Başarıyla oluşturuldu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity for company {CompanyId}", companyId);
            return (false, "Oluşturma sırasında hata oluştu.");
        }
    }
}
```

### **Service Registration in Program.cs**
```csharp
builder.Services.AddScoped<IModuleService, ModuleService>();
```

## 📊 **DATA ACCESS PATTERNS**

### **Entity Framework Rules**
```csharp
// ALWAYS filter by CompanyId
var items = await _context.Items
    .Where(i => i.CompanyId == companyId)
    .Include(i => i.RelatedEntity)
    .AsNoTracking() // For read-only queries
    .ToListAsync();

// Use transactions for complex operations
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Multiple operations
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
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
        
        // Foreign key relationships
        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## 🎨 **DTO & VALIDATION PATTERNS**

### **DTO Structure**
```csharp
namespace PushDashboard.DTOs;

#region Request DTOs
public class CreateRequestDto
{
    [Required(ErrorMessage = "Alan gereklidir")]
    [StringLength(100, ErrorMessage = "En fazla 100 karakter olabilir")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "1 ile 100 arasında olmalıdır")]
    public int Value { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string? Email { get; set; }
}
#endregion

#region Response DTOs
public class ResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
#endregion
```

## 🔗 **INTEGRATION PATTERNS**

### **E-commerce Integration Factory**
```csharp
public class EcommerceServiceFactory
{
    public IEcommerceService CreateService(CompanyIntegration integration)
    {
        return integration.Integration.Name switch
        {
            "Ticimax" => new TicimaxService(integration),
            "Ikas" => new IkasService(integration),
            _ => throw new NotSupportedException($"Platform {integration.Integration.Name} desteklenmiyor")
        };
    }
}
```

## 📱 **MODULE SYSTEM PATTERNS**

### **Module Service Structure**
```
Services/Modules/ModuleName/
├── IModuleService.cs           # Main business logic
├── ModuleService.cs
├── IModuleModuleService.cs     # Module management
└── ModuleModuleService.cs
```

### **Module Validation**
```csharp
// Check if company has active module
var hasActiveModule = await _context.CompanyModules
    .AnyAsync(cm => cm.CompanyId == companyId && 
                   cm.Module.Name == "ModuleName" && 
                   cm.IsActive);

if (!hasActiveModule)
{
    return (false, "Modül aktif değil.");
}
```

## 🎨 **FRONTEND PATTERNS**

### **View Organization**
```
Views/ControllerName/
├── Index.cshtml
├── Partials/
│   ├── _ComponentName.cshtml
│   ├── _ModalName.cshtml
│   └── _FormName.cshtml
```

### **Partial View Usage**
```html
<!-- In main view -->
@await Html.PartialAsync("Partials/_ComponentName", Model.ComponentData)

<!-- AJAX loading -->
<div id="content-area">
    <!-- Content loaded via AJAX -->
</div>
```

### **JavaScript Patterns**
```javascript
// AJAX with error handling
function performAction(data) {
    $.ajax({
        url: '/Controller/Action',
        type: 'POST',
        data: data,
        success: function(response) {
            if (response.success) {
                showSuccess(response.message);
                // Update UI
            } else {
                showError(response.message);
            }
        },
        error: function() {
            showError('İşlem sırasında hata oluştu.');
        }
    });
}
```

## 🚨 **ERROR HANDLING & LOGGING**

### **Logging Pattern**
```csharp
try
{
    _logger.LogInformation("Starting operation for company {CompanyId}", companyId);
    
    // Operation logic
    
    _logger.LogInformation("Operation completed successfully for company {CompanyId}", companyId);
    return (true, "İşlem başarılı.");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error in operation for company {CompanyId}", companyId);
    return (false, "İşlem sırasında hata oluştu.");
}
```

### **User-Friendly Error Messages**
- Use Turkish error messages
- Provide actionable feedback
- Log technical details, show user-friendly messages
- Use TempData for page redirects
- Return JSON for AJAX calls

## 🔄 **BACKGROUND PROCESSING**

### **SignalR Progress Updates**
```csharp
public class ProcessingHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
}

// In service
await _hubContext.Clients.Group($"company-{companyId}")
    .SendAsync("ProgressUpdate", new { progress = 50, message = "İşleniyor..." });
```

## 📊 **PERFORMANCE RULES**

- Use `async/await` for all I/O operations
- Apply pagination for large datasets: `Skip().Take()`
- Use `AsNoTracking()` for read-only queries
- Add database indexes for performance

## 🧪 **TESTING PATTERNS**

### **Unit Test Structure**
```csharp
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
}
```

## 🚀 **DEPLOYMENT CONSIDERATIONS**

- Environment-specific configurations in appsettings
- Use connection string from configuration
- Implement health checks
- Add proper logging configuration
- Handle database migrations

---

## ⚠️ **CRITICAL REMINDERS FOR AI ASSISTANTS**

1. **NEVER** skip company validation
2. **ALWAYS** use proper error handling
3. **FOLLOW** existing naming conventions
4. **MAINTAIN** consistent patterns
5. **INCLUDE** proper logging
6. **VALIDATE** input data
7. **USE** transactions for complex operations
8. **RESPECT** multi-tenant isolation
9. **IMPLEMENT** proper authentication checks
10. **TEST** thoroughly before suggesting deployment
