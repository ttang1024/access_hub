namespace AccessHub.BuildingBlocks.Infrastructure.Auth;

/// <summary>Role, policy, scheme, header and claim names. No magic strings elsewhere.</summary>
public static class AuthConstants
{
    /// <summary>Keycloak realm roles.</summary>
    public static class Roles
    {
        /// <summary>Everything.</summary>
        public const string Admin = "admin";

        /// <summary>Read everything; acknowledge and close alarms.</summary>
        public const string Operator = "operator";

        /// <summary>Read only.</summary>
        public const string Viewer = "viewer";
    }

    /// <summary>Authorisation policies.</summary>
    public static class Policies
    {
        /// <summary>Requires the admin role.</summary>
        public const string Admin = "admin";

        /// <summary>Requires admin or operator.</summary>
        public const string Operator = "operator";

        /// <summary>Any operator role, including viewer.</summary>
        public const string Viewer = "viewer";

        /// <summary>An authenticated site controller.</summary>
        public const string Site = "site";
    }

    /// <summary>Authentication schemes.</summary>
    public static class Schemes
    {
        /// <summary>Site controllers authenticate with X-Site-Id and X-Api-Key.</summary>
        public const string SiteApiKey = "SiteApiKey";
    }

    /// <summary>HTTP and gRPC metadata headers.</summary>
    public static class Headers
    {
        /// <summary>The calling site's ID.</summary>
        public const string SiteId = "X-Site-Id";

        /// <summary>The calling site's API key.</summary>
        public const string ApiKey = "X-Api-Key";
    }

    /// <summary>Claim types.</summary>
    public static class Claims
    {
        /// <summary>Flat list of realm roles emitted by the Keycloak realm mapper.</summary>
        public const string Roles = "roles";

        /// <summary>Preferred user name.</summary>
        public const string Name = "preferred_username";

        /// <summary>Subject.</summary>
        public const string Subject = "sub";

        /// <summary>The authenticated site's ID.</summary>
        public const string SiteId = "site_id";
    }
}
