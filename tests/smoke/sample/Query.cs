namespace Sample;

public sealed record Account(int Id, int RoleId);

public sealed record Role(int Id, string Name);

public static class Accounts
{
    // A left join over IQueryable: `r?.Name` would not compile here (CS8072), so IDE0031 must not ask for it.
    public static IQueryable<object> WithRoleNames(IQueryable<Account> accounts, IQueryable<Role> roles) =>
        from a in accounts
        join r in roles on a.RoleId equals r.Id into matches
        from r in matches.DefaultIfEmpty()
        select new { Account = a, RoleName = r == null ? null : r.Name };
}
