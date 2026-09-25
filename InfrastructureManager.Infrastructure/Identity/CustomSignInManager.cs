using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InfrastructureManager.Infrastructure.Identity;

/// <summary>
/// Extends the default SignInManager to block inactive users from signing
/// in, regardless of a correct password — while still validating the
/// password FIRST, so a wrong password for a deactivated account looks
/// identical to a wrong password for any other account (no "this account
/// has been deactivated" disclosure to someone who doesn't actually know
/// the password).
/// </summary>
public class CustomSignInManager : SignInManager<ApplicationUser>
{
    public CustomSignInManager(
        UserManager<ApplicationUser>          userManager,
        IHttpContextAccessor                  contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions>             optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider        schemes,
        IUserConfirmation<ApplicationUser>   confirmation)
        : base(userManager, contextAccessor, claimsFactory,
               optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<SignInResult> PasswordSignInAsync(
        string userName,
        string password,
        bool   isPersistent,
        bool   lockoutOnFailure)
    {
        var user = await UserManager.FindByEmailAsync(userName);
        if (user == null)
        {
            // Onbestaand e-mailadres: laat de basisimplementatie het normale,
            // generieke "mislukt" pad afhandelen (fake password hasher timing
            // incluis, zodat een onbestaand adres niet sneller antwoordt dan
            // een bestaand adres).
            return await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);
        }

        // Wachtwoord ALTIJD eerst controleren, vóór IsActive bekeken wordt —
        // anders verraadt de foutmelding of een account bestaat én
        // gedeactiveerd is, zonder dat de aanvaller het wachtwoord hoefde te
        // kennen.
        var result = await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);

        if (result.Succeeded && !user.IsActive)
        {
            // Het wachtwoord kloppen bevestigen we niet extra — meteen weer
            // uitloggen en het generieke "niet toegestaan"-resultaat
            // teruggeven, zodat dit pad zich exact hetzelfde gedraagt als bij
            // een fout wachtwoord op een actief account (behalve de finale
            // melding "account deactivated", die pas na correcte
            // authenticatie getoond wordt — een acceptabele, kleine
            // bevestiging enkel richting iemand die het wachtwoord al kende).
            await SignOutAsync();
            return SignInResult.NotAllowed;
        }

        return result;
    }
}