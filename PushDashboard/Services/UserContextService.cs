using PushDashboard.Models;

namespace PushDashboard.Services;

public interface IUserContextService
{
    string? UserId { get; }
    Guid? CompanyId { get; }
    ApplicationUser? User { get; }
    bool IsAuthenticated { get; }
    
    void SetUserContext(string? userId, Guid? companyId, ApplicationUser? user);
    void Clear();
}

public class UserContextService : IUserContextService
{
    private static readonly AsyncLocal<UserContextData> _userContext = new();

    public string? UserId => _userContext.Value?.UserId;
    public Guid? CompanyId => _userContext.Value?.CompanyId;
    public ApplicationUser? User => _userContext.Value?.User;
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

    public void SetUserContext(string? userId, Guid? companyId, ApplicationUser? user)
    {
        _userContext.Value = new UserContextData
        {
            UserId = userId,
            CompanyId = companyId,
            User = user
        };
    }

    public void Clear()
    {
        _userContext.Value = null;
    }

    private class UserContextData
    {
        public string? UserId { get; set; }
        public Guid? CompanyId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
