using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

public class CustomUserValidator : UserValidator<IdentityUser>
{
    public CustomUserValidator(IdentityErrorDescriber errors) : base(errors) { }

    public override async Task<IdentityResult> ValidateAsync(UserManager<IdentityUser> manager, IdentityUser user)
    {
        IdentityResult result = await base.ValidateAsync(manager, user);

        IdentityUser otherUser = await manager.FindByNameAsync(user.UserName);
        if (otherUser is not null && !string.Equals(await manager.GetUserIdAsync(otherUser), await manager.GetUserIdAsync(user)))
        {
            IdentityError duplicateUserNameError = result.Errors.FirstOrDefault(e => e.Code == "DuplicateUserName");
            if (duplicateUserNameError != null)
                result = IdentityResult.Success;
        }

        return result;
    }
}
