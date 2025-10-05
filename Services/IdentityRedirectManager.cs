using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
namespace BlazorKanban.Services;
public class IdentityRedirectManager
{
    private readonly NavigationManager _navigation;

    public IdentityRedirectManager(NavigationManager navigation)
    {
        _navigation = navigation;
    }

    public void RedirectToLogin(string returnUrl = null)
    {
        var url = "Identity/Account/Login";
        if (!string.IsNullOrEmpty(returnUrl))
        {
            url += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        }
        _navigation.NavigateTo(url);
    }
}
