using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Entities;

namespace Server.Services;

public class UserStore(IDbContextFactory<ApplicationDbContext> contextFactory, IdentityErrorDescriber describer) : UserStore<EntUser>(contextFactory.CreateDbContext(), describer), IUserStore<EntUser> {
}